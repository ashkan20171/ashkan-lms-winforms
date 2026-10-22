-- Stage 45: Run once after 003_relational_audit.sql. No automatic deletion or alerts.
IF OBJECT_ID(N'dbo.LmsAuditReviews',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.LmsAuditReviews(
  AuditId BIGINT NOT NULL PRIMARY KEY,
  ReviewStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_LmsAuditReviews_Status DEFAULT N'Open',
  Reviewer NVARCHAR(256) NOT NULL,
  ReviewNote NVARCHAR(2000) NOT NULL,
  ReviewedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_LmsAuditReviews_Time DEFAULT SYSUTCDATETIME(),
  RowVersion ROWVERSION NOT NULL,
  CONSTRAINT FK_LmsAuditReviews_Audit FOREIGN KEY(AuditId) REFERENCES dbo.LmsRelationalAudit(AuditId),
  CONSTRAINT CK_LmsAuditReviews_Status CHECK(ReviewStatus IN (N'Open',N'Investigating',N'Resolved',N'FalsePositive'))
 );
 CREATE INDEX IX_LmsAuditReviews_Status ON dbo.LmsAuditReviews(ReviewStatus,ReviewedAtUtc DESC);
END;
GO
-- Least privilege: only the app's authorized administrative SQL role should receive
-- SELECT/INSERT/UPDATE on dbo.LmsAuditReviews. No DELETE grant is needed.
