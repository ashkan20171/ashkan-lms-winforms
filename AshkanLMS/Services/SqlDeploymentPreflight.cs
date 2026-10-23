using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace AshkanLMS.Services
{
    /// <summary>Read-only SQL Server deployment checks. A successful result does not certify backup recovery.</summary>
    internal sealed class SqlDeploymentPreflight
    {
        internal sealed class Check
        {
            internal string Name { get; set; }
            internal bool Passed { get; set; }
            internal string Detail { get; set; }
        }

        internal static List<Check> Run(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A SQL Server connection string is required.", "connectionString");
            var checks = new List<Check>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new SqlCommand("SELECT DB_NAME(), @@SERVERNAME", connection))
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) throw new InvalidOperationException("Cannot identify the SQL Server database.");
                    string database = Convert.ToString(reader.GetValue(0));
                    string server = Convert.ToString(reader.GetValue(1));
                    checks.Add(new Check { Name = "SQL connection", Passed = true, Detail = server + " / " + database });
                    checks.Add(new Check { Name = "System database guard", Passed =
                        !new[] { "master", "model", "msdb", "tempdb" }.Contains(database, StringComparer.OrdinalIgnoreCase),
                        Detail = database });
                }
                string[] tables = { "LmsStudents", "LmsInstructors", "LmsCourses", "LmsEnrollments",
                    "LmsAttendance", "LmsGrades", "LmsCertificates" };
                foreach (string table in tables)
                {
                    using (var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@name, 'U') IS NULL THEN 0 ELSE 1 END", connection))
                    {
                        cmd.Parameters.Add("@name", SqlDbType.NVarChar, 256).Value = "dbo." + table;
                        checks.Add(new Check { Name = "Table " + table,
                            Passed = Convert.ToInt32(cmd.ExecuteScalar()) == 1,
                            Detail = "dbo." + table });
                    }
                }
                // This only checks backup metadata visibility. It does not verify a restorable backup.
                try
                {
                    using (var cmd = new SqlCommand(@"SELECT MAX(backup_finish_date) FROM msdb.dbo.backupset
                        WHERE database_name = DB_NAME() AND type = 'D'", connection))
                    {
                        object value = cmd.ExecuteScalar();
                        checks.Add(new Check { Name = "Backup history (informational)", Passed = true,
                            Detail = value == null || value == DBNull.Value ? "No visible full backup history; verify manually" :
                                Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss") + " (restore not tested)" });
                    }
                }
                catch (SqlException)
                {
                    checks.Add(new Check { Name = "Backup history (informational)", Passed = true,
                        Detail = "Backup metadata unavailable; verify manually" });
                }
            }
            return checks;
        }

        internal static string Format(IEnumerable<Check> checks)
        {
            var builder = new StringBuilder();
            foreach (var check in checks)
                builder.AppendLine((check.Passed ? "PASS" : "BLOCK") + " | " + check.Name + " | " + check.Detail);
            builder.AppendLine("Manual gates: verify backups with a restore test, access permissions, and application build before cutover.");
            return builder.ToString();
        }
    }
}
