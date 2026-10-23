-- Run manually in a dedicated application database, not a system database.
-- Provision a dedicated SQL principal with INSERT and SELECT only for the application.
IF OBJECT_ID(N'dbo.LmsDeploymentAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LmsDeploymentAudit (
        AuditId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LmsDeploymentAudit PRIMARY KEY,
        OccurredUtc DATETIME2(3) NOT NULL,
        Administrator NVARCHAR(80) NOT NULL,
        ActionName NVARCHAR(64) NOT NULL,
        Outcome VARCHAR(7) NOT NULL CONSTRAINT CK_LmsDeploymentAudit_Outcome CHECK (Outcome IN ('SUCCESS','FAILED')),
        Summary NVARCHAR(180) NOT NULL
    );
    CREATE INDEX IX_LmsDeploymentAudit_OccurredUtc ON dbo.LmsDeploymentAudit(OccurredUtc DESC);
    CREATE INDEX IX_LmsDeploymentAudit_ActionOutcome ON dbo.LmsDeploymentAudit(ActionName, Outcome, AuditId DESC);
END;
-- No UPDATE/DELETE permissions should be granted to the application account.
