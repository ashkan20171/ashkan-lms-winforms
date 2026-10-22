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
