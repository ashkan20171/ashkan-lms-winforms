/* Stage 109 — READ-ONLY SQL Server academic identity audit.
   Execute in the intended AshkanLMS database after backup. No data changes.
   LmsState JSON snapshots are NOT automatically migrated into relational tables. */
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.LmsStudents',N'U') IS NULL OR
   OBJECT_ID(N'dbo.LmsCourses',N'U') IS NULL OR
   OBJECT_ID(N'dbo.LmsEnrollments',N'U') IS NULL OR
   OBJECT_ID(N'dbo.LmsAttendance',N'U') IS NULL OR
   OBJECT_ID(N'dbo.LmsGrades',N'U') IS NULL OR
   OBJECT_ID(N'dbo.LmsCertificates',N'U') IS NULL
BEGIN
    THROW 51090, 'Required Lms relational tables are missing. Apply 001_relational_schema.sql first.', 1;
END;

/* Orphans, including records produced with constraints disabled. */
SELECT N'Enrollment missing student' AS Finding, e.EnrollmentId AS RecordId
FROM dbo.LmsEnrollments e LEFT JOIN dbo.LmsStudents s ON s.StudentId=e.StudentId
WHERE s.StudentId IS NULL
UNION ALL
SELECT N'Enrollment missing course',e.EnrollmentId
FROM dbo.LmsEnrollments e LEFT JOIN dbo.LmsCourses c ON c.CourseId=e.CourseId
WHERE c.CourseId IS NULL
UNION ALL
SELECT N'Attendance missing enrollment',a.AttendanceId
FROM dbo.LmsAttendance a LEFT JOIN dbo.LmsEnrollments e ON e.EnrollmentId=a.EnrollmentId
WHERE e.EnrollmentId IS NULL
UNION ALL
SELECT N'Grade missing enrollment',g.GradeId
FROM dbo.LmsGrades g LEFT JOIN dbo.LmsEnrollments e ON e.EnrollmentId=g.EnrollmentId
WHERE e.EnrollmentId IS NULL
UNION ALL
SELECT N'Certificate missing enrollment',c.CertificateId
FROM dbo.LmsCertificates c LEFT JOIN dbo.LmsEnrollments e ON e.EnrollmentId=c.EnrollmentId
WHERE e.EnrollmentId IS NULL;

/* Duplicate business keys: must return zero rows. */
SELECT StudentId,CourseId,COUNT_BIG(*) AS DuplicateCount
FROM dbo.LmsEnrollments GROUP BY StudentId,CourseId HAVING COUNT_BIG(*)>1;
SELECT EnrollmentId,SessionDate,COUNT_BIG(*) AS DuplicateCount
FROM dbo.LmsAttendance GROUP BY EnrollmentId,SessionDate HAVING COUNT_BIG(*)>1;
SELECT CertificateCode,COUNT_BIG(*) AS DuplicateCount
FROM dbo.LmsCertificates GROUP BY CertificateCode HAVING COUNT_BIG(*)>1;
/* Unlike attendance, current schema allows multiple grade records per enrollment.
   Report them for human review; do NOT delete or merge automatically. */
SELECT EnrollmentId,COUNT_BIG(*) AS GradeCount
FROM dbo.LmsGrades GROUP BY EnrollmentId HAVING COUNT_BIG(*)>1;

/* Name ambiguity is informational only; names must not be used as foreign keys. */
SELECT FullName,COUNT_BIG(*) AS StudentCount
FROM dbo.LmsStudents GROUP BY FullName HAVING COUNT_BIG(*)>1;
SELECT FullName,COUNT_BIG(*) AS InstructorCount
FROM dbo.LmsInstructors GROUP BY FullName HAVING COUNT_BIG(*)>1;

/* Validate foreign keys are trusted/enabled; do not silently re-enable them. */
SELECT fk.name AS ForeignKeyName,OBJECT_NAME(fk.parent_object_id) AS TableName,
       fk.is_disabled,fk.is_not_trusted
FROM sys.foreign_keys fk
WHERE fk.parent_object_id IN (OBJECT_ID(N'dbo.LmsCourses'),OBJECT_ID(N'dbo.LmsEnrollments'),
 OBJECT_ID(N'dbo.LmsAttendance'),OBJECT_ID(N'dbo.LmsGrades'),OBJECT_ID(N'dbo.LmsCertificates'));
