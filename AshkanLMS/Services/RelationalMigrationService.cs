using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace AshkanLMS.Services
{
    /// <summary>Explicit, one-time migration of the core snapshot entities into empty relational tables.
    /// Never runs during application startup and never deletes existing SQL records.</summary>
    internal sealed class RelationalMigrationService
    {
        private readonly string connectionString;
        internal RelationalMigrationService(string connectionString) { this.connectionString = connectionString ?? throw new ArgumentNullException("connectionString"); }

        internal void ImportCore(LmsState state)
        {
            LmsDataIntegrity.Validate(state);
            if (state.Instructors == null || state.Enrollments == null)
                throw new InvalidOperationException("Instructor and enrollment collections are required.");
            var instructors = state.Instructors.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            var students = state.Students.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            var courses = state.Courses.ToDictionary(x => x.Title, StringComparer.OrdinalIgnoreCase);
            if (state.Courses.Any(c => !instructors.ContainsKey(c.Instructor ?? "")))
                throw new InvalidOperationException("Course instructor name does not resolve uniquely. Correct source data before migration.");
            if (state.Enrollments.Any(e => !students.ContainsKey(e.Student ?? "") || !courses.ContainsKey(e.Course ?? "")))
                throw new InvalidOperationException("Enrollment name does not resolve to an entity.");
            if (state.Enrollments.Select(e => students[e.Student].Id.ToString() + ":" + courses[e.Course].Id).Distinct().Count() != state.Enrollments.Count)
                throw new InvalidOperationException("Duplicate student/course enrollment pairs.");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        using (var guard = new SqlCommand(@"IF EXISTS(SELECT 1 FROM dbo.LmsStudents) OR EXISTS(SELECT 1 FROM dbo.LmsCourses) OR EXISTS(SELECT 1 FROM dbo.LmsInstructors) OR EXISTS(SELECT 1 FROM dbo.LmsEnrollments) THROW 51001, 'Relational tables must be empty for first import.', 1;", connection, transaction))
                            guard.ExecuteNonQuery();
                        foreach (var i in state.Instructors)
                            Execute(connection, transaction, "INSERT dbo.LmsInstructors(InstructorId,FullName,Email,Expertise) VALUES(@id,@name,@email,@expertise)",
                                Param("@id", i.Id), Param("@name", i.Name), Param("@email", i.Email), Param("@expertise", i.Expertise));
                        foreach (var s in state.Students)
                            Execute(connection, transaction, "INSERT dbo.LmsStudents(StudentId,FullName,Email,StatusKey,Progress) VALUES(@id,@name,@email,@status,@progress)",
                                Param("@id", s.Id), Param("@name", s.Name), Param("@email", s.Email), Param("@status", s.StatusKey), Param("@progress", s.Progress));
                        foreach (var c in state.Courses)
                            Execute(connection, transaction, "INSERT dbo.LmsCourses(CourseId,Title,InstructorId,StatusKey,Progress) VALUES(@id,@title,@instructor,@status,@progress)",
                                Param("@id", c.Id), Param("@title", c.Title), Param("@instructor", instructors[c.Instructor].Id), Param("@status", c.StatusKey), Param("@progress", c.Progress));
                        foreach (var e in state.Enrollments)
                            Execute(connection, transaction, "INSERT dbo.LmsEnrollments(EnrollmentId,StudentId,CourseId,EnrolledAt,StatusKey) VALUES(@id,@student,@course,@date,@status)",
                                Param("@id", e.Id), Param("@student", students[e.Student].Id), Param("@course", courses[e.Course].Id), Param("@date", e.Date), Param("@status", e.StatusKey));
                        transaction.Commit();
                    }
                    catch { transaction.Rollback(); throw; }
                }
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
