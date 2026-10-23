using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using AshkanLMS.Core;

namespace AshkanLMS.Services
{
    // Optional central audit store. Must be provisioned separately before enabling.
    internal static class SqlDeploymentAuditStore
    {
        private static string ConnectionString()
        {
            var enabled = ConfigurationManager.AppSettings["CentralDeploymentAuditEnabled"];
            if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase)) return null;
            var entry = ConfigurationManager.ConnectionStrings["AshkanLMSAudit"];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ConnectionString))
                throw new InvalidOperationException("Central audit is enabled but AshkanLMSAudit is not configured.");
            return entry.ConnectionString;
        }

        internal static bool IsEnabled { get { return string.Equals(ConfigurationManager.AppSettings["CentralDeploymentAuditEnabled"], "true", StringComparison.OrdinalIgnoreCase); } }

        internal static void Append(string action, bool succeeded, string summary)
        {
            AuthorizationService.RequireAccountAdmin();
            var connectionString = ConnectionString();
            if (connectionString == null) return;
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"INSERT INTO dbo.LmsDeploymentAudit
                (OccurredUtc, Administrator, ActionName, Outcome, Summary)
                VALUES (@time, @admin, @action, @outcome, @summary)", connection))
            {
                command.Parameters.Add("@time", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                command.Parameters.Add("@admin", SqlDbType.NVarChar, 80).Value = DeploymentAuditTrail.Sanitize(AppSession.UserName, 80);
                command.Parameters.Add("@action", SqlDbType.NVarChar, 64).Value = DeploymentAuditTrail.Sanitize(action, 64);
                command.Parameters.Add("@outcome", SqlDbType.VarChar, 7).Value = succeeded ? "SUCCESS" : "FAILED";
                command.Parameters.Add("@summary", SqlDbType.NVarChar, 180).Value = DeploymentAuditTrail.Sanitize(summary, 180);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        internal static void AppendWithId(string eventId, string occurredUtc, string admin,
            string action, string outcome, string summary)
        {
            AuthorizationService.RequireAccountAdmin();
            var connectionString = ConnectionString();
            if (connectionString == null) throw new InvalidOperationException("Central audit is disabled.");
            DateTimeOffset timestamp;
            if (!DateTimeOffset.TryParse(occurredUtc, out timestamp))
                throw new InvalidOperationException("Invalid queued timestamp.");
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"IF NOT EXISTS
                (SELECT 1 FROM dbo.LmsDeploymentAudit WITH (UPDLOCK, HOLDLOCK) WHERE EventId = @id)
                INSERT INTO dbo.LmsDeploymentAudit
                (EventId, OccurredUtc, Administrator, ActionName, Outcome, Summary)
                VALUES (@id, @time, @admin, @action, @outcome, @summary)", connection))
            {
                command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = Guid.ParseExact(eventId, "N");
                command.Parameters.Add("@time", SqlDbType.DateTime2).Value = timestamp.UtcDateTime;
                command.Parameters.Add("@admin", SqlDbType.NVarChar, 80).Value = DeploymentAuditTrail.Sanitize(admin, 80);
                command.Parameters.Add("@action", SqlDbType.NVarChar, 64).Value = DeploymentAuditTrail.Sanitize(action, 64);
                command.Parameters.Add("@outcome", SqlDbType.VarChar, 7).Value = outcome;
                command.Parameters.Add("@summary", SqlDbType.NVarChar, 180).Value = DeploymentAuditTrail.Sanitize(summary, 180);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        internal static string[] ReadRecent(int max = 250, string actionFilter = null, string outcomeFilter = null)
        {
            AuthorizationService.RequireAccountAdmin();
            var connectionString = ConnectionString();
            if (connectionString == null) return new string[0];
            max = Math.Max(1, Math.Min(max, 1000));
            var rows = new List<string>();
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"SELECT TOP (@max) OccurredUtc, Administrator, ActionName, Outcome, Summary
                FROM dbo.LmsDeploymentAudit
                WHERE (@action IS NULL OR ActionName = @action)
                AND (@outcome IS NULL OR Outcome = @outcome)
                ORDER BY AuditId DESC", connection))
            {
                command.Parameters.Add("@max", SqlDbType.Int).Value = max;
                command.Parameters.Add("@action", SqlDbType.NVarChar, 64).Value = string.IsNullOrWhiteSpace(actionFilter) ? (object)DBNull.Value : actionFilter;
                command.Parameters.Add("@outcome", SqlDbType.VarChar, 7).Value = string.IsNullOrWhiteSpace(outcomeFilter) ? (object)DBNull.Value : outcomeFilter;
                connection.Open();
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        rows.Add(string.Join("\t", new[] {
                            DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc).ToString("o"),
                            DeploymentAuditTrail.Sanitize(reader.GetString(1), 80),
                            DeploymentAuditTrail.Sanitize(reader.GetString(2), 64),
                            reader.GetString(3), DeploymentAuditTrail.Sanitize(reader.GetString(4), 180)
                        }));
            }
            return rows.ToArray();
        }
    }
}
