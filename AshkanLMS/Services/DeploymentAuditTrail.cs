using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AshkanLMS.Core;

namespace AshkanLMS.Services
{
    // Local append-only operational history. Not a tamper-proof security log.
    public static class DeploymentAuditTrail
    {
        private static readonly object Sync = new object();
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AshkanLMS", "deployment-audit.tsv");

        internal static string Sanitize(string value, int limit)
        {
            value = (value ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
            return value.Length > limit ? value.Substring(0, limit) : value;
        }

        public static void Record(string action, bool succeeded, string summary)
        {
            AuthorizationService.RequireAccountAdmin();
            var line = string.Join("\t", new [] {
                DateTimeOffset.UtcNow.ToString("o"), Sanitize(AppSession.UserName, 80),
                Sanitize(action, 64), succeeded ? "SUCCESS" : "FAILED", Sanitize(summary, 180)
            });
            lock (Sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                using (var stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    writer.WriteLine(line);
            }
            // Queue before sending; a failed central connection never discards the event.
            if (SqlDeploymentAuditStore.IsEnabled)
            {
                DeploymentAuditOutbox.Enqueue(line);
                DeploymentAuditOutbox.FlushPending(25);
            }
        }

        public static string[] ReadRecent(int max = 250)
        {
            AuthorizationService.RequireAccountAdmin();
            lock (Sync)
            {
                if (!File.Exists(LogPath)) return new string[0];
                return File.ReadAllLines(LogPath, Encoding.UTF8).Reverse().Take(Math.Max(1, Math.Min(max, 1000))).ToArray();
            }
        }

        public static string ToCsv(IEnumerable<string> lines)
        {
            var output = new StringBuilder("UtcTime,Administrator,Action,Outcome,Summary\r\n");
            foreach (var line in lines)
            {
                var fields = line.Split('\t');
                if (fields.Length != 5) continue;
                output.AppendLine(string.Join(",", fields.Select(x => "\"" + x.Replace("\"", "\"\"") + "\"")));
            }
            return output.ToString();
        }
    }
}
