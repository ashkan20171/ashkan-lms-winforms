/* Stage 75: read-only relational schema integration checks.
   Run only after 001_relational_schema.sql in a disposable test database.
   No inserts, updates, deletes, or schema changes. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() IN (N'master',N'model',N'msdb',N'tempdb')
    THROW 51000, 'Refusing to test a system database.', 1;
DECLARE @required TABLE (TableName sysname NOT NULL);
INSERT INTO @required(TableName) VALUES
(N'LmsStudents'),(N'LmsInstructors'),(N'LmsCourses'),(N'LmsEnrollments'),
(N'LmsAttendance'),(N'LmsGrades'),(N'LmsCertificates');
IF EXISTS (SELECT 1 FROM @required WHERE OBJECT_ID(N'dbo.' + TableName, N'U') IS NULL)
    THROW 51001, 'One or more LMS relational tables are missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LmsEnrollments_Students' AND parent_object_id = OBJECT_ID(N'dbo.LmsEnrollments'))
    THROW 51002, 'Enrollment student foreign key is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LmsEnrollments_Courses' AND parent_object_id = OBJECT_ID(N'dbo.LmsEnrollments'))
    THROW 51003, 'Enrollment course foreign key is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_LmsStudents_Progress')
    THROW 51004, 'Student progress constraint is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_LmsGrades_Score')
    THROW 51005, 'Grade score constraint is missing.', 1;
SELECT N'PASS: relational schema smoke checks' AS Result, DB_NAME() AS DatabaseName;
