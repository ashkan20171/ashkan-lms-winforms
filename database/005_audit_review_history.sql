-- Stage 46: immutable history of audit-review decisions.
-- Run after database/004_audit_reviews.sql. Existing review rows are not backfilled.
IF OBJECT_ID(N'dbo.LmsAuditReviewHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LmsAuditReviewHistory
    (
        ReviewHistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LmsAuditReviewHistory PRIMARY KEY,
        AuditId BIGINT NOT NULL,
        ChangeType NVARCHAR(10) NOT NULL,
        PreviousStatus NVARCHAR(20) NULL,
        NewStatus NVARCHAR(20) NOT NULL,
        PreviousReviewer NVARCHAR(256) NULL,
        NewReviewer NVARCHAR(256) NOT NULL,
        PreviousNote NVARCHAR(2000) NULL,
        NewNote NVARCHAR(2000) NOT NULL,
        ChangedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_LmsAuditReviewHistory_Time DEFAULT SYSUTCDATETIME(),
        SqlLogin SYSNAME NOT NULL CONSTRAINT DF_LmsAuditReviewHistory_Login DEFAULT ORIGINAL_LOGIN(),
        CONSTRAINT CK_LmsAuditReviewHistory_Type CHECK (ChangeType IN (N'INSERT', N'UPDATE'))
    );
    CREATE INDEX IX_LmsAuditReviewHistory_AuditId_Time
        ON dbo.LmsAuditReviewHistory(AuditId, ChangedAtUtc DESC);
END;
GO
CREATE OR ALTER TRIGGER dbo.tr_LmsAuditReviews_History
ON dbo.LmsAuditReviews
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.LmsAuditReviewHistory
    (
        AuditId, ChangeType, PreviousStatus, NewStatus,
        PreviousReviewer, NewReviewer, PreviousNote, NewNote
    )
    SELECT
        i.AuditId,
        CASE WHEN d.AuditId IS NULL THEN N'INSERT' ELSE N'UPDATE' END,
        d.ReviewStatus, i.ReviewStatus,
        d.Reviewer, i.Reviewer,
        d.ReviewNote, i.ReviewNote
    FROM inserted AS i
    LEFT JOIN deleted AS d ON d.AuditId = i.AuditId;
END;
GO
-- Restrict SELECT to authorized administrators. Application users should not be granted
-- UPDATE/DELETE on this history table; the trigger is the only normal writer.
