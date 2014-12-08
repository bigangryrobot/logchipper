USE [iis_logs]
GO
SELECT COUNT(*) FROM [dbo].[events_clark];
SELECT COUNT(*) FROM [dbo].[events_clark_data];
SELECT DATEPART(second, max(insert_date_time) - min(insert_date_time))
FROM [dbo].[events_clark_data];
SELECT * FROM [dbo].[events_clark_data];

SELECT DATEPART(n, time_stamp) AS minute, COUNT(*) as results
FROM table_name 
WHERE time_stamp > DATEADD(hh, -1, GETDATE())

SELECT COUNT(*) FROM [dbo].[events_clark]
GROUP BY DATEPART(n, insert_date)


DELETE FROM [dbo].[events_clark];
GO
DELETE FROM [dbo].[events_clark_data];
GO

