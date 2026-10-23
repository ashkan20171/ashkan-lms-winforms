using System;
using System.Linq;
using AshkanLMS.Models;

namespace AshkanLMS.Services
{
    /// <summary>Resolves legacy display names only when exactly one identity matches.</summary>
    public static class AcademicIdentityService
    {
        public static int UniqueStudentId(DataService data, string name)
        {
            var matches = data.Students.Where(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            return matches.Count == 1 ? matches[0].Id : 0;
        }
        public static int UniqueCourseId(DataService data, string title)
        {
            var matches = data.Courses.Where(x => string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            return matches.Count == 1 ? matches[0].Id : 0;
        }
        public static int UniqueInstructorId(DataService data, string name)
        {
            var matches = data.Instructors.Where(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            return matches.Count == 1 ? matches[0].Id : 0;
        }
        // This is intentionally explicit; never silently rewrite ambiguous legacy references.
        public static int LinkUnambiguousLegacyRecords(DataService data)
        {
            int changed = 0;
            foreach (var enrollment in data.Enrollments)
            {
                if (enrollment.StudentId == 0) { int id = UniqueStudentId(data, enrollment.Student); if (id != 0) { enrollment.StudentId = id; changed++; } }
                if (enrollment.CourseId == 0) { int id = UniqueCourseId(data, enrollment.Course); if (id != 0) { enrollment.CourseId = id; changed++; } }
            }
            foreach (var course in data.Courses)
                if (course.InstructorId == 0) { int id = UniqueInstructorId(data, course.Instructor); if (id != 0) { course.InstructorId = id; changed++; } }
            foreach (var record in data.Attendance)
            {
                if (record.StudentId == 0) { var id = UniqueStudentId(data, record.Student); if (id > 0) { record.StudentId = id; changed++; } }
                if (record.CourseId == 0) { var id = UniqueCourseId(data, record.Course); if (id > 0) { record.CourseId = id; changed++; } }
            }
            foreach (var record in data.Grades)
            {
                if (record.StudentId == 0) { var id = UniqueStudentId(data, record.Student); if (id > 0) { record.StudentId = id; changed++; } }
                if (record.CourseId == 0) { var id = UniqueCourseId(data, record.Course); if (id > 0) { record.CourseId = id; changed++; } }
            }
            foreach (var record in data.Certificates)
            {
                if (record.StudentId == 0) { var id = UniqueStudentId(data, record.Student); if (id > 0) { record.StudentId = id; changed++; } }
                if (record.CourseId == 0) { var id = UniqueCourseId(data, record.Course); if (id > 0) { record.CourseId = id; changed++; } }
            }
            return changed;
        }
    }
}
