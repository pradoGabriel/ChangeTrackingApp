USE ChangeTrackingTest;  
GO  
CREATE PROCEDURE USP_RETORNA_ALTERACOES
AS   
BEGIN
DECLARE @StartVersionID BIGINT
, @EndVersionID BIGINT

-- Define versão inicial
SET @StartVersionID = (SELECT ChangeTrackingVersion
FROM dbo.ChangeTrackingVersion
WHERE TableName = 'ChangeTracking')
-- Define versão final
SET @EndVersionID = CHANGE_TRACKING_CURRENT_VERSION()

SELECT 
CT.*
FROM CHANGETABLE(CHANGES dbo.ChangeTracking, @StartVersionID) C
LEFT JOIN dbo.ChangeTracking CT
ON C.ID = CT.ID
WHERE (SELECT MAX(v)
FROM (VALUES(C.SYS_CHANGE_VERSION), (C.SYS_CHANGE_CREATION_VERSION)) AS VALUE(v)) <= @EndVersionID

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
END
