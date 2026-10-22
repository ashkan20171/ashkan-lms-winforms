-- Stage 39: SQL Server 2016+ required (FOR JSON PATH).
-- Run after 001_relational_schema.sql and 002_rowversion_concurrency.sql.
-- Database triggers capture all SQL writes, including writes outside the application.
IF OBJECT_ID(N'dbo.LmsRelationalAudit', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.LmsRelationalAudit (
  AuditId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  EntityName NVARCHAR(64) NOT NULL,
  Operation NVARCHAR(10) NOT NULL,
  RecordId INT NOT NULL,
  OccurredAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_LmsAudit_Time DEFAULT SYSUTCDATETIME(),
  SqlLogin NVARCHAR(256) NOT NULL,
  Actor NVARCHAR(256) NULL,
  OldValues NVARCHAR(MAX) NULL,
  NewValues NVARCHAR(MAX) NULL
 );
 CREATE INDEX IX_LmsRelationalAudit_EntityRecord ON dbo.LmsRelationalAudit(EntityName,RecordId,AuditId DESC);
 CREATE INDEX IX_LmsRelationalAudit_Time ON dbo.LmsRelationalAudit(AuditId DESC);
END;
GO
CREATE OR ALTER TRIGGER dbo.trg_LmsStudents_Audit
ON dbo.LmsStudents AFTER INSERT, UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 -- FULL OUTER JOIN distinguishes INSERT, UPDATE, DELETE even for multi-row statements.
 INSERT INTO dbo.LmsRelationalAudit(EntityName,Operation,RecordId,SqlLogin,Actor,OldValues,NewValues)
 SELECT N'Student', CASE WHEN d.StudentId IS NULL THEN N'INSERT' WHEN i.StudentId IS NULL THEN N'DELETE' ELSE N'UPDATE' END,
        COALESCE(i.StudentId,d.StudentId), ORIGINAL_LOGIN(),
        CONVERT(NVARCHAR(256),SESSION_CONTEXT(N'ashkan_actor')),
        CASE WHEN d.StudentId IS NULL THEN NULL ELSE (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
        CASE WHEN i.StudentId IS NULL THEN NULL ELSE (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END
 FROM inserted AS i FULL OUTER JOIN deleted AS d ON i.StudentId=d.StudentId;
END;
GO
CREATE OR ALTER TRIGGER dbo.trg_LmsCourses_Audit
ON dbo.LmsCourses AFTER INSERT, UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 -- FULL OUTER JOIN distinguishes INSERT, UPDATE, DELETE even for multi-row statements.
 INSERT INTO dbo.LmsRelationalAudit(EntityName,Operation,RecordId,SqlLogin,Actor,OldValues,NewValues)
 SELECT N'Course', CASE WHEN d.CourseId IS NULL THEN N'INSERT' WHEN i.CourseId IS NULL THEN N'DELETE' ELSE N'UPDATE' END,
        COALESCE(i.CourseId,d.CourseId), ORIGINAL_LOGIN(),
        CONVERT(NVARCHAR(256),SESSION_CONTEXT(N'ashkan_actor')),
        CASE WHEN d.CourseId IS NULL THEN NULL ELSE (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
        CASE WHEN i.CourseId IS NULL THEN NULL ELSE (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END
 FROM inserted AS i FULL OUTER JOIN deleted AS d ON i.CourseId=d.CourseId;
END;
GO
CREATE OR ALTER TRIGGER dbo.trg_LmsInstructors_Audit
ON dbo.LmsInstructors AFTER INSERT, UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 -- FULL OUTER JOIN distinguishes INSERT, UPDATE, DELETE even for multi-row statements.
 INSERT INTO dbo.LmsRelationalAudit(EntityName,Operation,RecordId,SqlLogin,Actor,OldValues,NewValues)
 SELECT N'Instructor', CASE WHEN d.InstructorId IS NULL THEN N'INSERT' WHEN i.InstructorId IS NULL THEN N'DELETE' ELSE N'UPDATE' END,
        COALESCE(i.InstructorId,d.InstructorId), ORIGINAL_LOGIN(),
        CONVERT(NVARCHAR(256),SESSION_CONTEXT(N'ashkan_actor')),
        CASE WHEN d.InstructorId IS NULL THEN NULL ELSE (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
        CASE WHEN i.InstructorId IS NULL THEN NULL ELSE (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END
 FROM inserted AS i FULL OUTER JOIN deleted AS d ON i.InstructorId=d.InstructorId;
END;
GO
CREATE OR ALTER TRIGGER dbo.trg_LmsEnrollments_Audit
ON dbo.LmsEnrollments AFTER INSERT, UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 -- FULL OUTER JOIN distinguishes INSERT, UPDATE, DELETE even for multi-row statements.
 INSERT INTO dbo.LmsRelationalAudit(EntityName,Operation,RecordId,SqlLogin,Actor,OldValues,NewValues)
 SELECT N'Enrollment', CASE WHEN d.EnrollmentId IS NULL THEN N'INSERT' WHEN i.EnrollmentId IS NULL THEN N'DELETE' ELSE N'UPDATE' END,
        COALESCE(i.EnrollmentId,d.EnrollmentId), ORIGINAL_LOGIN(),
        CONVERT(NVARCHAR(256),SESSION_CONTEXT(N'ashkan_actor')),
        CASE WHEN d.EnrollmentId IS NULL THEN NULL ELSE (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
        CASE WHEN i.EnrollmentId IS NULL THEN NULL ELSE (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END
 FROM inserted AS i FULL OUTER JOIN deleted AS d ON i.EnrollmentId=d.EnrollmentId;
END;
GO
