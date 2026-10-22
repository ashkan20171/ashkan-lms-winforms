using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using AshkanLMS.Models;
using AshkanLMS.Core;

namespace AshkanLMS.Services
{
    /// <summary>
    /// Opt-in relational SQL Server access. Does not modify legacy snapshot state.
    /// Callers must use this repository exclusively for these entities after cutover.
    /// </summary>
    internal sealed class RelationalCoreRepository
    {
        private readonly string connectionString;
        internal RelationalCoreRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required.", "connectionString");
            this.connectionString = connectionString;
        }

        private T WithConnection<T>(Func<SqlConnection, T> action)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Set context on every pooled connection, including reads, to prevent actor leakage.
                using (var actorCommand = new SqlCommand("EXEC sys.sp_set_session_context @key=N'ashkan_actor', @value=@actor", connection))
                {
                    actorCommand.Parameters.Add(P("@actor", SqlDbType.NVarChar,
                        string.IsNullOrWhiteSpace(AppSession.UserName) ? (object)DBNull.Value : AppSession.UserName, 256));
                    actorCommand.ExecuteNonQuery();
                }
                return action(connection);
            }
        }
        private static SqlParameter P(string name, SqlDbType type, object value, int size = 0)
        {
            var parameter = new SqlParameter(name, type);
            if (size > 0) parameter.Size = size;
            parameter.Value = value ?? DBNull.Value;
            return parameter;
        }
        private static int Execute(SqlConnection connection, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters);
                return command.ExecuteNonQuery();
            }
        }
        private static void RequireAffected(int affected, string entity)
        {
            if (affected != 1) throw new DBConcurrencyException(entity + " no longer exists or was changed.");
        }
        // Read-only audit history; SQL permissions should grant SELECT only to audit readers.
        internal DataTable GetAuditHistory(int maxRows = 200)
        {
            if (maxRows < 1 || maxRows > 1000) throw new ArgumentOutOfRangeException("maxRows");
            return WithConnection(connection =>
            {
                var table = new DataTable();
                using (var command = new SqlCommand(@"SELECT TOP (@limit) AuditId,EntityName,Operation,RecordId,
                    OccurredAtUtc,SqlLogin,Actor,OldValues,NewValues
                    FROM dbo.LmsRelationalAudit ORDER BY AuditId DESC", connection))
                {
                    command.Parameters.Add(P("@limit", SqlDbType.Int, maxRows));
                    using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                }
                return table;
            });
        }
        // All filters are optional and parameterized; date bounds use UTC and an exclusive upper bound.
        internal DataTable SearchAuditHistory(string entity, string actor, DateTime? fromUtc, DateTime? untilUtcExclusive, int maxRows = 200)
        {
            AuthorizationService.RequireAccountAdmin();
            if (maxRows < 1 || maxRows > 1000) throw new ArgumentOutOfRangeException("maxRows");
            if (fromUtc.HasValue && untilUtcExclusive.HasValue && fromUtc.Value >= untilUtcExclusive.Value)
                throw new ArgumentException("Invalid audit date range.");
            return WithConnection(connection =>
            {
                var table = new DataTable();
                using (var command = new SqlCommand(@"SELECT TOP (@limit) AuditId,EntityName,Operation,RecordId,
                    OccurredAtUtc,SqlLogin,Actor,OldValues,NewValues
                    FROM dbo.LmsRelationalAudit
                    WHERE (@entity IS NULL OR EntityName=@entity)
                      AND (@actor IS NULL OR Actor=@actor)
                      AND (@from IS NULL OR OccurredAtUtc>=@from)
                      AND (@until IS NULL OR OccurredAtUtc<@until)
                    ORDER BY AuditId DESC", connection))
                {
                    command.Parameters.Add(P("@limit", SqlDbType.Int, maxRows));
                    command.Parameters.Add(P("@entity", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(entity) ? null : entity.Trim(), 64));
                    command.Parameters.Add(P("@actor", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(actor) ? null : actor.Trim(), 256));
                    command.Parameters.Add(P("@from", SqlDbType.DateTime2, fromUtc.HasValue ? (object)fromUtc.Value : null));
                    command.Parameters.Add(P("@until", SqlDbType.DateTime2, untilUtcExclusive.HasValue ? (object)untilUtcExclusive.Value : null));
                    using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                }
                return table;
            });
        }
        // Aggregates are computed on SQL Server over the full selected window, not the 200-row audit preview.
        // UTC boundaries are [from, until); no raw row contents or personal data are returned.
        internal DataSet GetAuditDashboard(DateTime fromUtc, DateTime untilUtcExclusive)
        {
            AuthorizationService.RequireAccountAdmin();
            if (fromUtc >= untilUtcExclusive || (untilUtcExclusive - fromUtc).TotalDays > 366)
                throw new ArgumentException("Choose a valid UTC interval of at most 366 days.");
            return WithConnection(connection =>
            {
                var result = new DataSet("AuditDashboard");
                string[] queries = {
                    @"SELECT CONVERT(date,OccurredAtUtc) AS [DayUTC], COUNT_BIG(*) AS [Events]
                      FROM dbo.LmsRelationalAudit WHERE OccurredAtUtc>=@from AND OccurredAtUtc<@until
                      GROUP BY CONVERT(date,OccurredAtUtc) ORDER BY [DayUTC]",
                    @"SELECT EntityName, Operation, COUNT_BIG(*) AS [Events]
                      FROM dbo.LmsRelationalAudit WHERE OccurredAtUtc>=@from AND OccurredAtUtc<@until
                      GROUP BY EntityName,Operation ORDER BY [Events] DESC",
                    @"SELECT TOP (15) COALESCE(NULLIF(Actor,N''),N'(unknown)') AS Actor, COUNT_BIG(*) AS [Events]
                      FROM dbo.LmsRelationalAudit WHERE OccurredAtUtc>=@from AND OccurredAtUtc<@until
                      GROUP BY COALESCE(NULLIF(Actor,N''),N'(unknown)') ORDER BY [Events] DESC",
                    @"SELECT CONVERT(date,OccurredAtUtc) AS [DayUTC],
                        COALESCE(NULLIF(Actor,N''),N'(unknown)') AS Actor,
                        COUNT_BIG(*) AS [Changes]
                      FROM dbo.LmsRelationalAudit WHERE OccurredAtUtc>=@from AND OccurredAtUtc<@until
                      GROUP BY CONVERT(date,OccurredAtUtc),COALESCE(NULLIF(Actor,N''),N'(unknown)')
                      HAVING COUNT_BIG(*) >= 25 ORDER BY [Changes] DESC"
                };
                string[] names = { "Daily", "Operations", "Actors", "HighVolume" };
                for (int i = 0; i < queries.Length; i++)
                {
                    using (var command = new SqlCommand(queries[i], connection))
                    {
                        command.CommandTimeout = 30;
                        command.Parameters.Add(P("@from", SqlDbType.DateTime2, fromUtc));
                        command.Parameters.Add(P("@until", SqlDbType.DateTime2, untilUtcExclusive));
                        var table = new DataTable(names[i]);
                        using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                        result.Tables.Add(table);
                    }
                }
                return result;
            });
        }

        // Filtered dashboard: actor filter is applied to every aggregate, not only the chart.
        internal DataSet GetAuditDashboardForActor(DateTime fromUtc, DateTime untilUtcExclusive, string actor)
        {
            AuthorizationService.RequireAccountAdmin();
            if (string.IsNullOrWhiteSpace(actor)) return GetAuditDashboard(fromUtc, untilUtcExclusive);
            if (fromUtc >= untilUtcExclusive || (untilUtcExclusive - fromUtc).TotalDays > 366)
                throw new ArgumentException("Choose a valid UTC interval of at most 366 days.");
            actor = actor.Trim();
            if (actor.Length > 256) throw new ArgumentException("Actor filter is too long.");
            return WithConnection(connection =>
            {
                var result = new DataSet("FilteredAuditDashboard");
                string[] queries = {
                    @"SELECT CONVERT(date,OccurredAtUtc) AS [DayUTC], COUNT_BIG(*) AS [Events]
                      FROM dbo.LmsRelationalAudit WHERE OccurredAtUtc>=@from AND OccurredAtUtc<@until AND Actor=@actor
                      GROUP BY CONVERT(date,OccurredAtUtc) ORDER BY [DayUTC]",
                    @"SELECT EntityName, Operation, COUNT_BIG(*) AS [Events]
                      FROM dbo.LmsRelationalAudit WHERE OccurredAtUtc>=@from AND OccurredAtUtc<@until AND Actor=@actor
                      GROUP BY EntityName,Operation ORDER BY [Events] DESC",
                    @"SELECT TOP (15) COALESCE(NULLIF(Actor,N''),N'(unknown)') AS Actor, COUNT_BIG(*) AS [Events]
                      FROM dbo.LmsRelationalAudit WHERE OccurredAtUtc>=@from AND OccurredAtUtc<@until AND Actor=@actor
                      GROUP BY COALESCE(NULLIF(Actor,N''),N'(unknown)') ORDER BY [Events] DESC",
                    @"SELECT CONVERT(date,OccurredAtUtc) AS [DayUTC],
                        COALESCE(NULLIF(Actor,N''),N'(unknown)') AS Actor, COUNT_BIG(*) AS [Changes]
                      FROM dbo.LmsRelationalAudit WHERE OccurredAtUtc>=@from AND OccurredAtUtc<@until AND Actor=@actor
                      GROUP BY CONVERT(date,OccurredAtUtc),COALESCE(NULLIF(Actor,N''),N'(unknown)')
                      HAVING COUNT_BIG(*) >= 25 ORDER BY [Changes] DESC"
                };
                string[] names = { "Daily", "Operations", "Actors", "HighVolume" };
                for (int i = 0; i < queries.Length; i++)
                {
                    using (var command = new SqlCommand(queries[i], connection))
                    {
                        command.CommandTimeout = 30;
                        command.Parameters.Add(P("@from", SqlDbType.DateTime2, fromUtc));
                        command.Parameters.Add(P("@until", SqlDbType.DateTime2, untilUtcExclusive));
                        command.Parameters.Add(P("@actor", SqlDbType.NVarChar, actor, 256));
                        var table = new DataTable(names[i]);
                        using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                        result.Tables.Add(table);
                    }
                }
                return result;
            });
        }

        // Stage 47: administrator-only review history. Filters use parameters and UTC bounds.
        internal DataTable SearchAuditReviewHistory(long? auditId, string reviewer, string status,
            DateTime? fromUtc, DateTime? untilUtcExclusive, int maxRows = 500)
        {
            AuthorizationService.RequireAccountAdmin();
            if (maxRows < 1 || maxRows > 2000) throw new ArgumentOutOfRangeException("maxRows");
            if (fromUtc.HasValue && untilUtcExclusive.HasValue && fromUtc.Value >= untilUtcExclusive.Value)
                throw new ArgumentException("Invalid UTC date range.");
            return WithConnection(connection =>
            {
                var result = new DataTable("ReviewHistory");
                using (var command = new SqlCommand(@"SELECT TOP (@limit) ReviewHistoryId,AuditId,ChangeType,
                    PreviousStatus,NewStatus,PreviousReviewer,NewReviewer,PreviousNote,NewNote,
                    ChangedAtUtc,SqlLogin
                    FROM dbo.LmsAuditReviewHistory
                    WHERE (@id IS NULL OR AuditId=@id)
                      AND (@reviewer IS NULL OR NewReviewer=@reviewer)
                      AND (@status IS NULL OR NewStatus=@status)
                      AND (@from IS NULL OR ChangedAtUtc>=@from)
                      AND (@until IS NULL OR ChangedAtUtc<@until)
                    ORDER BY ReviewHistoryId DESC", connection))
                {
                    command.Parameters.Add(P("@limit", SqlDbType.Int, maxRows));
                    command.Parameters.Add(P("@id", SqlDbType.BigInt, auditId.HasValue ? (object)auditId.Value : DBNull.Value));
                    command.Parameters.Add(P("@reviewer", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(reviewer) ? (object)DBNull.Value : reviewer.Trim(), 256));
                    command.Parameters.Add(P("@status", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(status) ? (object)DBNull.Value : status, 20));
                    command.Parameters.Add(P("@from", SqlDbType.DateTime2, fromUtc.HasValue ? (object)fromUtc.Value : DBNull.Value));
                    command.Parameters.Add(P("@until", SqlDbType.DateTime2, untilUtcExclusive.HasValue ? (object)untilUtcExclusive.Value : DBNull.Value));
                    using (var adapter = new SqlDataAdapter(command)) adapter.Fill(result);
                }
                return result;
            });
        }

        // Review decisions are separate from immutable source audit events.
        // Stage 48: aggregate the entire selected period, not a truncated preview.
        // Stage 49: read-only, administrator-scoped threshold rule. No alerts are persisted or sent.
        // Stage 50: persistent configuration, not an automatic alert service.
        internal int GetSavedDailyDeletionThreshold()
        {
            AuthorizationService.RequireAccountAdmin();
            return WithConnection(connection =>
            {
                using (var command = new SqlCommand(
                    "SELECT MinimumDailyDeletes FROM dbo.LmsDeletionThresholdRules WHERE RuleKey=N'DailyDeletionCount'", connection))
                {
                    object value = command.ExecuteScalar();
                    return value == null || value == DBNull.Value ? 25 : Convert.ToInt32(value);
                }
            });
        }

        internal void SaveDailyDeletionThreshold(int minimum)
        {
            AuthorizationService.RequireAccountAdmin();
            if (minimum < 1 || minimum > 10000) throw new ArgumentOutOfRangeException("minimum");
            if (string.IsNullOrWhiteSpace(AppSession.UserName)) throw new InvalidOperationException("Sign in to save a rule.");
            WithConnection(connection =>
            {
                // The unique RuleKey and transaction lock prevent duplicate initial inserts.
                const string sql = @"SET XACT_ABORT ON;
                    BEGIN TRANSACTION;
                    UPDATE dbo.LmsDeletionThresholdRules WITH (UPDLOCK, SERIALIZABLE)
                       SET MinimumDailyDeletes=@minimum,UpdatedBy=@actor,UpdatedAtUtc=SYSUTCDATETIME()
                     WHERE RuleKey=N'DailyDeletionCount';
                    IF @@ROWCOUNT=0
                        INSERT INTO dbo.LmsDeletionThresholdRules(RuleKey,MinimumDailyDeletes,UpdatedBy)
                        VALUES(N'DailyDeletionCount',@minimum,@actor);
                    COMMIT TRANSACTION;";
                Execute(connection, sql, P("@minimum", SqlDbType.Int, minimum),
                    P("@actor", SqlDbType.NVarChar, AppSession.UserName, 256));
                return 0;
            });
        }

        internal DataTable GetHighVolumeDeletionSignals(DateTime fromUtc, DateTime untilUtcExclusive,
            string actor, int dailyThreshold)
        {
            AuthorizationService.RequireAccountAdmin();
            if (dailyThreshold < 1 || dailyThreshold > 10000)
                throw new ArgumentOutOfRangeException("dailyThreshold");
            if (fromUtc >= untilUtcExclusive || (untilUtcExclusive - fromUtc).TotalDays > 366)
                throw new ArgumentException("Select a valid UTC period of at most 366 days.");
            return WithConnection(connection =>
            {
                var table = new DataTable("HighVolumeDeletionSignals");
                using (var command = new SqlCommand(@"SELECT CAST(OccurredAtUtc AS date) AS UtcDay,
                    COALESCE(NULLIF(Actor,N''),N'(unknown)') AS ActorName,
                    COUNT_BIG(*) AS DeletionCount
                    FROM dbo.LmsRelationalAudit
                    WHERE Operation=N'DELETE' AND OccurredAtUtc>=@from AND OccurredAtUtc<@until
                      AND (@actor IS NULL OR Actor=@actor)
                    GROUP BY CAST(OccurredAtUtc AS date), COALESCE(NULLIF(Actor,N''),N'(unknown)')
                    HAVING COUNT_BIG(*) >= @threshold
                    ORDER BY DeletionCount DESC, UtcDay DESC", connection))
                {
                    command.Parameters.Add(P("@from", SqlDbType.DateTime2, fromUtc));
                    command.Parameters.Add(P("@until", SqlDbType.DateTime2, untilUtcExclusive));
                    command.Parameters.Add(P("@actor", SqlDbType.NVarChar,
                        string.IsNullOrWhiteSpace(actor) ? null : actor.Trim(), 256));
                    command.Parameters.Add(P("@threshold", SqlDbType.Int, dailyThreshold));
                    using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                }
                return table;
            });
        }

        internal DataTable GetReviewStatusSummary(DateTime fromUtc, DateTime untilUtcExclusive, string actor)
        {
            AuthorizationService.RequireAccountAdmin();
            if (fromUtc >= untilUtcExclusive || (untilUtcExclusive - fromUtc).TotalDays > 366)
                throw new ArgumentException("Select a valid UTC period of at most 366 days.");
            return WithConnection(connection =>
            {
                var table = new DataTable("ReviewStatusSummary");
                using (var command = new SqlCommand(@"SELECT
                    CASE WHEN r.AuditId IS NULL THEN N'Unreviewed' ELSE r.ReviewStatus END AS ReviewStatus,
                    COUNT_BIG(*) AS EventCount
                    FROM dbo.LmsRelationalAudit a
                    LEFT JOIN dbo.LmsAuditReviews r ON r.AuditId=a.AuditId
                    WHERE a.Operation=N'DELETE' AND a.OccurredAtUtc>=@from AND a.OccurredAtUtc<@until
                      AND (@actor IS NULL OR a.Actor=@actor)
                    GROUP BY CASE WHEN r.AuditId IS NULL THEN N'Unreviewed' ELSE r.ReviewStatus END
                    ORDER BY EventCount DESC", connection))
                {
                    command.Parameters.Add(P("@from", SqlDbType.DateTime2, fromUtc));
                    command.Parameters.Add(P("@until", SqlDbType.DateTime2, untilUtcExclusive));
                    command.Parameters.Add(P("@actor", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(actor) ? null : actor.Trim(), 256));
                    using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                }
                return table;
            });
        }

        internal DataTable GetAuditReviewDecisions(DateTime fromUtc, DateTime untilUtcExclusive, string actor)
        {
            AuthorizationService.RequireAccountAdmin();
            if (fromUtc >= untilUtcExclusive || (untilUtcExclusive - fromUtc).TotalDays > 366)
                throw new ArgumentException("Invalid review interval.");
            if (actor != null && actor.Trim().Length > 256) throw new ArgumentException("Actor filter too long.");
            return WithConnection(connection =>
            {
                const string sql = @"SELECT TOP (300) a.AuditId,a.OccurredAtUtc,a.EntityName,a.RecordId,a.Actor,
                    a.SqlLogin,a.OldValues,r.ReviewStatus,r.Reviewer,r.ReviewNote,r.ReviewedAtUtc,r.RowVersion
                    FROM dbo.LmsRelationalAudit a LEFT JOIN dbo.LmsAuditReviews r ON r.AuditId=a.AuditId
                    WHERE a.OccurredAtUtc>=@from AND a.OccurredAtUtc<@until AND a.Operation=N'DELETE'
                    AND (@actor IS NULL OR a.Actor=@actor) ORDER BY a.AuditId DESC";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(P("@from", SqlDbType.DateTime2, fromUtc));
                    command.Parameters.Add(P("@until", SqlDbType.DateTime2, untilUtcExclusive));
                    command.Parameters.Add(P("@actor", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(actor) ? null : actor.Trim(), 256));
                    var table = new DataTable("AuditReviews");
                    using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                    return table;
                }
            });
        }
        internal void SaveAuditReview(long auditId, string status, string note, byte[] expectedVersion)
        {
            AuthorizationService.RequireAccountAdmin();
            if (auditId <= 0 || !new[] { "Open", "Investigating", "Resolved", "FalsePositive" }.Contains(status))
                throw new ArgumentException("Invalid review status or audit ID.");
            if (string.IsNullOrWhiteSpace(note) || note.Trim().Length > 2000)
                throw new ArgumentException("A review note (max 2000 characters) is required.");
            string reviewer = AppSession.UserName;
            if (string.IsNullOrWhiteSpace(reviewer) || reviewer.Length > 256)
                throw new InvalidOperationException("An authenticated reviewer is required.");
            WithConnection(connection =>
            {
                // Avoid a race between first-time reviewers by relying on the AuditId PK.
                // Existing decisions require the last observed rowversion.
                if (expectedVersion == null)
                {
                    using (var command = new SqlCommand(@"INSERT INTO dbo.LmsAuditReviews(AuditId,ReviewStatus,Reviewer,ReviewNote)
                        SELECT AuditId,@status,@reviewer,@note FROM dbo.LmsRelationalAudit
                        WHERE AuditId=@id AND Operation=N'DELETE'", connection))
                    {
                        command.Parameters.Add(P("@id", SqlDbType.BigInt, auditId));
                        command.Parameters.Add(P("@status", SqlDbType.NVarChar, status, 20));
                        command.Parameters.Add(P("@reviewer", SqlDbType.NVarChar, reviewer, 256));
                        command.Parameters.Add(P("@note", SqlDbType.NVarChar, note.Trim(), 2000));
                        RequireAffected(command.ExecuteNonQuery(), "Audit event");
                    }
                }
                else
                {
                    if (expectedVersion.Length != 8) throw new ArgumentException("Invalid review version.");
                    using (var command = new SqlCommand(@"UPDATE dbo.LmsAuditReviews SET ReviewStatus=@status,
                        Reviewer=@reviewer,ReviewNote=@note,ReviewedAtUtc=SYSUTCDATETIME()
                        WHERE AuditId=@id AND RowVersion=@version", connection))
                    {
                        command.Parameters.Add(P("@id", SqlDbType.BigInt, auditId));
                        command.Parameters.Add(P("@status", SqlDbType.NVarChar, status, 20));
                        command.Parameters.Add(P("@reviewer", SqlDbType.NVarChar, reviewer, 256));
                        command.Parameters.Add(P("@note", SqlDbType.NVarChar, note.Trim(), 2000));
                        command.Parameters.Add(P("@version", SqlDbType.Binary, expectedVersion, 8));
                        RequireAffected(command.ExecuteNonQuery(), "Audit review");
                    }
                }
                return 0;
            });
        }

        // Review signals only: these are not automatically classified as security incidents.
        internal DataTable GetAuditReviewSignals(DateTime fromUtc, DateTime untilUtcExclusive, string actor)
        {
            AuthorizationService.RequireAccountAdmin();
            if (fromUtc >= untilUtcExclusive || (untilUtcExclusive - fromUtc).TotalDays > 366)
                throw new ArgumentException("Choose a valid UTC interval of at most 366 days.");
            if (actor != null && actor.Trim().Length > 256) throw new ArgumentException("Actor filter is too long.");
            return WithConnection(connection =>
            {
                using (var command = new SqlCommand(@"SELECT TOP (300) AuditId, OccurredAtUtc, EntityName, Operation,
                    RecordId, Actor, SqlLogin, OldValues, NewValues
                    FROM dbo.LmsRelationalAudit
                    WHERE OccurredAtUtc >= @from AND OccurredAtUtc < @until
                      AND (@actor IS NULL OR Actor = @actor)
                      AND Operation = N'DELETE'
                    ORDER BY AuditId DESC", connection))
                {
                    command.CommandTimeout = 30;
                    command.Parameters.Add(P("@from", SqlDbType.DateTime2, fromUtc));
                    command.Parameters.Add(P("@until", SqlDbType.DateTime2, untilUtcExclusive));
                    command.Parameters.Add(P("@actor", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(actor) ? null : actor.Trim(), 256));
                    var table = new DataTable("DeletionReview");
                    using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                    return table;
                }
            });
        }

        internal List<Student> GetStudents()
        {
            return WithConnection(connection =>
            {
                var result = new List<Student>();
                using (var command = new SqlCommand("SELECT StudentId, FullName, Email, StatusKey, Progress FROM dbo.LmsStudents ORDER BY StudentId", connection))
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) result.Add(new Student { Id = reader.GetInt32(0), Name = reader.GetString(1), Email = reader.GetString(2), StatusKey = reader.GetString(3), Progress = reader.GetInt32(4) });
                return result;
            });
        }
        internal void InsertStudent(Student student)
        {
            if (student == null || student.Id <= 0 || string.IsNullOrWhiteSpace(student.Name) || string.IsNullOrWhiteSpace(student.Email) || student.Progress < 0 || student.Progress > 100)
                throw new ArgumentException("Invalid student.", "student");
            AuthorizationService.RequireAcademicWrite();
            WithConnection(c => Execute(c, "INSERT INTO dbo.LmsStudents(StudentId,FullName,Email,StatusKey,Progress) VALUES(@id,@name,@email,@status,@progress)",
                P("@id", SqlDbType.Int, student.Id), P("@name", SqlDbType.NVarChar, student.Name, 120), P("@email", SqlDbType.NVarChar, student.Email, 254), P("@status", SqlDbType.NVarChar, student.StatusKey ?? "Active", 40), P("@progress", SqlDbType.Int, student.Progress)));
        }
        internal void UpdateStudent(Student student)
        {
            if (student == null || student.Id <= 0 || string.IsNullOrWhiteSpace(student.Name) || string.IsNullOrWhiteSpace(student.Email) || student.Progress < 0 || student.Progress > 100)
                throw new ArgumentException("Invalid student.", "student");
            AuthorizationService.RequireAcademicWrite();
            WithConnection(c => { RequireAffected(Execute(c, "UPDATE dbo.LmsStudents SET FullName=@name,Email=@email,StatusKey=@status,Progress=@progress WHERE StudentId=@id",
                P("@id", SqlDbType.Int, student.Id), P("@name", SqlDbType.NVarChar, student.Name, 120), P("@email", SqlDbType.NVarChar, student.Email, 254), P("@status", SqlDbType.NVarChar, student.StatusKey ?? "Active", 40), P("@progress", SqlDbType.Int, student.Progress)), "Student"); return 0; });
        }
        // Optimistic concurrency: the version must be captured before the user begins editing.
        internal byte[] GetStudentVersion(int id)
        {
            return WithConnection(c =>
            {
                using (var cmd = new SqlCommand("SELECT RowVersion FROM dbo.LmsStudents WHERE StudentId=@id", c))
                {
                    cmd.Parameters.Add(P("@id", SqlDbType.Int, id));
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) throw new DBConcurrencyException("Student no longer exists.");
                    return (byte[])result;
                }
            });
        }
        internal void UpdateStudentChecked(Student student, byte[] expectedVersion)
        {
            AuthorizationService.RequireAcademicWrite();
            if (student == null || student.Id <= 0 || string.IsNullOrWhiteSpace(student.Name) ||
                string.IsNullOrWhiteSpace(student.Email) || student.Progress < 0 || student.Progress > 100)
                throw new ArgumentException("Invalid student.");
            if (expectedVersion == null || expectedVersion.Length != 8)
                throw new ArgumentException("A valid original row version is required.", "expectedVersion");
            WithConnection(c =>
            {
                RequireAffected(Execute(c, @"UPDATE dbo.LmsStudents
                    SET FullName=@name, Email=@email, StatusKey=@status, Progress=@progress
                    WHERE StudentId=@id AND RowVersion=@version",
                    P("@id", SqlDbType.Int, student.Id),
                    P("@name", SqlDbType.NVarChar, student.Name.Trim(), 120),
                    P("@email", SqlDbType.NVarChar, student.Email.Trim(), 254),
                    P("@status", SqlDbType.NVarChar, student.StatusKey ?? "Active", 40),
                    P("@progress", SqlDbType.Int, student.Progress),
                    P("@version", SqlDbType.Timestamp, expectedVersion)), "Student (concurrent modification detected)");
                return 0;
            });
        }
        internal byte[] GetCourseVersion(int id)
        {
            return WithConnection(c =>
            {
                using (var cmd = new SqlCommand("SELECT RowVersion FROM dbo.LmsCourses WHERE CourseId=@id", c))
                {
                    cmd.Parameters.Add(P("@id", SqlDbType.Int, id));
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) throw new DBConcurrencyException("Course no longer exists.");
                    return (byte[])result;
                }
            });
        }
        internal void UpdateCourseChecked(int id, string title, int? instructorId, string status, int progress, byte[] expectedVersion)
        {
            AuthorizationService.RequireAcademicWrite();
            if (id <= 0 || string.IsNullOrWhiteSpace(title) || progress < 0 || progress > 100 ||
                expectedVersion == null || expectedVersion.Length != 8) throw new ArgumentException("Invalid course or row version.");
            WithConnection(c =>
            {
                RequireAffected(Execute(c, @"UPDATE dbo.LmsCourses SET Title=@title, InstructorId=@instructor,
                    StatusKey=@status, Progress=@progress WHERE CourseId=@id AND RowVersion=@version",
                    P("@id", SqlDbType.Int, id), P("@title", SqlDbType.NVarChar, title.Trim(), 160),
                    P("@instructor", SqlDbType.Int, instructorId), P("@status", SqlDbType.NVarChar, status, 40),
                    P("@progress", SqlDbType.Int, progress), P("@version", SqlDbType.Timestamp, expectedVersion)),
                    "Course (concurrent modification detected)");
                return 0;
            });
        }
        // Versions are read before the editing dialog and compared atomically on UPDATE.
        private byte[] ReadVersion(string table, string key, int id)
        {
            return WithConnection(c =>
            {
                using (var cmd = new SqlCommand("SELECT RowVersion FROM dbo." + table + " WHERE " + key + "=@id", c))
                {
                    cmd.Parameters.Add(P("@id", SqlDbType.Int, id));
                    var value = cmd.ExecuteScalar();
                    if (value == null || value == DBNull.Value) throw new DBConcurrencyException("Record was deleted.");
                    return (byte[])value;
                }
            });
        }
        internal byte[] GetInstructorVersion(int id) { return ReadVersion("LmsInstructors", "InstructorId", id); }
        internal byte[] GetEnrollmentVersion(int id) { return ReadVersion("LmsEnrollments", "EnrollmentId", id); }
        private static void ValidateVersion(byte[] version)
        {
            if (version == null || version.Length != 8) throw new ArgumentException("Original rowversion is required.", "version");
        }
        internal void UpdateInstructorChecked(int id, string name, string email, string expertise, byte[] version)
        {
            AuthorizationService.RequireAcademicWrite();
            ValidateInstructor(id, name, email);
            ValidateVersion(version);
            WithConnection(c => { RequireAffected(Execute(c, @"UPDATE dbo.LmsInstructors SET FullName=@name, Email=@email, Expertise=@expertise
                WHERE InstructorId=@id AND RowVersion=@version",
                P("@id", SqlDbType.Int, id), P("@name", SqlDbType.NVarChar, name.Trim(), 120),
                P("@email", SqlDbType.NVarChar, email.Trim(), 254), P("@expertise", SqlDbType.NVarChar, expertise ?? "", 160),
                P("@version", SqlDbType.Timestamp, version)), "Instructor (concurrent edit)"); return 0; });
        }
        internal void SetEnrollmentStatusChecked(int id, string status, byte[] version)
        {
            AuthorizationService.RequireAcademicWrite();
            if (id <= 0 || string.IsNullOrWhiteSpace(status) || status.Length > 40) throw new ArgumentException("Invalid enrollment status.");
            ValidateVersion(version);
            WithConnection(c => { RequireAffected(Execute(c, @"UPDATE dbo.LmsEnrollments SET StatusKey=@status
                WHERE EnrollmentId=@id AND RowVersion=@version", P("@id", SqlDbType.Int, id),
                P("@status", SqlDbType.NVarChar, status.Trim(), 40), P("@version", SqlDbType.Timestamp, version)),
                "Enrollment (concurrent edit)"); return 0; });
        }
        internal void SetCourseInstructorChecked(int courseId, int? instructorId, byte[] version)
        {
            AuthorizationService.RequireAcademicWrite();
            if (courseId <= 0) throw new ArgumentException("Invalid course ID.");
            ValidateVersion(version);
            WithConnection(c => { RequireAffected(Execute(c, @"UPDATE dbo.LmsCourses SET InstructorId=@instructor
                WHERE CourseId=@id AND RowVersion=@version", P("@id", SqlDbType.Int, courseId),
                P("@instructor", SqlDbType.Int, instructorId), P("@version", SqlDbType.Timestamp, version)),
                "Course (concurrent edit)"); return 0; });
        }
        internal void DeleteStudent(int id)
        {
            AuthorizationService.RequireAcademicWrite();
            WithConnection(c => { RequireAffected(Execute(c, "DELETE FROM dbo.LmsStudents WHERE StudentId=@id", P("@id", SqlDbType.Int, id)), "Student"); return 0; });
        }
        internal List<Course> GetCourses()
        {
            return WithConnection(connection =>
            {
                var result = new List<Course>();
                const string sql = @"SELECT c.CourseId,c.Title,COALESCE(i.FullName,N''),c.StatusKey,c.Progress,
                    (SELECT COUNT(*) FROM dbo.LmsEnrollments e WHERE e.CourseId=c.CourseId)
                    FROM dbo.LmsCourses c LEFT JOIN dbo.LmsInstructors i ON i.InstructorId=c.InstructorId ORDER BY c.CourseId";
                using (var command = new SqlCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) result.Add(new Course { Id = reader.GetInt32(0), Title = reader.GetString(1), Instructor = reader.GetString(2), StatusKey = reader.GetString(3), Progress = reader.GetInt32(4), Students = reader.GetInt32(5) });
                return result;
            });
        }
        internal void InsertCourse(int id, string title, int? instructorId, string status = "Draft")
        {
            AuthorizationService.RequireAcademicWrite();
            if (id <= 0 || string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Invalid course.");
            WithConnection(c => Execute(c, "INSERT INTO dbo.LmsCourses(CourseId,Title,InstructorId,StatusKey,Progress) VALUES(@id,@title,@instructor,@status,0)",
                P("@id", SqlDbType.Int, id), P("@title", SqlDbType.NVarChar, title, 160), P("@instructor", SqlDbType.Int, instructorId), P("@status", SqlDbType.NVarChar, status, 40)));
        }
        internal void UpdateCourse(int id, string title, int? instructorId, string status, int progress)
        {
            AuthorizationService.RequireAcademicWrite();
            if (id <= 0 || string.IsNullOrWhiteSpace(title) || progress < 0 || progress > 100) throw new ArgumentException("Invalid course.");
            WithConnection(c => { RequireAffected(Execute(c, "UPDATE dbo.LmsCourses SET Title=@title,InstructorId=@instructor,StatusKey=@status,Progress=@progress WHERE CourseId=@id",
                P("@id", SqlDbType.Int, id), P("@title", SqlDbType.NVarChar, title, 160), P("@instructor", SqlDbType.Int, instructorId), P("@status", SqlDbType.NVarChar, status, 40), P("@progress", SqlDbType.Int, progress)), "Course"); return 0; });
        }
        internal void DeleteCourse(int id)
        {
            AuthorizationService.RequireAcademicWrite();
            WithConnection(c => { RequireAffected(Execute(c, "DELETE FROM dbo.LmsCourses WHERE CourseId=@id", P("@id", SqlDbType.Int, id)), "Course"); return 0; });
        }
        internal DataTable GetEnrollments()
        {
            return WithConnection(c =>
            {
                var table = new DataTable();
                using (var command = new SqlCommand(@"SELECT e.EnrollmentId,e.StudentId,s.FullName AS StudentName,e.CourseId,c.Title AS CourseTitle,e.EnrolledAt,e.StatusKey FROM dbo.LmsEnrollments e INNER JOIN dbo.LmsStudents s ON s.StudentId=e.StudentId INNER JOIN dbo.LmsCourses c ON c.CourseId=e.CourseId ORDER BY e.EnrollmentId", c))
                using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                return table;
            });
        }
        internal DataTable GetInstructors()
        {
            return WithConnection(c =>
            {
                var table = new DataTable();
                using (var command = new SqlCommand("SELECT InstructorId,FullName,Email FROM dbo.LmsInstructors ORDER BY FullName", c))
                using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                return table;
            });
        }
        internal void InsertInstructor(int id, string name, string email, string expertise)
        {
            AuthorizationService.RequireAcademicWrite();
            ValidateInstructor(id, name, email);
            WithConnection(c => Execute(c, "INSERT INTO dbo.LmsInstructors(InstructorId,FullName,Email,Expertise) VALUES(@id,@name,@email,@expertise)",
                P("@id", SqlDbType.Int, id), P("@name", SqlDbType.NVarChar, name.Trim(), 120),
                P("@email", SqlDbType.NVarChar, email.Trim(), 254), P("@expertise", SqlDbType.NVarChar, expertise, 160)));
        }
        internal void UpdateInstructor(int id, string name, string email, string expertise)
        {
            AuthorizationService.RequireAcademicWrite();
            ValidateInstructor(id, name, email);
            WithConnection(c => { RequireAffected(Execute(c, "UPDATE dbo.LmsInstructors SET FullName=@name,Email=@email,Expertise=@expertise WHERE InstructorId=@id",
                P("@id", SqlDbType.Int, id), P("@name", SqlDbType.NVarChar, name.Trim(), 120),
                P("@email", SqlDbType.NVarChar, email.Trim(), 254), P("@expertise", SqlDbType.NVarChar, expertise, 160)), "Instructor"); return 0; });
        }
        internal void DeleteInstructor(int id)
        {
            AuthorizationService.RequireAcademicWrite();
            if (id <= 0) throw new ArgumentException("Invalid instructor ID.");
            WithConnection(c => { RequireAffected(Execute(c, "DELETE FROM dbo.LmsInstructors WHERE InstructorId=@id", P("@id", SqlDbType.Int, id)), "Instructor"); return 0; });
        }
        private static void ValidateInstructor(int id, string name, string email)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120 ||
                string.IsNullOrWhiteSpace(email) || email.Trim().Length > 254 || !email.Contains("@"))
                throw new ArgumentException("Invalid instructor details.");
        }
        internal void SetCourseInstructor(int courseId, int? instructorId)
        {
            AuthorizationService.RequireAcademicWrite();
            if (courseId <= 0 || (instructorId.HasValue && instructorId.Value <= 0)) throw new ArgumentException("Invalid identifiers.");
            WithConnection(c => { RequireAffected(Execute(c, "UPDATE dbo.LmsCourses SET InstructorId=@instructor WHERE CourseId=@id", P("@id",SqlDbType.Int,courseId),P("@instructor",SqlDbType.Int,instructorId)), "Course"); return 0; });
        }
        internal void Enroll(int enrollmentId, int studentId, int courseId, DateTime enrolledAt)
        {
            AuthorizationService.RequireAcademicWrite();
            if (enrollmentId <= 0 || studentId <= 0 || courseId <= 0) throw new ArgumentException("Invalid enrollment identifiers.");
            WithConnection(c => Execute(c, "INSERT INTO dbo.LmsEnrollments(EnrollmentId,StudentId,CourseId,EnrolledAt,StatusKey) VALUES(@id,@student,@course,@date,N'Active')",
                P("@id", SqlDbType.Int, enrollmentId), P("@student", SqlDbType.Int, studentId), P("@course", SqlDbType.Int, courseId), P("@date", SqlDbType.DateTime2, enrolledAt)));
        }
        internal void SetEnrollmentStatus(int id, string status)
        {
            AuthorizationService.RequireAcademicWrite();
            if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Status is required.", "status");
            WithConnection(c => { RequireAffected(Execute(c, "UPDATE dbo.LmsEnrollments SET StatusKey=@status WHERE EnrollmentId=@id", P("@id", SqlDbType.Int, id), P("@status", SqlDbType.NVarChar, status, 40)), "Enrollment"); return 0; });
        }
        internal void RemoveEnrollment(int id)
        {
            AuthorizationService.RequireAcademicWrite();
            WithConnection(c => { RequireAffected(Execute(c, "DELETE FROM dbo.LmsEnrollments WHERE EnrollmentId=@id", P("@id", SqlDbType.Int, id)), "Enrollment"); return 0; });
        }
    }
}
