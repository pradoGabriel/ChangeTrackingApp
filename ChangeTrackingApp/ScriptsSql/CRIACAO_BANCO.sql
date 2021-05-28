GO
CREATE DATABASE ChangeTrackingTest

GO
USE ChangeTrackingTest;  

GO 
CREATE TABLE [dbo].[ChangeTracking]
(
	Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	RandomString VARCHAR(MAX) NOT NULL
)

GO
CREATE TABLE [dbo].[ChangeTrackingVersion]
(
  [TableName] VARCHAR(100) NOT NULL PRIMARY KEY, 
  [ChangeTrackingVersion] [BIGINT] NULL
)

GO
ALTER DATABASE ChangeTrackingTest
SET CHANGE_TRACKING = ON
(CHANGE_RETENTION = 4 DAYS, AUTO_CLEANUP = ON)

GO
ALTER TABLE ChangeTracking ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON)

GO
MERGE dbo.ChangeTrackingVersion AS target
USING (SELECT 'ChangeTracking' [TableName]
, CHANGE_TRACKING_CURRENT_VERSION() [VersionID] 
) AS source
ON target.TableName = source.TableName

WHEN MATCHED 
THEN UPDATE
SET target.ChangeTrackingVersion = source.VersionID

WHEN NOT MATCHED
THEN INSERT (TableName, ChangeTrackingVersion)
VALUES (source.TableName, source.VersionID);