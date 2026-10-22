-- Run after database/001_initial_schema.sql in SQL Server Management Studio.
USE AshkanLMS;
GO
IF OBJECT_ID(N'dbo.LmsState', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.LmsState (
  Id INT NOT NULL CONSTRAINT PK_LmsState PRIMARY KEY,
  StateJson NVARCHAR(MAX) NOT NULL,
  UpdatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_LmsState_UpdatedUtc DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_LmsState_Json CHECK (ISJSON(StateJson)=1)
 );
END;
GO
