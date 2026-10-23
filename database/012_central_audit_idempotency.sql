-- Apply to the dedicated application database BEFORE enabling Stage 120 syncing.
-- Existing legacy events retain NULL EventId; new queued events use unique GUIDs.
IF COL_LENGTH('dbo.LmsDeploymentAudit', 'EventId') IS NULL
    ALTER TABLE dbo.LmsDeploymentAudit ADD EventId UNIQUEIDENTIFIER NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.LmsDeploymentAudit') AND name = 'UX_LmsDeploymentAudit_EventId')
    CREATE UNIQUE INDEX UX_LmsDeploymentAudit_EventId ON dbo.LmsDeploymentAudit(EventId) WHERE EventId IS NOT NULL;
GO
