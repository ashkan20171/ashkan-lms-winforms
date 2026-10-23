using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AshkanLMS.Core;

namespace AshkanLMS.Services
{
    // Durable at-least-once queue. SQL EventId uniqueness makes retries idempotent.
    internal static class DeploymentAuditOutbox
    {
        private static readonly object Sync = new object();
        private static readonly string DirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AshkanLMS", "audit-outbox");
        internal static string LastError { get; private set; }
        private static DateTime NextRetryUtc = DateTime.MinValue;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);
        private static string QuarantinePath { get { return Path.Combine(DirectoryPath, "quarantine"); } }

        // SHA-256 detects accidental corruption; it is not an authenticity signature.
        private static string Digest(string payload)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)))
                    .Replace("-", "").ToLowerInvariant();
        }

        private static bool ValidEntry(string file, string[] fields)
        {
            Guid parsed;
            if ((fields.Length != 6 && fields.Length != 7) ||
                !Guid.TryParseExact(fields[0], "N", out parsed) ||
                !string.Equals(Path.GetFileNameWithoutExtension(file), fields[0], StringComparison.OrdinalIgnoreCase))
                return false;
            if (fields.Length == 6) return true; // Existing pre-123 entries.
            var payload = string.Join("\t", fields.Take(6));
            return string.Equals(Digest(payload), fields[6], StringComparison.OrdinalIgnoreCase);
        }

        internal static int QuarantinedCount
        {
            get
            {
                AuthorizationService.RequireAccountAdmin();
                lock (Sync) return Directory.Exists(QuarantinePath)
                    ? Directory.GetFiles(QuarantinePath, "*.damaged").Length : 0;
            }
        }

        // A file is moved, never deleted, and its payload is not shown in the UI.
        private static void Quarantine(string file)
        {
            Directory.CreateDirectory(QuarantinePath);
            var destination = Path.Combine(QuarantinePath, Path.GetFileNameWithoutExtension(file) + ".damaged");
            if (File.Exists(destination))
                destination = Path.Combine(QuarantinePath, Path.GetFileNameWithoutExtension(file) + "-" + Guid.NewGuid().ToString("N") + ".damaged");
            File.Move(file, destination);
        }

        internal static string[] InspectQuarantine(int limit)
        {
            AuthorizationService.RequireAccountAdmin();
            lock (Sync)
            {
                if (!Directory.Exists(QuarantinePath)) return new string[0];
                return Directory.GetFiles(QuarantinePath, "*.damaged")
                    .OrderBy(x => x).Take(Math.Max(1, Math.Min(limit, 500)))
                    .Select(x => Path.GetFileName(x) + "\tQUARANTINED").ToArray();
            }
        }

        internal static int PendingCount
        {
            get
            {
                AuthorizationService.RequireAccountAdmin();
                lock (Sync) return Directory.Exists(DirectoryPath)
                    ? Directory.GetFiles(DirectoryPath, "*.pending").Length : 0;
            }
        }

        // Read-only operational overview; no audit payloads or secrets are exposed.
        internal static string[] InspectPending(int limit)
        {
            AuthorizationService.RequireAccountAdmin();
            lock (Sync)
            {
                if (!Directory.Exists(DirectoryPath)) return new string[0];
                return Directory.GetFiles(DirectoryPath, "*.pending")
                    .OrderBy(x => x).Take(Math.Max(1, Math.Min(limit, 500)))
                    .Select(file =>
                    {
                        try
                        {
                            var fields = File.ReadAllText(file, Encoding.UTF8).Split('\t');
                            var valid = ValidEntry(file, fields);
                            return Path.GetFileName(file) + "\t" + (valid ? "PENDING" : "DAMAGED");
                        }
                        catch (IOException) { return Path.GetFileName(file) + "\tUNREADABLE"; }
                        catch (UnauthorizedAccessException) { return Path.GetFileName(file) + "\tUNREADABLE"; }
                    }).ToArray();
            }
        }

        internal static void Enqueue(string auditLine)
        {
            AuthorizationService.RequireAccountAdmin();
            lock (Sync)
            {
                Directory.CreateDirectory(DirectoryPath);
                var id = Guid.NewGuid().ToString("N");
                var pending = Path.Combine(DirectoryPath, id + ".pending");
                var temp = Path.Combine(DirectoryPath, id + ".tmp");
                var payload = id + "\t" + auditLine;
                File.WriteAllText(temp, payload + "\t" + Digest(payload), new UTF8Encoding(false));
                File.Move(temp, pending);
            }
        }

        internal static int FlushPending(int limit)
        {
            AuthorizationService.RequireAccountAdmin();
            if (!SqlDeploymentAuditStore.IsEnabled) return 0;
            limit = Math.Max(1, Math.Min(limit, 100));
            lock (Sync)
            {
                LastError = null;
                if (DateTime.UtcNow < NextRetryUtc)
                {
                    LastError = "Audit sync is cooling down after a connection failure.";
                    return 0;
                }
                if (!Directory.Exists(DirectoryPath)) return 0;
                // Exclusive file handle prevents two application processes flushing concurrently.
                FileStream lease;
                try
                {
                    lease = new FileStream(Path.Combine(DirectoryPath, "sync.lock"),
                        FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    LastError = "Audit sync is already running in another process.";
                    return 0;
                }
                var sent = 0;
                using (lease)
                {
                foreach (var file in Directory.GetFiles(DirectoryPath, "*.pending").OrderBy(x => x).Take(limit))
                {
                    try
                    {
                        var fields = File.ReadAllText(file, Encoding.UTF8).Split('\t');
                        if (!ValidEntry(file, fields))
                        {
                            Quarantine(file);
                            LastError = "An invalid audit entry was quarantined for manual review.";
                            continue; // Other valid entries must still be delivered.
                        }
                        SqlDeploymentAuditStore.AppendWithId(fields[0], fields[1], fields[2], fields[3], fields[4], fields[5]);
                        File.Delete(file);
                        sent++;
                        NextRetryUtc = DateTime.MinValue;
                    }
                    catch (Exception ex)
                    {
                        // Do not persist connection strings or exception details.
                        LastError = "Central audit sync failed (" + ex.GetType().Name + ").";
                        NextRetryUtc = DateTime.UtcNow.Add(RetryDelay);
                        break; // No busy loop; subsequent explicit retry observes cooldown.
                    }
                }
                }
                return sent;
            }
        }
    }
}
