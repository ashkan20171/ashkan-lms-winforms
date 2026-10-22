-- Run AFTER 001_relational_schema.sql. SQL Server ROWVERSION changes on every update.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.LmsStudents', 'RowVersion') IS NULL
    ALTER TABLE dbo.LmsStudents ADD RowVersion rowversion NOT NULL;
IF COL_LENGTH('dbo.LmsCourses', 'RowVersion') IS NULL
    ALTER TABLE dbo.LmsCourses ADD RowVersion rowversion NOT NULL;
IF COL_LENGTH('dbo.LmsInstructors', 'RowVersion') IS NULL
    ALTER TABLE dbo.LmsInstructors ADD RowVersion rowversion NOT NULL;
IF COL_LENGTH('dbo.LmsEnrollments', 'RowVersion') IS NULL
    ALTER TABLE dbo.LmsEnrollments ADD RowVersion rowversion NOT NULL;
COMMIT TRANSACTION;
