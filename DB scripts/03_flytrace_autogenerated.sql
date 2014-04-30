-------------------------------------------------------------------------------
-- Flytrace, online viewer for GPS trackers.
-- Copyright (C) 2011-2014 Mikhail Karmazin
-- 
-- This file is part of Flytrace.
-- 
-- Flytrace is free software: you can redistribute it and/or modify
-- it under the terms of the GNU Affero General Public License as
-- published by the Free Software Foundation, either version 3 of the
-- License, or (at your option) any later version.
-- 
-- Flytrace is distributed in the hope that it will be useful,
-- but WITHOUT ANY WARRANTY; without even the implied warranty of
-- MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
-- GNU Affero General Public License for more details.
-- 
-- You should have received a copy of the GNU Affero General Public License
-- along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
-------------------------------------------------------------------------------


CREATE ROLE [flytrace_sp_Execute] AUTHORIZATION [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION [dbo].[ZeroPaddedString]
(
	@val INT,
	@min_length INT
)
RETURNS VARCHAR(50)
AS
BEGIN
	DECLARE @result VARCHAR(50)
	
	SET @result = CAST(@val AS VARCHAR(50))
	
	IF( LEN(@result) < @min_length )
		SET @result = REPLACE(STR(@val , @min_length, 0), ' ', '0')
		
	RETURN @result
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Event](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](256) NOT NULL,
	[StartTs] [datetime] NULL,
 CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Event_UserId_Name] ON [dbo].[Event] 
(
	[UserId] ASC,
	[Name] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'"Event" is a short name for "Waypoint Set"' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Event'
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Group](
	[Id] [int] IDENTITY(33,1) NOT NULL,
	[Name] [nvarchar](256) NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[EventId] [int] NULL,
	[NewestCoordTs] [datetime] NULL,
	[NewestLat] [float] NULL,
	[NewestLon] [float] NULL,
	[ViewsNum] [int] NOT NULL,
	[PageUpdatesNum] [bigint] NOT NULL,
	[IsPublic] [bit] NOT NULL,
	[Version] [int] NOT NULL,
	[DisplayUserMessages] [bit] NOT NULL,
 CONSTRAINT [PK_Group] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Group_UserId_Name] ON [dbo].[Group] 
(
	[UserId] ASC,
	[Name] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Group] ADD  CONSTRAINT [DF__Group__ViewsNum__160F4887]  DEFAULT ((0)) FOR [ViewsNum]
GO
ALTER TABLE [dbo].[Group] ADD  CONSTRAINT [DF__Group__PageUpdat__17036CC0]  DEFAULT ((0)) FOR [PageUpdatesNum]
GO
ALTER TABLE [dbo].[Group] ADD  CONSTRAINT [Group_IsPublic]  DEFAULT ((1)) FOR [IsPublic]
GO
ALTER TABLE [dbo].[Group] ADD  DEFAULT ((0)) FOR [DisplayUserMessages]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GroupTracker](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GroupId] [int] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[TrackerForeignId] [nvarchar](256) NOT NULL,
 CONSTRAINT [PK_GroupTracker] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_GroupTracker_GroupId_ForeignId] ON [dbo].[GroupTracker] 
(
	[GroupId] ASC,
	[TrackerForeignId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_GroupTracker_GroupId_Name] ON [dbo].[GroupTracker] 
(
	[GroupId] ASC,
	[Name] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Task](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[WaypointId] [int] NOT NULL,
	[Radius] [int] NOT NULL,
	[WptOrder] [int] NOT NULL,
 CONSTRAINT [PK_Task] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE NONCLUSTERED INDEX [IX_Task] ON [dbo].[Task] 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserProfile](
	[UserId] [uniqueidentifier] NOT NULL,
	[DefaultEventId] [int] NULL,
	[IsSimpleEventsModel] [bit] NOT NULL,
	[CoordFormat] [nvarchar](50) NOT NULL,
	[DefHemisphereNS] [char](1) NOT NULL,
	[DefHemisphereEW] [char](1) NOT NULL,
	[ShowUserMessagesByDefault] [bit] NOT NULL,
	[UserMessagesSettingIsNew] [bit] NOT NULL
) ON [PRIMARY]

GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Default event IDs kept here. UserId is unique field. As a result, only zero or one default event is possible for every user.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'UserProfile', @level2type=N'COLUMN',@level2name=N'DefaultEventId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'UserProfile'
GO
ALTER TABLE [dbo].[UserProfile]  WITH CHECK ADD  CONSTRAINT [CK_UserProfile_CoordFormat] CHECK  (([CoordFormat]='DegMinSec' OR [CoordFormat]='DegMin' OR [CoordFormat]='Deg'))
GO
ALTER TABLE [dbo].[UserProfile] CHECK CONSTRAINT [CK_UserProfile_CoordFormat]
GO
ALTER TABLE [dbo].[UserProfile]  WITH CHECK ADD  CONSTRAINT [CK_UserProfile_DefHemispheres] CHECK  ((([DefHemisphereNS]='S' OR [DefHemisphereNS]='N') AND ([DefHemisphereEW]='W' OR [DefHemisphereEW]='E')))
GO
ALTER TABLE [dbo].[UserProfile] CHECK CONSTRAINT [CK_UserProfile_DefHemispheres]
GO
ALTER TABLE [dbo].[UserProfile] ADD  CONSTRAINT [DF_UserProfile_IsSimpleEventsModel]  DEFAULT ((1)) FOR [IsSimpleEventsModel]
GO
ALTER TABLE [dbo].[UserProfile] ADD  CONSTRAINT [DF_UserProfile_CoordFormat]  DEFAULT (N'Deg') FOR [CoordFormat]
GO
ALTER TABLE [dbo].[UserProfile] ADD  CONSTRAINT [DF_UserProfile_DefHemisphereNS]  DEFAULT ('N') FOR [DefHemisphereNS]
GO
ALTER TABLE [dbo].[UserProfile] ADD  CONSTRAINT [DF_UserProfile_DefHemisphereEW]  DEFAULT ('E') FOR [DefHemisphereEW]
GO
ALTER TABLE [dbo].[UserProfile] ADD  DEFAULT ((0)) FOR [ShowUserMessagesByDefault]
GO
ALTER TABLE [dbo].[UserProfile] ADD  DEFAULT ((0)) FOR [UserMessagesSettingIsNew]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Waypoint](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EventId] [int] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Lat] [float] NOT NULL,
	[Lon] [float] NOT NULL,
	[Alt] [float] NOT NULL,
	[Description] [nvarchar](500) NULL,
 CONSTRAINT [PK_Waypoint] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Waypoint] ON [dbo].[Waypoint] 
(
	[EventId] ASC,
	[Name] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Event]  WITH CHECK ADD  CONSTRAINT [FK_Event_aspnet_Users] FOREIGN KEY([UserId])
REFERENCES [aspnet_Users] ([UserId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Event] CHECK CONSTRAINT [FK_Event_aspnet_Users]
GO
ALTER TABLE [dbo].[Group]  WITH CHECK ADD  CONSTRAINT [FK_Group_aspnet_Users] FOREIGN KEY([UserId])
REFERENCES [aspnet_Users] ([UserId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Group] CHECK CONSTRAINT [FK_Group_aspnet_Users]
GO
ALTER TABLE [dbo].[Group]  WITH CHECK ADD  CONSTRAINT [FK_Group_Event] FOREIGN KEY([EventId])
REFERENCES [Event] ([Id])
GO
ALTER TABLE [dbo].[Group] CHECK CONSTRAINT [FK_Group_Event]
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Group might use an event made by the same user, or another user, or might have no assigned event at all.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Group', @level2type=N'CONSTRAINT',@level2name=N'FK_Group_Event'
GO
ALTER TABLE [dbo].[GroupTracker]  WITH CHECK ADD  CONSTRAINT [FK_GroupTracker_Group] FOREIGN KEY([GroupId])
REFERENCES [Group] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[GroupTracker] CHECK CONSTRAINT [FK_GroupTracker_Group]
GO
ALTER TABLE [dbo].[Task]  WITH CHECK ADD  CONSTRAINT [FK_Task_Waypoint] FOREIGN KEY([WaypointId])
REFERENCES [Waypoint] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Task] CHECK CONSTRAINT [FK_Task_Waypoint]
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'At the moment, just one (current) task per event' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Task', @level2type=N'CONSTRAINT',@level2name=N'FK_Task_Waypoint'
GO
ALTER TABLE [dbo].[UserProfile]  WITH CHECK ADD  CONSTRAINT [FK_UserProfile_aspnet_Users] FOREIGN KEY([UserId])
REFERENCES [aspnet_Users] ([UserId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserProfile] CHECK CONSTRAINT [FK_UserProfile_aspnet_Users]
GO
ALTER TABLE [dbo].[UserProfile]  WITH CHECK ADD  CONSTRAINT [FK_UserProfile_Event] FOREIGN KEY([DefaultEventId])
REFERENCES [Event] ([Id])
GO
ALTER TABLE [dbo].[UserProfile] CHECK CONSTRAINT [FK_UserProfile_Event]
GO
ALTER TABLE [dbo].[Waypoint]  WITH CHECK ADD  CONSTRAINT [FK_Waypoint_Event] FOREIGN KEY([EventId])
REFERENCES [Event] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Waypoint] CHECK CONSTRAINT [FK_Waypoint_Event]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [dbo].[EventView]
AS
SELECT    
	E.Id, 
	E.UserId, 
	E.Name, 
	E.StartTs,
	CAST(CASE WHEN (P.DefaultEventId IS NULL) THEN 0 ELSE 1 END AS BIT) AS IsDefault
FROM  dbo.[Event] AS E LEFT OUTER JOIN
	dbo.UserProfile AS P ON E.Id = P.DefaultEventId AND E.UserId = P.UserId

GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "E"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 110
               Right = 198
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "P"
            Begin Extent = 
               Top = 44
               Left = 345
               Bottom = 133
               Right = 507
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'EventView'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'EventView'
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE AddTrackerToGroup
	@GroupId INT,
	@Name NVARCHAR(256),
	@TrackerForeignId NVARCHAR(256)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- See [GetGroupTrackerIds] proc for explaining the transaction. Note that read committed level is fine here,
	-- it should be REPEATABLE READ only when reading.
	-- Setting isolation level to make sure a caller didn't override it to make it lower:
	SET TRANSACTION ISOLATION LEVEL READ COMMITTED

	BEGIN TRANSACTION
	
	-- updating version should be the first in transaction, see above
	UPDATE [Group] SET [Version] = [Version] + 1 WHERE Id = @GroupId

	INSERT INTO GroupTracker(GroupId, Name, TrackerForeignId) VALUES (@GroupId, @Name, @TrackerForeignId)
	
	COMMIT TRANSACTION
END

GO
GRANT EXECUTE ON [dbo].[AddTrackerToGroup] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[AssignEventToGroup]
	-- Add the parameters for the stored procedure here
	@EventId INT, 
	@GroupId INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @GroupUserId UNIQUEIDENTIFIER
	SELECT @GroupUserId = [UserId] FROM [Group] WHERE [Id] = @GroupId
	IF( @GroupUserId IS NULL ) RETURN

	DECLARE @ErrorVar INT
	SET @ErrorVar = 0
	
	IF( @EventId IS NULL )
	BEGIN
		UPDATE [Group] SET [EventId] = NULL WHERE [Id] = @GroupId
		SET @ErrorVar = @@ERROR
	END ELSE
	BEGIN
		DECLARE @EventUserId UNIQUEIDENTIFIER
		SELECT @EventUserId = [UserId] FROM [Event] WHERE [Id] = @EventId

		IF( @EventUserId IS NOT NULL AND
			@EventUserId = @GroupUserId )
		BEGIN
			UPDATE [Group] SET [EventId] = @EventId WHERE [Id] = @GroupId
			SET @ErrorVar = @@ERROR
			
			EXEC @ErrorVar = SetEventAsDefault @EventId
		END
	END
	
	RETURN @ErrorVar
END

GO
GRANT EXECUTE ON [dbo].[AssignEventToGroup] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CreateEvent]
	-- Add the parameters for the stored procedure here
	@UserId UNIQUEIDENTIFIER,
	@NewEventId INT OUTPUT
AS
BEGIN
	SET NOCOUNT ON;
	
	-- Race condition are considered as highly unlikely. At worst, without
	-- transaction there could be an error and it still will be atomic.
	
	-- Find free automatic group name for the user first:
	DECLARE @eventName NVARCHAR(256)
	DECLARE @eventNum INT
	SET @eventNum = 1
	WHILE(0=0)
	BEGIN
		SET @eventName = 'Event ' + dbo.ZeroPaddedString(@eventNum, 2)
		IF( NOT EXISTS( SELECT * FROM [Event] WHERE UserId=@UserId AND [Name]=@eventName)) BREAK
		
		SET @eventNum = @eventNum + 1
	END

	DECLARE @errorVar INT
	SET @errorVar = 0
	
	INSERT INTO [Event] (Name, UserId) VALUES (@eventName, @UserId)
	SET @errorVar = @@ERROR
	
	IF( @errorVar = 0 )
		SET @NewEventId = SCOPE_IDENTITY();

	RETURN @errorVar
END

GO
GRANT EXECUTE ON [dbo].[CreateEvent] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[CreateTrackersGroup]
	-- Add the parameters for the stored procedure here
	@UserId UNIQUEIDENTIFIER,
	@NewGroupId INT OUTPUT
AS
BEGIN
	SET NOCOUNT ON
	
	-- race condition are considered as highly unlikely, and even if it happens the worst thing to happen 
	-- will be just an error msg displayed to the user. Preventing it with transactions looks like 
	-- an overkill since there are no ways to lock a new record apart from TABLOCKX or sp_getapplock
	-- and both seems like too much (Update/Insert procs will be needed too etc)
	
	-- Find default event, and create one if necessary
	DECLARE @DefaultEventId INT
	DECLARE @IsSimpleEventsModel BIT
	DECLARE @CoordFormat NVARCHAR(50)
	DECLARE @DefHemisphereNS CHAR(1)
	DECLARE @DefHemisphereEW CHAR(1)
	DECLARE @ShowUserMessagesByDefault BIT

	DECLARE @errorVar INT	
	EXEC @errorVar = 
		GetUserProfile 
			@UserId, 
			@DefaultEventId OUT, 
			@IsSimpleEventsModel OUT, 
			@CoordFormat OUT, 
			@DefHemisphereNS OUT, 
			@DefHemisphereEW OUT, 
			@ShowUserMessagesByDefault OUT
	   
	IF( @errorVar = 0 AND @IsSimpleEventsModel = 1 )
	BEGIN
		EXEC @errorVar = EnsureDefaultTask @UserId, @DefaultEventId OUT 
	END
	
	IF( @errorVar = 0 )
	BEGIN
		-- Find free automatic group name for the user first
		DECLARE @GroupName NVARCHAR(256)
		DECLARE @groupNum INT
		SET @groupNum = 1
		WHILE(0=0)
		BEGIN
			SET @GroupName = 'Group ' + dbo.ZeroPaddedString(@groupNum, 2)
			IF( NOT EXISTS( SELECT * FROM [Group] WHERE UserId=@UserId AND [Name]=@GroupName)) BREAK
			
			SET @groupNum = @groupNum + 1
		END

		INSERT INTO [Group] (Name, UserId, EventId, [Version], DisplayUserMessages) 
			VALUES (@GroupName, @UserId, @DefaultEventId, 0, @ShowUserMessagesByDefault)
		SET @ErrorVar = @@ERROR

		IF( @ErrorVar = 0 )
			SET @NewGroupId = SCOPE_IDENTITY();
	END
	
	RETURN @ErrorVar
END	

GO
GRANT EXECUTE ON [dbo].[CreateTrackersGroup] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[DeleteEvent]
	@EventId INT
AS
BEGIN
	-- Normally what's done here should be done by cascading SET NULL rules for FK_UserProfile_Event and 
	-- FK_Group_Event. But there are already too many other cacading constaints, and it's not possible to 
	-- create that one because there will be multiple cascading update paths. So we use this proc to 
	-- achieve the same result.

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @errorVar INT
	SET @errorVar = 0
	
	BEGIN TRANSACTION
	
	UPDATE UserProfile SET DefaultEventId = NULL WHERE DefaultEventId = @EventId
	SET @errorVar = @@ERROR

	IF( @errorVar = 0 )
	BEGIN
		UPDATE [Group] SET EventId = NULL WHERE EventId = @EventId
		SET @errorVar = @@ERROR
	END
		
	IF( @errorVar = 0 )
	BEGIN
		DELETE FROM [Event] WHERE [Id] = @EventId
		SET @errorVar = @@ERROR
	END
	
	IF( @errorVar = 0 )
		COMMIT TRANSACTION
	ELSE
		ROLLBACK TRANSACTION
	
	RETURN @errorVar
END

GO
GRANT EXECUTE ON [dbo].[DeleteEvent] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE DeleteTrackerFromGroup
	@TrackerId INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- Do not need any transaction in this proc, including an outer one.
	
	DECLARE @GroupId INT
	
	SELECT @GroupId = GroupId FROM [GroupTracker] WHERE Id = @TrackerId
	
	-- See [GetGroupTrackerIds] proc for explaining the transaction. Note that read committed level is fine here,
	-- it should be REPEATABLE READ only when reading.
	-- Setting isolation level to make sure a caller didn't override it to make it lower:
	SET TRANSACTION ISOLATION LEVEL READ COMMITTED

	BEGIN TRANSACTION
	
	-- Updating version should be the first in transaction, see above.
	-- Also, do not need "IF(@GroupId IS NOT NULL)" either. If it's NULL then nothing crashes below.
	UPDATE [Group] SET [Version] = [Version] + 1 WHERE Id = @GroupId
	
	DELETE FROM GroupTracker WHERE (Id = @TrackerId)
	
	COMMIT TRANSACTION
END

GO
GRANT EXECUTE ON [dbo].[DeleteTrackerFromGroup] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[EnsureDefaultTask]
	@UserId UNIQUEIDENTIFIER,
	@DefaultEventId INT OUT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @errorVar INT
	SET @errorVar = 0

	BEGIN TRANSACTION
	
	EXEC @errorVar = GetUserProfile @UserId, @DefaultEventId OUT
	-- at this point, user profile record exsits and has either U-lock or X-lock on it.
	-- That ensures that any parallel tranasction would wait to read the data in GetUserProfile
	-- until we finish here.
    
	IF( @errorVar = 0 AND @DefaultEventId IS NULL )
	BEGIN
		EXEC @errorVar = CreateEvent @UserId, @DefaultEventId OUT
		
		IF( @errorVar = 0 )
		BEGIN
			EXEC @errorVar = SetEventAsDefault @DefaultEventId
		END
	END
	
	IF( @errorVar = 0 )
		COMMIT TRANSACTION
	ELSE
		ROLLBACK TRANSACTION
	
	RETURN @errorVar
END

GO
GRANT EXECUTE ON [dbo].[EnsureDefaultTask] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GetDefaultEventParams]
	@UserId UNIQUEIDENTIFIER ,
	@DefaultEventId INT OUT,
	@LoadedWptsCount INT OUT,
	@TaskWptsCount INT OUT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT @DefaultEventId = DefaultEventId FROM [UserProfile] WHERE UserId = @UserId
	IF( @DefaultEventId IS NULL )
	BEGIN
		SET @LoadedWptsCount = 0
		SET @TaskWptsCount = 0
	END
	ELSE
	BEGIN
		SELECT @LoadedWptsCount = COUNT(*) 
			FROM [Waypoint]
			WHERE EventId = @DefaultEventId
			
		SELECT @TaskWptsCount = COUNT(*)
			FROM [Task] T 
				JOIN [Waypoint] W ON T.WaypointId = W.Id
			WHERE W.EventId = @DefaultEventId
	END
END

GO
GRANT EXECUTE ON [dbo].[GetDefaultEventParams] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GetGroupTrackerIds]
	@GroupId INT ,
	@Version INT OUT,
	@DisplayUserMessages BIT OUT,
	@StartTs DATETIME OUT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	-- REPEATABLE READ and selecting Version first, so placing a shared lock onto the Group record.
	-- Any proc that updates a Group (add, update or delete a tracker) also updates Version field first 
	-- in transaction. As a result, many callers of this proc (readers) can run concurrently without 
	-- problems, while simultaneous update/read operations are always serialized.
	SET TRANSACTION ISOLATION LEVEL REPEATABLE READ

	BEGIN TRANSACTION	

	SELECT 
		@Version = [Version], 
		@DisplayUserMessages = DisplayUserMessages 
	FROM [Group] 
	WHERE Id = @GroupId

	SELECT @StartTs = E.StartTs
	FROM [Event] AS E INNER JOIN
		[Group] AS G ON E.Id = G.EventId
	WHERE     (G.Id = @GroupId)

	SELECT G.Name, G.TrackerForeignId
	FROM GroupTracker AS G 
	WHERE     (G.GroupId = @GroupId)

	COMMIT TRANSACTION
END

GO
GRANT EXECUTE ON [dbo].[GetGroupTrackerIds] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GetUserProfile]
	@UserId UNIQUEIDENTIFIER,
	@DefaultEventId INT = NULL OUTPUT ,
	@IsSimpleEventsModel BIT = NULL OUTPUT,
	@CoordFormat NVARCHAR(50) = NULL OUTPUT,
	@DefHemisphereNS CHAR(1) = NULL OUTPUT,
	@DefHemisphereEW CHAR(1) = NULL OUTPUT,
	@ShowUserMessagesByDefault BIT = NULL OUTPUT,
	@UserMessagesSettingIsNew BIT = NULL OUTPUT
AS
BEGIN
	-- All the transactional stuff here is not needed with 99.99987654321% probability.
	-- But let's have it here for purity.
	
    -- When this proc succesfully completes inside the transaction, 
	-- the user profile record exists and has either Exclusive or Update lock on it,
	-- which means that no other transaction can read or modify it until the end of 
	-- the current one (others wait until this one ends). Although read operations 
	-- technically can take place with the use of the NOLOCK hint or read uncommitted
	-- isolation level, but we assume that stored procs or clients don't do that.

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	-- use inner tran to make sure that the locks work properly - need it for insert/select case
	BEGIN TRAN
	
	DECLARE @errorVar INT
	DECLARE @rowCount INT	

	SELECT @errorVar = 0
	
	SELECT @DefaultEventId = [DefaultEventId],
			@IsSimpleEventsModel = [IsSimpleEventsModel],
			@CoordFormat = CoordFormat, 
			@DefHemisphereNS = DefHemisphereNS,
			@DefHemisphereEW = DefHemisphereEW,
			@ShowUserMessagesByDefault = ShowUserMessagesByDefault,
			@UserMessagesSettingIsNew = UserMessagesSettingIsNew
			-- don't forget to repeat new columns below
		FROM [UserProfile] WITH (UPDLOCK)
		WHERE [UserId] = @UserId
	
	SELECT @rowCount = @@ROWCOUNT

	IF( @rowCount = 0 )
	BEGIN
		-- INSERT still might fail if another transaction has 
		-- called this proc in parallel and both passed IF above
		
		-- INSERT will use default values for all columns, we'll have to retrieve it
		-- after insert:
		INSERT INTO UserProfile (UserId) VALUES (@UserId)
		SELECT @errorVar = @@ERROR
		
		-- now get default values from the table. No need to use WITH(UPDLOCK) as above, 
		-- because the record already has an X-lock.
		
		-- Note that we're always in transaction (see BEGIN/END TRAN in this proc), 
		-- so locks are still here at the moment of SELECT:
		SELECT @DefaultEventId = [DefaultEventId],
				@IsSimpleEventsModel = [IsSimpleEventsModel],
				@CoordFormat = CoordFormat, 
				@DefHemisphereNS = DefHemisphereNS,
				@DefHemisphereEW = DefHemisphereEW,
				@ShowUserMessagesByDefault = ShowUserMessagesByDefault,
				@UserMessagesSettingIsNew = UserMessagesSettingIsNew
			FROM [UserProfile]
			WHERE [UserId] = @UserId
	END
	
	-- No need to rollback because the above is always atomic.
	-- At the same time, nested rollbacks are a bit tricky.
	COMMIT TRAN 
	
	RETURN @ErrorVar
END

GO
GRANT EXECUTE ON [dbo].[GetUserProfile] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[SetCoordFormat]
	@UserId UNIQUEIDENTIFIER ,
	@CoordFormat NVARCHAR(50)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- This proc is most like not in transaction - if so, the worst case that could happen is error due to 
	--      non-unique UserId, and the call to this proc still will be atomic. I.e. either no change 
	--		happens due to an error, or operation succeedes.
	
	DECLARE @ErrorVar INT
	DECLARE @RowCount INT
	SET @ErrorVar = 0	

	UPDATE [UserProfile] SET [CoordFormat] = @CoordFormat WHERE UserId = @UserId
	SELECT @RowCount = @@ROWCOUNT, @ErrorVar = @@ERROR
	
	IF( @ErrorVar = 0 AND @RowCount = 0 )
	BEGIN
		-- fields that are not listed here get their default values:
		INSERT INTO [UserProfile] ([UserId], [CoordFormat]) 
			VALUES (@UserId, @CoordFormat)
		SELECT @ErrorVar = @@ERROR
	END
	
	RETURN @ErrorVar
END

GO
GRANT EXECUTE ON [dbo].[SetCoordFormat] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[SetDefHemispheres]
	@UserId UNIQUEIDENTIFIER ,
	@DefHemisphereNS CHAR(1),
	@DefHemisphereEW CHAR(1)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- This proc most likely is not in transaction - if so, the worst case that could happen is error due to 
	--      non-unique UserId, and the call to this proc still will be atomic. I.e. either no change 
	--		happens due to an error, or operation succeedes.
	
	DECLARE @ErrorVar INT
	DECLARE @RowCount INT
	SET @ErrorVar = 0	

	UPDATE [UserProfile] 
		SET [DefHemisphereNS] = @DefHemisphereNS, 
			[DefHemisphereEW] = @DefHemisphereEW
		WHERE UserId = @UserId
	SELECT @RowCount = @@ROWCOUNT, @ErrorVar = @@ERROR
	
	IF( @ErrorVar = 0 AND @RowCount = 0 )
	BEGIN
		-- fields that are not listed here get their default values:
		INSERT INTO [UserProfile] ([UserId], [DefHemisphereNS], [DefHemisphereEW]) 
			VALUES (@UserId, @DefHemisphereNS, @DefHemisphereEW)
		
		SELECT @ErrorVar = @@ERROR
	END
	
	RETURN @ErrorVar
END

GO
GRANT EXECUTE ON [dbo].[SetDefHemispheres] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[SetEventAsDefault]
	-- Add the parameters for the stored procedure here
	@EventId INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	-- This proc is either:
	--    - in transaction that ensures somehow that none else could insert the user profile record
	--		(like e.g. when it's called from EnsureDefaultTask)
	--    OR
	--	  - not in transaction - if so, the worst case that could happen is error due to 
	--      non-unique UserId, and the call to this proc still will be atomic. I.e. either no change 
	--		happens due to an error, or operation succeedes.

	DECLARE @UserId UNIQUEIDENTIFIER
	SELECT @UserId = [UserId] FROM [Event] WHERE [Id] = @EventId

	DECLARE @ErrorVar INT
	DECLARE @RowCount INT
	SET @ErrorVar = 0	
	
	IF( @UserId IS NOT NULL )
	BEGIN
		UPDATE [UserProfile] SET [DefaultEventId] = @EventId WHERE UserId = @UserId
		SELECT @RowCount = @@ROWCOUNT, @ErrorVar = @@ERROR
		
		IF( @ErrorVar = 0 AND @RowCount = 0 )
		BEGIN
			-- fields that are not listed here get their default values:
			INSERT INTO [UserProfile] ([UserId], [DefaultEventId]) 
				VALUES (@UserId, @EventId)
			SELECT @ErrorVar = @@ERROR
		END
	END
	
	RETURN @ErrorVar
END

GO
GRANT EXECUTE ON [dbo].[SetEventAsDefault] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[SetEventAsNonDefault]
	-- Add the parameters for the stored procedure here
	@EventId INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @UserId UNIQUEIDENTIFIER
	SELECT @UserId = [UserId] FROM [Event] WHERE [Id] = @EventId

	DECLARE @ErrorVar INT
	SET @ErrorVar = 0	
	
	IF( @UserId IS NOT NULL )
	BEGIN
		DECLARE @PrevDefaultEventId INT
		
		SET TRANSACTION ISOLATION LEVEL REPEATABLE READ
		BEGIN TRANSACTION
		
			SELECT @PrevDefaultEventId = [DefaultEventId] 
				FROM [UserProfile]
				WHERE UserId = @UserId
				
			IF( @PrevDefaultEventId = @EventId )
			BEGIN
				UPDATE [UserProfile] SET [DefaultEventId] = NULL
				SELECT @ErrorVar = @@ERROR
			END
		
		COMMIT TRANSACTION
		
	END
	
	RETURN @ErrorVar
END

GO
GRANT EXECUTE ON [dbo].[SetEventAsNonDefault] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[SetEventsModel]
	@UserId UNIQUEIDENTIFIER ,
	@IsSimpleEventsModel BIT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- This proc is most like not in transaction - if so, the worst case that could happen is error due to 
	--      non-unique UserId, and the call to this proc still will be atomic. I.e. either no change 
	--		happens due to an error, or operation succeedes.
	
	DECLARE @ErrorVar INT
	DECLARE @RowCount INT
	SET @ErrorVar = 0	

	UPDATE [UserProfile] SET [IsSimpleEventsModel] = @IsSimpleEventsModel WHERE UserId = @UserId
	SELECT @RowCount = @@ROWCOUNT, @ErrorVar = @@ERROR
	
	IF( @ErrorVar = 0 AND @RowCount = 0 )
	BEGIN
		-- fields that are not listed here get their default values:
		INSERT INTO [UserProfile] ([UserId], [IsSimpleEventsModel]) 
			VALUES (@UserId, @IsSimpleEventsModel)
		SELECT @ErrorVar = @@ERROR
	END
	
	RETURN @ErrorVar
END

GO
GRANT EXECUTE ON [dbo].[SetEventsModel] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE SetUserMessagesFlag
	@UserId UNIQUEIDENTIFIER,
	@ShowUserMessagesByDefault BIT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	UPDATE [UserProfile] SET ShowUserMessagesByDefault = @ShowUserMessagesByDefault WHERE [UserId]=@UserId
END

GO
GRANT EXECUTE ON [dbo].[SetUserMessagesFlag] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE SetUserMessagesSettingIsNewFlag
	@UserId UNIQUEIDENTIFIER,
	@UserMessagesSettingIsNew BIT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	UPDATE [UserProfile] SET UserMessagesSettingIsNew = @UserMessagesSettingIsNew WHERE [UserId]=@UserId
END

GO
GRANT EXECUTE ON [dbo].[SetUserMessagesSettingIsNewFlag] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE ShowUserMessagesInAllGroupsForUser
	@UserId UNIQUEIDENTIFIER
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	UPDATE [Group] SET DisplayUserMessages = 1 WHERE [UserId]=@UserId
END

GO
GRANT EXECUTE ON [dbo].[ShowUserMessagesInAllGroupsForUser] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[UpdateEventStartTs]
	@EventId INT ,
	@StartTs DATETIME
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	-- See [GetGroupTrackerIds] proc for explaining the transaction. Note that read committed level is fine here,
	-- it should be REPEATABLE READ only when reading.
	-- Setting isolation level to make sure a caller didn't override it to make it lower:
	SET TRANSACTION ISOLATION LEVEL READ COMMITTED

	BEGIN TRANSACTION
	
	-- updating version should be the first in transaction to put an update lock onto it
	UPDATE [Group] SET [Version] = [Version] + 1 WHERE EventId = @EventId

	UPDATE [Event] SET StartTs = @StartTs WHERE Id = @EventId
	
	COMMIT TRANSACTION
END

GO
GRANT EXECUTE ON [dbo].[UpdateEventStartTs] TO [flytrace_sp_Execute] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE UpdateTrackerInGroup
	@TrackerId INT,
	@Name NVARCHAR(256)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- Do not need any transaction in this proc, including an outer one.
	
	DECLARE @GroupId INT
	
	SELECT @GroupId = GroupId FROM [GroupTracker] WHERE Id = @TrackerId

	-- See [GetGroupTrackerIds] proc for explaining the transaction. Note that read committed level is fine here,
	-- it should be REPEATABLE READ only when reading.
	-- Setting isolation level to make sure a caller didn't override it to make it lower:
	SET TRANSACTION ISOLATION LEVEL READ COMMITTED

	BEGIN TRANSACTION
	
	-- Updating version should be the first in transaction, see above.
	-- Also, do not need "IF(@GroupId IS NOT NULL)" either. If it's NULL then nothing crashes below.
	UPDATE [Group] SET [Version] = [Version] + 1 WHERE Id = @GroupId
	
	UPDATE GroupTracker SET Name = @Name WHERE (Id = @TrackerId)
	
	COMMIT TRANSACTION
END

GO
GRANT EXECUTE ON [dbo].[UpdateTrackerInGroup] TO [flytrace_sp_Execute] AS [dbo]
GO
