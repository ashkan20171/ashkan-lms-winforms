-- Stage 50: optional persistent daily deletion threshold configuration.
-- Run after 003_relational_audit.sql. Requires SQL Server.
IF OBJECT_ID(N'dbo.LmsDeletionThresholdRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LmsDeletionThresholdRules
    (
        RuleKey NVARCHAR(64) NOT NULL CONSTRAINT PK_LmsDeletionThresholdRules PRIMARY KEY,
        MinimumDailyDeletes INT NOT NULL CONSTRAINT CK_LmsDeletionThresholdRules_Min CHECK (MinimumDailyDeletes BETWEEN 1 AND 10000),
        UpdatedBy NVARCHAR(256) NOT NULL,
        UpdatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_LmsDeletionThresholdRules_Utc DEFAULT SYSUTCDATETIME(),
        RowVersion ROWVERSION NOT NULL
    );
END;
GO
-- This table stores configuration only; no background jobs or notifications are created.
