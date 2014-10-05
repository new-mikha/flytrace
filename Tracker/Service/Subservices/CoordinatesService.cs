﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Web;

using FlyTrace.LocationLib;
using FlyTrace.Service.Properties;

namespace FlyTrace.Service.Subservices
{
  public interface ICoordinatesService
  {
    IAsyncResult BeginGetCoordinates( int group, string clientSeed, AsyncCallback callback, object state );

    GroupData EndGetCoordinates( IAsyncResult asyncResult );
  }

  public class CoordinatesService : TrackerServiceBase, ICoordinatesService
  {
    private readonly string clientSeed;

    public CoordinatesService( int group, string clientSeed )
      : base( group )
    {
      this.clientSeed = clientSeed;
    }

    public IAsyncResult BeginGetCoordinates( int unused1, string unused2, AsyncCallback callback, object state )
    {
      AsyncChainedState<GroupData> asyncChainedState = new AsyncChainedState<GroupData>( callback, state );

      return BeginGetCoordinates( asyncChainedState );
    }

    public IAsyncResult BeginGetCoordinates( AsyncChainedState<GroupData> asyncChainedState )
    {
      Global.ConfigureThreadCulture( );

      int callCount = Interlocked.Increment( ref SimultaneousCallCount );
      try
      {
        /* TODO
         * A bottleneck: too many object are created during the call. Object pools should be used instead of "new".
         * But not right now :)
         */

        if ( IncrLog.IsInfoEnabled )
          IncrLog.InfoFormat(
            "Call id {0}, for group {1}, client seed is \"{2}\"", CallId, Group, this.clientSeed );

        LogCallCount( callCount );

        if ( Log.IsDebugEnabled )
        {
          string testId = "";
          string[] val = HttpContext.Current.Request.Headers.GetValues( "User-Agent" );
          if ( val != null &&
               val.Length == 1 &&
               val[0].Contains( "FtTickle_" ) )
          {
            testId = val[0];
          }

          Log.DebugFormat( "Getting coordinates for group {0}, client seed \"{1}\", call id {2} ({3}), call count: {4}",
                              Group, this.clientSeed, CallId, testId, callCount );
        }


        GroupFacade.BeginGetGroupTrackerIds( Group, OnEndGettingGroupTrackerIds, asyncChainedState );

        return asyncChainedState.FinalAsyncResult;
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Error in BeginGetCoordinates: {0}", exc );
        throw;
      }
    }

    private void OnEndGettingGroupTrackerIds( IAsyncResult ar )
    {
      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Finishing getting group trackers id for coords, sync flag {0}...", ar.CompletedSynchronously );

      AsyncChainedState<GroupData> asyncChainedState = ( AsyncChainedState<GroupData> ) ar.AsyncState;

      if ( Log.IsDebugEnabled )
        Log.DebugFormat(
          "Finishing getting group trackers ids with call id {0}, group {1}...", CallId, Group );

      asyncChainedState.CheckSynchronousFlag( ar.CompletedSynchronously );

      try
      {
        int actualGroupVersion;
        bool showUserMessages;
        DateTime? groupStartTs;

        List<TrackerId> trackerIds =
          GroupFacade.EndGetGroupTrackerIds
          (
            ar,
            out actualGroupVersion,
            out showUserMessages,
            out groupStartTs
           );

        if ( Log.IsDebugEnabled )
        {
          TimeSpan timespan = DateTime.UtcNow - CallStartTime;
          Log.DebugFormat(
            "Got {0} trackers ids for call id {1}, group {2} with version {3} in {4} ms, getting their data now...",
            trackerIds.Count, CallId, Group, actualGroupVersion, ( int ) timespan.TotalMilliseconds );
        }

        TrackerDataManager2 dataManager = TrackerDataManager2.Singleton;

        TrackerStateHolder[] holders = new TrackerStateHolder[trackerIds.Count];
        RevisedTrackerState[] snapshots = new RevisedTrackerState[trackerIds.Count];
        List<CoordResponseItem> resultTrackers = new List<CoordResponseItem>( trackerIds.Count );

        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Acquiring read lock for call id {0}, group {1}...", CallId, Group );

        dataManager.RwLock.AttemptEnterReadLock( );
        try
        {
          #region Why under read lock
          // under read lock to make sure that following situation is avoided in incremental update:
          // 
          // Read | Write
          //      |
          // A1   | 
          // B2   | 
          // C3   | 
          //      | C4
          //      | D5
          // D5   |
          // 
          // Here Read is this thread; Write is thread where Snapshot is set; A,B,C,etc are trackers; and 1,2,3,etc 
          // are revisions. If this happens, maximum revision of snapshots collectons would be 5, so newer C4 
          // position snapshot (missed in this call where C3 is returned) will be missed on next incremental 
          // updates call from the same client.
          // 
          // In other words, reading of several Snapshot has to be atomic here.
          #endregion

          if ( Log.IsDebugEnabled )
            Log.DebugFormat( "Inside read lock for call id {0}, group {1}...", CallId, Group );

          int notNullCount = 0;
          for ( int i = 0; i < trackerIds.Count; i++ )
          {
            TrackerId trackerId = trackerIds[i];
            TrackerStateHolder trackerStateHolder;

            // so i'th elements might be null if the tracker in not in dataManager yet:
            if ( dataManager.Trackers.TryGetValue( trackerId.ForeignId, out trackerStateHolder ) )
            {
              notNullCount++;
              holders[i] = trackerStateHolder;
              snapshots[i] = trackerStateHolder.Snapshot;
            }
          }

          if ( Log.IsDebugEnabled )
            Log.DebugFormat(
              "Got {0} tracker(s) ({1} not null) under read lock for call id {2}",
              trackerIds.Count, notNullCount, CallId );
        }
        finally
        {
          dataManager.RwLock.ExitReadLock( );
          if ( Log.IsDebugEnabled )
            Log.DebugFormat( "Left lock in GetTrackerIdResponse for call id {0}", CallId );
        }

        if ( holders.Any( h => h == null ) )
        { // add it in a separate thread to return the call asap (adding might be blocked by another thread)
          ThreadPool.QueueUserWorkItem( unused => dataManager.AddMissingTrackers( trackerIds ) );
        }

        DateTime? dtNewestForeignTime = null;
        double newestLat = 0;
        double newestLon = 0;

        // If the client seed is valid for incremental update, call below extracts a time threshold 
        // from there to compare with snapshot.ModificationTime. Otherwise null is returned:
        int? thresholdRevision = null;

        // If any DataRevision is null it's an emergency mode and isAlwaysFullGroup should be true anyway. 
        // But make sure it really true.
        int nullSnapshotsCount = snapshots.Count( sn => sn == null || sn.DataRevision == null );

        // if any snaphot is null then hold incremental stuff for the moment. 
        bool isFullGroup = dataManager.IsAlwaysFullGroup || nullSnapshotsCount > 0;

        bool isDebugFullGroup = false;

        double incrDebugRatio = Settings.Default.IncrDebugRatio;

        if ( incrDebugRatio > 0.0 )
        {
          if ( incrDebugRatio >= 1.0 )
            isDebugFullGroup = true;
          else
          {
            lock ( debugRnd )
            {
              isDebugFullGroup = debugRnd.NextDouble( ) <= incrDebugRatio;
            }
          }
        }

        Thread.MemoryBarrier( ); // make sure isAlwaysFullGroup read once only, because it might be set in another thread

        if ( !isFullGroup && trackerIds.Any( ) )
          thresholdRevision = TryParseThresholdRevision( actualGroupVersion );

        if ( IncrLog.IsInfoEnabled )
        {
          IncrLog.InfoFormat(
            "Call id {0}, input threshold revision {1}, isFullGroup {2}, null snapshots count {3}",
            CallId,
            thresholdRevision,
            isFullGroup,
            nullSnapshotsCount
          );
        }

        int nextThresholdRevision = 0;
        int incrLogicIncludeCount = 0;

        for ( int i = 0; i < snapshots.Length; i++ )
        {
          string trackerName = trackerIds[i].Name;
          RevisedTrackerState nullableSnapshot = snapshots[i]; // null means that position is not retrieved yet from the foreign server 

          if ( nullableSnapshot != null && nullableSnapshot.DataRevision != null )
            nextThresholdRevision = Math.Max( nextThresholdRevision, nullableSnapshot.DataRevision.Value );

          bool isIncluded = false;

          bool includeByNormalIncrLogic =
            nullableSnapshot == null ||
            nullableSnapshot.DataRevision == null ||
            thresholdRevision == null ||
            nullableSnapshot.DataRevision.Value > thresholdRevision.Value;

          if ( includeByNormalIncrLogic )
            incrLogicIncludeCount++;

          if ( includeByNormalIncrLogic || isDebugFullGroup )
          {
            CoordResponseItem coordResponseItem =
              TrackerFromTrackerSnapshot
              (
                trackerName,
                nullableSnapshot,
                includeByNormalIncrLogic,
                showUserMessages,
                groupStartTs
              );

            resultTrackers.Add( coordResponseItem );
            isIncluded = true;

            if ( coordResponseItem.ShouldSerializeTs( ) ) // i.e. if lat, lon are not zero
            {
              if ( dtNewestForeignTime == null ||
                   dtNewestForeignTime.Value < coordResponseItem.Ts )
              {
                dtNewestForeignTime = coordResponseItem.Ts;
                newestLat = coordResponseItem.Lat;
                newestLon = coordResponseItem.Lon;
              }
            }

            TrackerStateHolder trackerStateHolder = holders[i];
            if ( trackerStateHolder != null )
            { // Interlocked used to make sure the operation is atomic:
              Interlocked.Exchange( ref trackerStateHolder.AccessTimestamp, DateTime.UtcNow.ToFileTime( ) );
            }
          }

          #region Log

          if ( IncrLog.IsInfoEnabled )
          {
            if ( nullableSnapshot == null )
            {
              // DebugFormat is not a mistake here despite IsInfoEnabled above
              IncrLog.DebugFormat(
                "Call id {0}, got NULL tracker {1} - {2} (included as it's null)",
                CallId, trackerIds[i].Name, trackerIds[i].ForeignId );
            }
            else if ( thresholdRevision.HasValue && ( IncrLog.IsDebugEnabled || includeByNormalIncrLogic ) )
            { // should be InfoFormat despite IsDebugEnabled above
              IncrLog.InfoFormat
              (
                "Call id {0}, got tracker {1} - {2} with foreign time {3}, refresh time {4}, revision {5} ({6}), included: {7}",
                CallId,
                trackerIds[i].Name,
                trackerIds[i].ForeignId,
                nullableSnapshot.Position != null ? nullableSnapshot.Position.CurrPoint.ForeignTime.ToString( ) : null,
                nullableSnapshot.RefreshTime,
                nullableSnapshot.DataRevision,
                nullableSnapshot.UpdatedPart,
                isIncluded
              );
            }
          }
          #endregion
        }

        // Update statistics for the group: number of calls, latest coords
        UpdateStatFields( dtNewestForeignTime, newestLat, newestLon );

        GroupData result;

        result.Trackers = resultTrackers.Count > 0 ? resultTrackers : null;

        result.IncrSurr = isDebugFullGroup;

        if ( isFullGroup )
        {
          result.Res = null;
          result.Src = null;
        }
        else
        {
          result.Res = actualGroupVersion + ";" + nextThresholdRevision;

          if ( thresholdRevision == null )
          {
            result.Src = null;
          }
          else
          {
            result.Src = this.clientSeed;

            if ( incrLogicIncludeCount == 0 || result.Src == result.Res )
            {
              if ( !isDebugFullGroup )
              {
                // In "no update case" both and any of the checks above should be true.
                // At this point's it that case, so check that none of them is false:
                if ( incrLogicIncludeCount != 0 || result.Src != result.Res )
                {
                  string errMessage =
                    string.Format(
                      "Incremental update error, reverting to full groups. Group id {0}, call id {1}, {2} tracker(s) in total, and returning {3} in this call, src seed is {4}, res seed is {5}",
                      Group,
                      CallId,
                      holders.Length,
                      resultTrackers.Count,
                      result.Src,
                      result.Res
                    );
                  IncrLog.Fatal( errMessage );

                  dataManager.AdminAlerts["Incremental Logic Error"] = errMessage;
                  dataManager.IsAlwaysFullGroup = true;

                  throw new ApplicationException( "Incremental updates error" ); // will go to map page status string, so don't make it lengthy
                }
              }

              // reduce data in result struct to save amount of data returned in most cases:
              result.Src = null;
              result.Res = "NIL";
            }
          }
        }

        result.StartTs =
          result.Src == null
          ? groupStartTs
          : null;

        if ( IncrLog.IsInfoEnabled )
        {
          IncrLog.InfoFormat(
            "Call id {0}, {1} tracker(s) in total, and returning {2} in this call ({3} by incr.logic). Res.seed is \"{4}\"",
            CallId,
            snapshots.Length,
            resultTrackers.Count,
            incrLogicIncludeCount,
            result.Res
          );
        }

        if ( Log.IsDebugEnabled )
        {
          TimeSpan timespan = DateTime.UtcNow - CallStartTime;
          Log.DebugFormat(
            "Finishing EndGetCoordinates, call id {0}, got {1} trackers, took {2} ms.",
            CallId,
            resultTrackers.Count,
            ( int ) timespan.TotalMilliseconds
          );
        }

        result.Ver = Settings.Default.Version;

        result.CallId = CallId;

        asyncChainedState.SetAsCompleted( result );
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "GetTrackerIdResponseForCoords: call id {0}: {1}", CallId, exc );
        asyncChainedState.SetAsCompleted( exc );
      }
    }

    public GroupData EndGetCoordinates( IAsyncResult asyncResult )
    {
      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Entering EndGetCoordinates for call id {0}, group {1}...", CallId, Group );

      try
      {
        AsyncResult<GroupData> groupDataAsyncResult = ( AsyncResult<GroupData> ) asyncResult;

        GroupData result = groupDataAsyncResult.EndInvoke( );

        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Finishing EndGetCoordinates, for call id {0}, group {1} got result: {2}",
            CallId, Group, result );

        return result;
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Error in EndGetCoordinates, call id {0}: {1}", CallId, exc.Message );
        throw;
      }
      finally
      {
        Interlocked.Decrement( ref SimultaneousCallCount );
      }
    }



    #region Incremental update algorithm explanation
    /* 
     * Returning full data for all requested trackers from EndGetCoordinates doesn't make much sense for a client that 
     * already received exactly the same data. It generates too much traffic for mobile clients with bad and/or expensive 
     * connection, which became especially important after introducing UsrMsg field containing custom user text that could
     * be quite long.
     * 
     * Incremental update was introduced to solve this problem. On the first request a client receives full actual data for 
     * the requested group from the server. This includes a "seed", a string that the client should pass back to the server 
     * with its next request (in first request client just passes a null string, so the server knows that it's a first request 
     * for that client). It is basically a version that allows the server to filter out only those trackers on next request
     * that have "newer" verions than the one of the previous request. So if no change happens then the client receieves just a 
     * small response data saying "no change". But if there is a change to some trackers then in the incremental update server 
     * returns only those trackers that have changed after the client's seed version.
     * 
     * Figuring out the actual response for an incremental update has several steps.
     * 
     * First, the server should know if the set of trackers on the client is the same as it is now in DB for the group the 
     * client is requesting. A new tracker could be added to the group, or another could be removed, or renamed, or many of 
     * these happened to several of the group's trackers at the same time. To check that, any of those actions increment the 
     * corresponding group version. This group version is returned as a part of the database request to get a set of tracker 
     * IDs for the group (see CallData.ActualGroupVersion and how it's set). And the group version is also a separate part of 
     * the seed. If a group version from the seed is not equal to CallData.ActualGroupVersion then it's not an incremental
     * update and full actual group data is returned with value of GroupData.IsIncr to false, thus forcing the client to update 
     * its set of trackers.
     * 
     * Next, a version data for actual trackers needs to be specified in a seed. A trivial solution would be to choose time
     * of a tracker creation, and then take a maximum time of trackers returning in the call. However, a time on the server could
     * be adjusted which would stuff the things up. So instead of time a revision number is used. This is a singleton number that 
     * increments with each update to any tracker. When all trackers are ready to be returned, a maximum revison number of returned
     * trackers is written to the call returning seed. On subsequent calls, revision number from client is compared with actual
     * (potentially changed) revisions of trackers ready to be returned, and only fresh updates are returned.
     * 
     */
    #endregion

    private readonly Random debugRnd = new Random( );

    private void UpdateStatFields( DateTime? dtNewestForeignTime, double newestLat, double newestLon )
    {
      Interlocked.Increment( ref TrackerDataManager2.Singleton.AdminAlerts.CoordAccessCount );

      DateTime updateStart = DateTime.UtcNow;

      string sql;
      if ( dtNewestForeignTime == null )
      {
        sql =
          "UPDATE [Group] SET " +
          "         [PageUpdatesNum] = [PageUpdatesNum] + 1" +
          " WHERE [Id] = @GroupId";
      }
      else
      {
        sql =
          "UPDATE [Group] SET " +
          "         [NewestCoordTs] = @NewestCoordTime, " +
          "         [NewestLat] = @NewestLat, " +
          "         [NewestLon] = @NewestLon, " +
          "         [PageUpdatesNum] = [PageUpdatesNum] + 1" +
          " WHERE [Id] = @GroupId";
      }

      string connString = Data.GetConnectionString( );

      SqlConnection sqlConn = new SqlConnection( connString );
      sqlConn.Open( );
      try
      {
        // Can't wrap sqlCmd into using because it's asynchronous
        SqlCommand sqlCmd = new SqlCommand( sql, sqlConn );

        // Use parameters rather than format params into SQL statement string to allow SQL Server cache 
        //    execution plan for the query.
        sqlCmd.Parameters.Add( "@GroupId", System.Data.SqlDbType.Int );
        sqlCmd.Parameters[0].Value = Group;

        if ( dtNewestForeignTime != null )
        {
          sqlCmd.Parameters.Add( "@NewestCoordTime", System.Data.SqlDbType.DateTime );
          sqlCmd.Parameters["@NewestCoordTime"].Value = dtNewestForeignTime.Value;

          sqlCmd.Parameters.Add( "@NewestLat", System.Data.SqlDbType.Float );
          sqlCmd.Parameters["@NewestLat"].Value = newestLat;

          sqlCmd.Parameters.Add( "@NewestLon", System.Data.SqlDbType.Float );
          sqlCmd.Parameters["@NewestLon"].Value = newestLon;
        }

        sqlCmd.BeginExecuteNonQuery( UpdateNewestCoordCallback, sqlCmd );
      }
      catch
      {
        sqlConn.Close( );
        throw;
      }

      if ( Log.IsDebugEnabled )
      {
        TimeSpan updateTime = DateTime.UtcNow - updateStart;
        Log.DebugFormat( "Stat fields updated in {0} ms", ( int ) updateTime.TotalMilliseconds );
      }
    }

    private void UpdateNewestCoordCallback( IAsyncResult ar )
    {
      SqlCommand sqlCmd = ( SqlCommand ) ar.AsyncState;

      // don't need a result, so just finalize call & log an exception if it happens.
      // VERY IMPORTANT: close the connection when done.
      try
      {
        try
        {
          sqlCmd.EndExecuteNonQuery( ar );
        }
        finally
        {
          sqlCmd.Connection.Close( );
        }
      }
      catch ( Exception exc )
      {
        Log.Error( "Can't update Newest* fields & PageUpdatesNum field", exc );
      }
    }

    private CoordResponseItem TrackerFromTrackerSnapshot
    (
      string trackerName,
      TrackerState snapshot,
      bool incrTest,
      bool showUserMessage,
      DateTime? startTs
    )
    {
      CoordResponseItem result = default( CoordResponseItem );

      result.Name = trackerName;

      if ( snapshot == null )
      {
        result.Type = "wait";
      }
      else
      {
        if ( snapshot.Position != null )
        {
          result.Lat = snapshot.Position.CurrPoint.Latitude;
          result.Lon = snapshot.Position.CurrPoint.Longitude;
          result.Type = snapshot.Position.Type;
          result.Ts = snapshot.Position.CurrPoint.ForeignTime;
          result.IsOfficial = false; // obsolete field
          if ( showUserMessage )
            result.UsrMsg = snapshot.Position.UserMessage;
          result.Age = CalcAge( snapshot.Position.CurrPoint.ForeignTime );

          if ( snapshot.Position.PreviousPoint != null )
          {
            if ( startTs == null ||
                 snapshot.Position.PreviousPoint.ForeignTime > startTs.Value )
            {
              result.PrevLat = snapshot.Position.PreviousPoint.Latitude;
              result.PrevLon = snapshot.Position.PreviousPoint.Longitude;
              result.PrevTs = snapshot.Position.PreviousPoint.ForeignTime;
              result.PrevAge = CalcAge( snapshot.Position.PreviousPoint.ForeignTime );
            }
          }

          if ( startTs.HasValue &&
               snapshot.Position.CurrPoint.ForeignTime < startTs.Value )
          {
            result.IsHidden = true;
          }
        }

        if ( snapshot.Error != null )
        {
          result.Error = snapshot.Error.Message;
        }
      }

      result.IncrTest = incrTest;

      return result;
    }

    public static int CalcAge( DateTime time )
    {
      if ( time == default( DateTime ) )
        return 0;

      TimeSpan locationAge = DateTime.UtcNow - time;
      return Math.Max( 0, ( int ) locationAge.TotalSeconds ); // to fix potential error in this server time settings
    }

    /// <summary>
    /// A valid client seed looks like that: "25;54215" where 1st value is the group version,
    /// and 2nd value is the maximum client tracker revision, both received by the client earlier by
    /// a similar call to this web service. Result is not null only if the group version from client seed 
    /// is equal to the current actual group version kept in Seed.ActualGroupVersion. There are also some other
    /// checks to ensure that the extracted revision is healthy. If anything's wrong then null returned,
    /// and as a result the client receives a full actual group info.
    /// </summary>
    /// <returns></returns>
    public int? TryParseThresholdRevision( int actualGroupVersion )
    {
      if ( this.clientSeed == null )
        return null;

      string[] elements = this.clientSeed.Split( ';' );
      if ( elements.Length != 2 )
        return null;

      {
        int clientGroupVersion;

        if ( !int.TryParse( elements[0], out clientGroupVersion ) )
          return null;

        if ( clientGroupVersion < 0 )
          return null;

        if ( clientGroupVersion != actualGroupVersion )
        {
          if ( IncrLog.IsInfoEnabled )
          {
            IncrLog.InfoFormat
            (
              "Call id {0}, actual group version ({1}) is different from one came from the client ({2}), so this call is not incremental",
              CallId,
              actualGroupVersion,
              clientGroupVersion
            );
          }

          return null;
        }
      }

      {
        int thresholdRevision;

        if ( !int.TryParse( elements[1], out thresholdRevision ) )
          return null;

        if ( thresholdRevision < 0 )
          return null;

        return thresholdRevision;
      }
    }

  }
}