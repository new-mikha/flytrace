-- A template to restore LocalDB database in project from the SQL Server backup.
-- Change pathes & DB part names in 'MOVE' clauses accordingly.
-- Useful Git commands to apply after succesful the script applied successfully,
-- just to let Git ignore the changed DB file:
--      git update-index --assume-unchanged Tracker/App_Data/Flytrace.mdf		
--      git update-index --assume-unchanged Tracker/App_Data/Flytrace_log.ldf

RESTORE DATABASE [C:\Work\flytrace\Tracker\App_Data\Flytrace.mdf]
FROM DISK = 'C:\Temp\Flytrace.bak'
WITH REPLACE,
MOVE 'Tracker' TO 'C:\Work\flytrace\Tracker\App_Data\Flytrace.mdf',
MOVE 'Tracker_Log' TO 'C:\Work\flytrace\Tracker\App_Data\Flytrace_log.ldf'
