/* Stage 109 — OPTIONAL constraint hardening for relational grade records.
   Requires review of 009_academic_identity_audit.sql first.
   No automatic deletions, remapping or snapshot conversion. Backup first.
   SQL Server: run only against the correct database. */
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.LmsGrades',N'U') IS NULL
    THROW 51091, 'dbo.LmsGrades missing; apply relational schema first.', 1;
IF EXISTS (SELECT 1 FROM dbo.LmsGrades GROUP BY EnrollmentId HAVING COUNT_BIG(*)>1)
    THROW 51092, 'Duplicate grade records exist; resolve manually before adding unique index.', 1;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LmsGrades') AND name=N'UX_LmsGrades_EnrollmentId')
    CREATE UNIQUE INDEX UX_LmsGrades_EnrollmentId ON dbo.LmsGrades(EnrollmentId);
COMMIT TRANSACTION;
