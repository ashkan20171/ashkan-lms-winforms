using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace AshkanLMS.Services
{
    /// <summary>Explicit, one-time migration of the core snapshot entities into empty relational tables.
    /// Never runs during application startup and never deletes existing SQL records.</summary>
    internal sealed class RelationalMigrationService
    {
        private readonly string connectionString;
        internal RelationalMigrationService(string connectionString) { this.connectionString = connectionString ?? throw new ArgumentNullException("connectionString"); }

        // Read-only preflight: no database connection or mutation is performed.
        internal MigrationPreview PreviewCore(LmsState state)
        {
            LmsDataIntegrity.Validate(state);
            if (state.Instructors == null || state.Enrollments == null)
                throw new InvalidOperationException("Instructors and enrollments are required.");
            if (state.Instructors.Any(x => x == null || x.Id <= 0 || string.IsNullOrWhiteSpace(x.Name) || string.IsNullOrWhiteSpace(x.Email)))
                throw new InvalidOperationException("Invalid instructor record.");
            if (state.Instructors.Select(x => x.Id).Distinct().Count() != state.Instructors.Count ||
                state.Instructors.Select(x => x.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count() != state.Instructors.Count)
                throw new InvalidOperationException("Duplicate instructor ID or email.");
            var instructorById = state.Instructors.ToDictionary(x => x.Id);
            var studentById = state.Students.ToDictionary(x => x.Id);
            var courseById = state.Courses.ToDictionary(x => x.Id);
            var instructorNames = state.Instructors.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
            var studentNames = state.Students.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
            var courseTitles = state.Courses.GroupBy(x => x.Title, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
            var preview = new MigrationPreview();
            foreach (var c in state.Courses)
            {
                int id = c.InstructorId;
                if (id == 0)
                {
                    List<Models.Instructor> matches;
                    if (!instructorNames.TryGetValue(c.Instructor ?? "", out matches) || matches.Count != 1)
                        throw new InvalidOperationException("Course instructor name is missing or ambiguous: " + c.Title);
                    id = matches[0].Id;
                }
                if (!instructorById.ContainsKey(id)) throw new InvalidOperationException("Invalid instructor ID for course " + c.Id);
                preview.CourseInstructors.Add(c.Id, id);
            }
            foreach (var e in state.Enrollments)
            {
                if (e == null || e.Id <= 0) throw new InvalidOperationException("Invalid enrollment.");
                int studentId = e.StudentId, courseId = e.CourseId;
                if (studentId == 0)
                {
                    List<Models.Student> matches;
                    if (!studentNames.TryGetValue(e.Student ?? "", out matches) || matches.Count != 1)
                        throw new InvalidOperationException("Student name is missing or ambiguous for enrollment " + e.Id);
                    studentId = matches[0].Id;
                }
                if (courseId == 0)
                {
                    List<Models.Course> matches;
                    if (!courseTitles.TryGetValue(e.Course ?? "", out matches) || matches.Count != 1)
                        throw new InvalidOperationException("Course title is missing or ambiguous for enrollment " + e.Id);
                    courseId = matches[0].Id;
                }
                if (!studentById.ContainsKey(studentId) || !courseById.ContainsKey(courseId))
                    throw new InvalidOperationException("Unknown enrollment reference " + e.Id);
                preview.EnrollmentStudents.Add(e.Id, studentId);
                preview.EnrollmentCourses.Add(e.Id, courseId);
            }
            if (state.Enrollments.Select(x => x.Id).Distinct().Count() != state.Enrollments.Count ||
                state.Enrollments.Select(x => preview.EnrollmentStudents[x.Id] + ":" + preview.EnrollmentCourses[x.Id]).Distinct().Count() != state.Enrollments.Count)
                throw new InvalidOperationException("Duplicate enrollment IDs or student/course pairs.");
            // Resolve dependent records only through an unambiguous enrollment.
            if (state.Enrollments.Select(x => preview.EnrollmentStudents[x.Id] + ":" + preview.EnrollmentCourses[x.Id]).Distinct().Count() != state.Enrollments.Count)
                throw new InvalidOperationException("Duplicate enrollment pairs.");
            var enrollmentPairs = preview.EnrollmentStudents.Keys.ToDictionary(
                id => preview.EnrollmentStudents[id] + ":" + preview.EnrollmentCourses[id], id => id);
            ResolveDependents(state.Attendance, x => x.Id, x => x.StudentId, x => x.CourseId,
                x => x.Student, x => x.Course, studentNames, courseTitles, studentById, courseById,
                enrollmentPairs, preview.AttendanceEnrollments, "attendance");
            ResolveDependents(state.Grades, x => x.Id, x => x.StudentId, x => x.CourseId,
                x => x.Student, x => x.Course, studentNames, courseTitles, studentById, courseById,
                enrollmentPairs, preview.GradeEnrollments, "grade");
            ResolveDependents(state.Certificates, x => x.Id, x => x.StudentId, x => x.CourseId,
                x => x.Student, x => x.Course, studentNames, courseTitles, studentById, courseById,
                enrollmentPairs, preview.CertificateEnrollments, "certificate");
            if (state.Attendance.GroupBy(x => preview.AttendanceEnrollments[x.Id] + ":" + x.Date.Date.ToString("yyyyMMdd")).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Duplicate attendance for enrollment/date.");
            if (state.Grades.GroupBy(x => preview.GradeEnrollments[x.Id]).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Duplicate grade for enrollment.");
            if (state.Certificates.GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1) ||
                state.Certificates.GroupBy(x => preview.CertificateEnrollments[x.Id]).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Duplicate certificate code or enrollment.");
            if (state.Grades.Any(x => x.Score < 0 || x.Score > 20) || state.Certificates.Any(x => string.IsNullOrWhiteSpace(x.Code)))
                throw new InvalidOperationException("Invalid grade score or certificate code.");
            preview.Attendance = state.Attendance.Count;
            preview.Grades = state.Grades.Count;
            preview.Certificates = state.Certificates.Count;
            preview.Students = state.Students.Count;
            preview.Instructors = state.Instructors.Count;
            preview.Courses = state.Courses.Count;
            preview.Enrollments = state.Enrollments.Count;
            return preview;
        }

        // Opt-in import. Requires verified backups and a dedicated empty relational target.
        // A SQL transaction rolls back all inserts on failure; existing snapshot is untouched.
        internal void ImportCore(LmsState state)
        {
            var preview = PreviewCore(state); // fail closed before opening SQL connection
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        using (var guard = new SqlCommand(@"IF EXISTS(SELECT 1 FROM dbo.LmsStudents) OR EXISTS(SELECT 1 FROM dbo.LmsCourses) OR EXISTS(SELECT 1 FROM dbo.LmsInstructors) OR EXISTS(SELECT 1 FROM dbo.LmsEnrollments) OR EXISTS(SELECT 1 FROM dbo.LmsAttendance) OR EXISTS(SELECT 1 FROM dbo.LmsGrades) OR EXISTS(SELECT 1 FROM dbo.LmsCertificates) THROW 51001, 'Relational target must be entirely empty for first import.', 1;", connection, transaction))
                            guard.ExecuteNonQuery();
                        foreach (var i in state.Instructors)
                            Execute(connection, transaction, "INSERT dbo.LmsInstructors(InstructorId,FullName,Email,Expertise) VALUES(@id,@name,@email,@expertise)",
                                Param("@id", i.Id), Param("@name", i.Name), Param("@email", i.Email), Param("@expertise", i.Expertise));
                        foreach (var s in state.Students)
                            Execute(connection, transaction, "INSERT dbo.LmsStudents(StudentId,FullName,Email,StatusKey,Progress) VALUES(@id,@name,@email,@status,@progress)",
                                Param("@id", s.Id), Param("@name", s.Name), Param("@email", s.Email), Param("@status", s.StatusKey), Param("@progress", s.Progress));
                        foreach (var c in state.Courses)
                            Execute(connection, transaction, "INSERT dbo.LmsCourses(CourseId,Title,InstructorId,StatusKey,Progress) VALUES(@id,@title,@instructor,@status,@progress)",
                                Param("@id", c.Id), Param("@title", c.Title), Param("@instructor", preview.CourseInstructors[c.Id]), Param("@status", c.StatusKey), Param("@progress", c.Progress));
                        foreach (var e in state.Enrollments)
                            Execute(connection, transaction, "INSERT dbo.LmsEnrollments(EnrollmentId,StudentId,CourseId,EnrolledAt,StatusKey) VALUES(@id,@student,@course,@date,@status)",
                                Param("@id", e.Id), Param("@student", preview.EnrollmentStudents[e.Id]), Param("@course", preview.EnrollmentCourses[e.Id]), Param("@date", e.Date), Param("@status", e.StatusKey));
                        foreach (var a in state.Attendance)
                            Execute(connection, transaction, "INSERT dbo.LmsAttendance(AttendanceId,EnrollmentId,SessionDate,StatusKey) VALUES(@id,@enrollment,@date,@status)",
                                Param("@id", a.Id), Param("@enrollment", preview.AttendanceEnrollments[a.Id]), Param("@date", a.Date.Date), Param("@status", a.StatusKey));
                        foreach (var g in state.Grades)
                            Execute(connection, transaction, "INSERT dbo.LmsGrades(GradeId,EnrollmentId,Score,Letter) VALUES(@id,@enrollment,@score,@letter)",
                                Param("@id", g.Id), Param("@enrollment", preview.GradeEnrollments[g.Id]), Param("@score", g.Score), Param("@letter", g.Letter));
                        foreach (var c in state.Certificates)
                            Execute(connection, transaction, "INSERT dbo.LmsCertificates(CertificateId,EnrollmentId,IssuedAt,CertificateCode) VALUES(@id,@enrollment,@issued,@code)",
                                Param("@id", c.Id), Param("@enrollment", preview.CertificateEnrollments[c.Id]), Param("@issued", c.IssuedAt), Param("@code", c.Code));
                        transaction.Commit();
                    }
                    catch { transaction.Rollback(); throw; }
                }
            }
        }

        // Read-only verification after import. Compares exact primary-key sets, not just totals.
        // Does not switch the application storage mode or modify the target database.
        internal MigrationVerification VerifyTarget(LmsState state)
        {
            var expected = PreviewCore(state);
            var report = new MigrationVerification();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                VerifyIds(connection, report, "Students", "dbo.LmsStudents", "StudentId", state.Students.Select(x => x.Id));
                VerifyIds(connection, report, "Instructors", "dbo.LmsInstructors", "InstructorId", state.Instructors.Select(x => x.Id));
                VerifyIds(connection, report, "Courses", "dbo.LmsCourses", "CourseId", state.Courses.Select(x => x.Id));
                VerifyIds(connection, report, "Enrollments", "dbo.LmsEnrollments", "EnrollmentId", state.Enrollments.Select(x => x.Id));
                VerifyIds(connection, report, "Attendance", "dbo.LmsAttendance", "AttendanceId", state.Attendance.Select(x => x.Id));
                VerifyIds(connection, report, "Grades", "dbo.LmsGrades", "GradeId", state.Grades.Select(x => x.Id));
                VerifyIds(connection, report, "Certificates", "dbo.LmsCertificates", "CertificateId", state.Certificates.Select(x => x.Id));
                VerifyLinks(connection, report, "Course instructors", "SELECT CourseId,InstructorId FROM dbo.LmsCourses", expected.CourseInstructors);
                VerifyLinks(connection, report, "Enrollment students", "SELECT EnrollmentId,StudentId FROM dbo.LmsEnrollments", expected.EnrollmentStudents);
                VerifyLinks(connection, report, "Enrollment courses", "SELECT EnrollmentId,CourseId FROM dbo.LmsEnrollments", expected.EnrollmentCourses);
                VerifyLinks(connection, report, "Attendance enrollments", "SELECT AttendanceId,EnrollmentId FROM dbo.LmsAttendance", expected.AttendanceEnrollments);
                VerifyLinks(connection, report, "Grade enrollments", "SELECT GradeId,EnrollmentId FROM dbo.LmsGrades", expected.GradeEnrollments);
                VerifyLinks(connection, report, "Certificate enrollments", "SELECT CertificateId,EnrollmentId FROM dbo.LmsCertificates", expected.CertificateEnrollments);
                // Compare actual persisted field values, not merely identifiers or foreign keys.
                VerifyValues(connection, report, "Student details", "SELECT StudentId,FullName,Email,StatusKey,Progress FROM dbo.LmsStudents",
                    state.Students.ToDictionary(x => x.Id, x => Fields(x.Name, x.Email, x.StatusKey, x.Progress)));
                VerifyValues(connection, report, "Instructor details", "SELECT InstructorId,FullName,Email,Expertise FROM dbo.LmsInstructors",
                    state.Instructors.ToDictionary(x => x.Id, x => Fields(x.Name, x.Email, x.Expertise)));
                VerifyValues(connection, report, "Course details", "SELECT CourseId,Title,StatusKey,Progress FROM dbo.LmsCourses",
                    state.Courses.ToDictionary(x => x.Id, x => Fields(x.Title, x.StatusKey, x.Progress)));
                VerifyValues(connection, report, "Enrollment details", "SELECT EnrollmentId,EnrolledAt,StatusKey FROM dbo.LmsEnrollments",
                    state.Enrollments.ToDictionary(x => x.Id, x => Fields(x.Date, x.StatusKey)));
                VerifyValues(connection, report, "Attendance details", "SELECT AttendanceId,SessionDate,StatusKey FROM dbo.LmsAttendance",
                    state.Attendance.ToDictionary(x => x.Id, x => Fields(x.Date.Date, x.StatusKey)));
                VerifyValues(connection, report, "Grade details", "SELECT GradeId,Score,Letter FROM dbo.LmsGrades",
                    state.Grades.ToDictionary(x => x.Id, x => Fields(x.Score, x.Letter)));
                VerifyValues(connection, report, "Certificate details", "SELECT CertificateId,IssuedAt,CertificateCode FROM dbo.LmsCertificates",
                    state.Certificates.ToDictionary(x => x.Id, x => Fields(x.IssuedAt, x.Code)));
            }
            return report;
        }

        internal sealed class MigrationVerification
        {
            internal readonly List<VerificationRow> Rows = new List<VerificationRow>();
            internal readonly List<VerificationDetail> Details = new List<VerificationDetail>();
            internal bool Passed { get { return Rows.All(x => x.Missing == 0 && x.Extra == 0 && x.Mismatched == 0); } }
            // Advisory readiness only: never switches the application's active database.
            internal MigrationReadiness AssessReadiness()
            {
                int missing = Rows.Sum(x => x.Missing);
                int extra = Rows.Sum(x => x.Extra);
                int mismatched = Rows.Sum(x => x.Mismatched);
                int total = missing + extra + mismatched;
                return new MigrationReadiness {
                    Ready = Rows.Count >= 14 && total == 0,
                    CheckedSections = Rows.Count,
                    Missing = missing,
                    Extra = extra,
                    Mismatched = mismatched,
                    CriticalIssues = total,
                    Message = Rows.Count < 14 ? "Verification is incomplete; do not cut over." :
                        total > 0 ? "Discrepancies detected; do not cut over." :
                        "Verification passed. Backups, SQL constraints and a live restore test are still required before cutover."
                };
            }
            internal string ToCsv()
            {
                var lines = new List<string> { "Check,Expected,Actual,Missing,Extra,Mismatched" };
                foreach (var row in Rows)
                    lines.Add("\"" + row.Name.Replace("\"", "\"\"") + "\"," + row.Expected + "," + row.Actual + "," + row.Missing + "," + row.Extra + "," + row.Mismatched);
                lines.Add("");
                lines.Add("Check,RecordId,Issue,ExpectedValue,ActualValue");
                foreach (var detail in Details)
                    lines.Add(string.Join(",", new[] { detail.Check, detail.RecordId.ToString(CultureInfo.InvariantCulture), detail.Issue, detail.ExpectedValue, detail.ActualValue }.Select(CsvCell)));
                return string.Join(Environment.NewLine, lines);
            }
        }
        internal sealed class MigrationReadiness
        {
            internal bool Ready;
            internal int CheckedSections, Missing, Extra, Mismatched, CriticalIssues;
            internal string Message;
            internal string ToReport()
            {
                return "AshkanLMS migration readiness (advisory only)" + Environment.NewLine +
                    "Status: " + (Ready ? "Verification passed" : "Blocked") + Environment.NewLine +
                    "Sections: " + CheckedSections + Environment.NewLine +
                    "Missing: " + Missing + Environment.NewLine +
                    "Extra: " + Extra + Environment.NewLine +
                    "Mismatched: " + Mismatched + Environment.NewLine +
                    "Critical issues: " + CriticalIssues + Environment.NewLine + Message + Environment.NewLine +
                    "No automatic cutover was performed.";
            }
        }
        private static string CsvCell(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        }
        internal sealed class VerificationDetail
        {
            internal string Check, Issue, ExpectedValue, ActualValue;
            internal int RecordId;
        }
        internal sealed class VerificationRow
        {
            internal string Name;
            internal int Expected, Actual, Missing, Extra, Mismatched;
        }
        private static void VerifyIds(SqlConnection connection, MigrationVerification report, string name, string table, string idColumn, IEnumerable<int> expectedIds)
        {
            var expected = new HashSet<int>(expectedIds);
            var actual = new HashSet<int>();
            // All identifiers are fixed literals supplied by this class, never user input.
            using (var command = new SqlCommand("SELECT " + idColumn + " FROM " + table, connection))
            using (var reader = command.ExecuteReader())
                while (reader.Read()) actual.Add(reader.GetInt32(0));
            report.Rows.Add(new VerificationRow { Name = name, Expected = expected.Count, Actual = actual.Count,
                Missing = expected.Except(actual).Count(), Extra = actual.Except(expected).Count() });
        }
        private static void VerifyLinks(SqlConnection connection, MigrationVerification report, string name, string sql, Dictionary<int,int> expected)
        {
            var actual = new Dictionary<int,int>();
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
                while (reader.Read()) actual.Add(reader.GetInt32(0), reader.IsDBNull(1) ? 0 : reader.GetInt32(1));
            AddDetails(report, name, expected.ToDictionary(x => x.Key, x => x.Value.ToString(CultureInfo.InvariantCulture)),
                actual.ToDictionary(x => x.Key, x => x.Value.ToString(CultureInfo.InvariantCulture)));
            report.Rows.Add(new VerificationRow { Name = name, Expected = expected.Count, Actual = actual.Count,
                Missing = expected.Keys.Except(actual.Keys).Count(), Extra = actual.Keys.Except(expected.Keys).Count(),
                Mismatched = expected.Count(x => actual.ContainsKey(x.Key) && actual[x.Key] != x.Value) });
        }

        private static string[] Fields(params object[] values)
        {
            return values.Select(NormalizeField).ToArray();
        }
        private static string NormalizeField(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            if (value is DateTime) return ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);
            if (value is decimal) return ((decimal)value).ToString("G29", CultureInfo.InvariantCulture);
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }
        private static void VerifyValues(SqlConnection connection, MigrationVerification report, string name,
            string sql, Dictionary<int, string[]> expected)
        {
            var actual = new Dictionary<int, string[]>();
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                {
                    var fields = new string[reader.FieldCount - 1];
                    for (int i = 1; i < reader.FieldCount; i++)
                        fields[i - 1] = NormalizeField(reader.GetValue(i));
                    actual.Add(reader.GetInt32(0), fields);
                }
            AddDetails(report, name, expected.ToDictionary(x => x.Key, x => string.Join(" | ", x.Value)),
                actual.ToDictionary(x => x.Key, x => string.Join(" | ", x.Value)));
            report.Rows.Add(new VerificationRow {
                Name = name, Expected = expected.Count, Actual = actual.Count,
                Missing = expected.Keys.Except(actual.Keys).Count(),
                Extra = actual.Keys.Except(expected.Keys).Count(),
                Mismatched = expected.Count(pair => actual.ContainsKey(pair.Key) &&
                    !pair.Value.SequenceEqual(actual[pair.Key], StringComparer.Ordinal))
            });
        }

        private static void AddDetails(MigrationVerification report, string check,
            Dictionary<int, string> expected, Dictionary<int, string> actual)
        {
            foreach (var id in expected.Keys.Union(actual.Keys).OrderBy(x => x))
            {
                string left, right;
                bool hasLeft = expected.TryGetValue(id, out left);
                bool hasRight = actual.TryGetValue(id, out right);
                if (hasLeft && hasRight && string.Equals(left, right, StringComparison.Ordinal)) continue;
                report.Details.Add(new VerificationDetail {
                    Check = check, RecordId = id,
                    Issue = !hasLeft ? "Extra" : !hasRight ? "Missing" : "Mismatched",
                    ExpectedValue = hasLeft ? left : "", ActualValue = hasRight ? right : ""
                });
            }
        }

        internal sealed class MigrationPreview
        {
            internal int Students, Instructors, Courses, Enrollments, Attendance, Grades, Certificates;
            internal readonly Dictionary<int, int> AttendanceEnrollments = new Dictionary<int, int>();
            internal readonly Dictionary<int, int> GradeEnrollments = new Dictionary<int, int>();
            internal readonly Dictionary<int, int> CertificateEnrollments = new Dictionary<int, int>();
            internal readonly Dictionary<int, int> CourseInstructors = new Dictionary<int, int>();
            internal readonly Dictionary<int, int> EnrollmentStudents = new Dictionary<int, int>();
            internal readonly Dictionary<int, int> EnrollmentCourses = new Dictionary<int, int>();
        }

        private static void ResolveDependents<T>(IEnumerable<T> records, Func<T, int> idOf,
            Func<T, int> studentIdOf, Func<T, int> courseIdOf, Func<T, string> studentNameOf,
            Func<T, string> courseNameOf, Dictionary<string, List<Models.Student>> studentsByName,
            Dictionary<string, List<Models.Course>> coursesByTitle,
            Dictionary<int, Models.Student> studentsById, Dictionary<int, Models.Course> coursesById,
            Dictionary<string, int> enrollmentPairs, Dictionary<int, int> result, string kind)
        {
            if (records == null) throw new InvalidOperationException("Missing " + kind + " collection.");
            foreach (var record in records)
            {
                if (record == null || idOf(record) <= 0 || result.ContainsKey(idOf(record)))
                    throw new InvalidOperationException("Invalid or duplicate " + kind + " ID.");
                int studentId = studentIdOf(record), courseId = courseIdOf(record);
                if (studentId == 0)
                {
                    List<Models.Student> matches;
                    if (!studentsByName.TryGetValue(studentNameOf(record) ?? "", out matches) || matches.Count != 1)
                        throw new InvalidOperationException("Ambiguous " + kind + " student name.");
                    studentId = matches[0].Id;
                }
                if (courseId == 0)
                {
                    List<Models.Course> matches;
                    if (!coursesByTitle.TryGetValue(courseNameOf(record) ?? "", out matches) || matches.Count != 1)
                        throw new InvalidOperationException("Ambiguous " + kind + " course title.");
                    courseId = matches[0].Id;
                }
                if (!studentsById.ContainsKey(studentId) || !coursesById.ContainsKey(courseId))
                    throw new InvalidOperationException("Unknown " + kind + " student or course ID.");
                int enrollmentId;
                if (!enrollmentPairs.TryGetValue(studentId + ":" + courseId, out enrollmentId))
                    throw new InvalidOperationException("Missing enrollment for " + kind + " " + idOf(record));
                result.Add(idOf(record), enrollmentId);
            }
        }

        private static SqlParameter Param(string name, object value) { return new SqlParameter(name, value ?? DBNull.Value); }
        private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddRange(parameters);
                command.ExecuteNonQuery();
            }
        }
    }
}
