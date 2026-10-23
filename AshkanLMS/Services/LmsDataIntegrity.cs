using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AshkanLMS.Services
{
    /// <summary>Checks core identity and reference invariants before persisting a snapshot.</summary>
    internal static class LmsDataIntegrity
    {
        internal static void Validate(LmsState state)
        {
            if (state == null || state.Students == null || state.Courses == null)
                throw new InvalidDataException("LMS state is incomplete.");
            if (state.Students.Any(x => x == null || x.Id <= 0 || string.IsNullOrWhiteSpace(x.Name) ||
                                        string.IsNullOrWhiteSpace(x.Email) || x.Progress < 0 || x.Progress > 100))
                throw new InvalidDataException("Student data contains invalid records.");
            if (state.Courses.Any(x => x == null || x.Id <= 0 || string.IsNullOrWhiteSpace(x.Title) ||
                                       x.Progress < 0 || x.Progress > 100))
                throw new InvalidDataException("Course data contains invalid records.");
            if (state.Students.Select(x => x.Id).Distinct().Count() != state.Students.Count ||
                state.Courses.Select(x => x.Id).Distinct().Count() != state.Courses.Count)
                throw new InvalidDataException("Duplicate student or course IDs.");
            if (state.Students.Select(x => x.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count() != state.Students.Count ||
                state.Courses.Select(x => x.Title).Distinct(StringComparer.OrdinalIgnoreCase).Count() != state.Courses.Count)
                throw new InvalidDataException("Duplicate student email or course title.");
            // Explicit identifiers, when present, must refer to existing entities.
            var studentIds = new HashSet<int>(state.Students.Select(x => x.Id));
            var courseIds = new HashSet<int>(state.Courses.Select(x => x.Id));
            if (state.Enrollments != null && state.Enrollments.Any(x => x != null &&
                ((x.StudentId != 0 && !studentIds.Contains(x.StudentId)) ||
                 (x.CourseId != 0 && !courseIds.Contains(x.CourseId)))))
                throw new InvalidDataException("Enrollment has an invalid identity reference.");
            if (state.Attendance != null && state.Attendance.Any(x => x == null ||
                (x.StudentId != 0 && !studentIds.Contains(x.StudentId)) ||
                (x.CourseId != 0 && !courseIds.Contains(x.CourseId))))
                throw new InvalidDataException("Attendance has an invalid identity reference.");
            if (state.Grades != null && state.Grades.Any(x => x == null ||
                (x.StudentId != 0 && !studentIds.Contains(x.StudentId)) ||
                (x.CourseId != 0 && !courseIds.Contains(x.CourseId))))
                throw new InvalidDataException("Grade has an invalid identity reference.");
            if (state.Certificates != null && state.Certificates.Any(x => x == null ||
                (x.StudentId != 0 && !studentIds.Contains(x.StudentId)) ||
                (x.CourseId != 0 && !courseIds.Contains(x.CourseId))))
                throw new InvalidDataException("Certificate has an invalid identity reference.");
            if (state.Enrollments != null)
            {
                var studentNames = new HashSet<string>(state.Students.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
                var courseNames = new HashSet<string>(state.Courses.Select(x => x.Title), StringComparer.OrdinalIgnoreCase);
                if (state.Enrollments.Any(x => x == null || !studentNames.Contains(x.Student ?? "") ||
                                              !courseNames.Contains(x.Course ?? "")))
                    throw new InvalidDataException("An enrollment references an unknown student or course.");
            }
        }
    }
}
