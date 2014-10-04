using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;

namespace FlyTrace.Service.Services
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

    public IAsyncResult BeginGetCoordinates( int group, string clientSeed, AsyncCallback callback, object state )
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
            "Call id {0}, for group {1}, client seed is \"{2}\"", this.callId, this.group, this.clientSeed );

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
                              this.group, this.clientSeed, this.callId, testId, callCount );
        }


        this.groupFacade.BeginGetGroupTrackerIds( group, GetTrackerIdResponse, asyncChainedState );

        return asyncChainedState.FinalAsyncResult;
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Error in BeginGetCoordinates: {0}", exc.ToString( ) );
        throw;
      }
    }

    private void GetTrackerIdResponse( IAsyncResult ar )
    {
      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Finishing getting group trackers id for coords, sync flag {0}...", ar.CompletedSynchronously );

      var asyncChainedState = ( AsyncChainedState<GroupData> ) ar.AsyncState;

      if ( Log.IsDebugEnabled )
        Log.DebugFormat(
          "Finishing getting group trackers ids with call id {0}, group {1}...", this.callId, this.group );

      asyncChainedState.CheckSynchronousFlag( ar.CompletedSynchronously );

      try
      {
        int actualGroupVersion;
        bool showUserMessages;
        DateTime? groupStartTs;

        List<TrackerId> trackerIds =
          this.groupFacade.EndGetGroupTrackerIds
          (
            ar,
            out actualGroupVersion,
            out showUserMessages,
            out groupStartTs
           );

        if ( Log.IsDebugEnabled )
        {
          TimeSpan timespan = DateTime.UtcNow - this.callStartTime;
          Log.DebugFormat(
            "Got {0} trackers ids for call id {1}, group {2} with version {3} in {4} ms, getting their data now...",
            trackerIds.Count, this.callId, this.group, actualGroupVersion, ( int ) timespan.TotalMilliseconds );
        }

        TrackerDataManager2 dataManager = TrackerDataManager2.Singleton;

        TrackerStateHolder[] holders = new TrackerStateHolder[trackerIds.Count];

        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Acquiring lock in TrackerIdsReady for call id {0}, group {1}...", this.callId, this.group );

        dataManager.RwLock.AttemptEnterReadLock( );
        try
        {
          if ( Log.IsDebugEnabled )
            Log.DebugFormat( "Inside lock in GetTrackerIdResponse for call id {0}...", this.callId, this.group );

          for ( int i = 0; i < trackerIds.Count; i++ )
          {
            TrackerId trackerId = trackerIds[i];

            TrackerStateHolder trackerStateHolder;
            dataManager.Trackers.TryGetValue( trackerId.ForeignId, out trackerStateHolder );
            holders[i] = trackerStateHolder; // might be null
          }
        }
        finally
        {
          dataManager.RwLock.ExitReadLock( );
          if ( Log.IsDebugEnabled )
            Log.DebugFormat( "Left lock in GetTrackerIdResponse for call id {0}", this.callId );
        }

        if ( holders.Any( h => h == null ) )
        { // add it in a separate thread to return the call asap (adding might be blocked by another thread)
          ThreadPool.QueueUserWorkItem( unused => dataManager.AddMissingTrackers( trackerIds ) );
        }



        DateTime? dtNewestForeignTime = null;
        double newestLat = 0;
        double newestLon = 0;

        List<CoordResponseItem> resultTrackers = new List<CoordResponseItem>( callData.TrackerStateHolders.Length );

        List<RevisedTrackerState> snapshots;

        lock ( this.snapshotAccessSync )
        { /* lock to make sure that following situation is avoided in incremental update:
           * 
           * Read | Write
           *      |
           * A1   | 
           * B2   | 
           * C3   | 
           *      | C4
           *      | D5
           * D5   |
           * 
           * Here Read is this thread; Write is thread where Snapshot is set; A,B,C,etc are trackers; and 1,2,3,etc 
           * are revisions. If this happens, maximum revision of snapshots collectons would be 5, so newer C4 
           * position snapshot (missed in this call where C3 is returned) will be missed on next incremental 
           * updates call from the same client.
           * 
           * In other words, reading of several Snapshot has to be atomic here.
           */

          snapshots =
            callData
            .TrackerStateHolders
            .Select( tsh => tsh.Snapshot )
            .ToList( );
        }

        // If the client seed is valid for incremental update, call below extracts a time threshold 
        // from there to compare with snapshot.ModificationTime. Otherwise null is returned:
        int? thresholdRevision = null;

        // If any DataRevision is null it's an emergency mode and isAlwaysFullGroup should be true anyway. 
        // But make sure it really true.
        int nullSnapshotsCount = snapshots.Count( sn => sn == null || sn.DataRevision == null );

        // if any snaphot is null then hold incremental stuff for the moment. 
        bool isFullGroup = this.isAlwaysFullGroup || nullSnapshotsCount > 0;

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

        if ( !isFullGroup )
          thresholdRevision = callData.TryParseThresholdRevision( );

        if ( IncrLog.IsInfoEnabled )
        {
          IncrLog.InfoFormat(
            "Call id {0}, input threshold revision {1}, isFullGroup {2}, null snapshots count {3}",
            callData.CallId,
            thresholdRevision == null ? "null" : thresholdRevision.ToString( ),
            isFullGroup,
            nullSnapshotsCount
          );
        }

        int nextThresholdRevision = 0;

        int incrLogicIncludeCount = 0;

        for ( int i = 0; i < callData.TrackerStateHolders.Length; i++ )
        {
          string trackerName = callData.TrackerIds[i].Name;
          TrackerStateHolder trackerStateHolder = callData.TrackerStateHolders[i];

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
                callData.ShowUserMessages,
                callData.StartTs
              );

            resultTrackers.Add( coordResponseItem );
            isIncluded = true;

            if ( coordResponseItem.ShouldSerializeTs( ) )
            {
              if ( dtNewestForeignTime == null ||
                   dtNewestForeignTime.Value < coordResponseItem.Ts )
              {
                dtNewestForeignTime = coordResponseItem.Ts;
                newestLat = coordResponseItem.Lat;
                newestLon = coordResponseItem.Lon;
              }
            }

            // Interlocked used to make sure the operation is atomic:
            Interlocked.Exchange( ref trackerStateHolder.AccessTimestamp, DateTime.UtcNow.ToFileTime( ) );
          }

          #region Log

          if ( IncrLog.IsInfoEnabled )
          {
            if ( nullableSnapshot == null )
            {
              // DebugFormat is not a mistake here despite IsInfoEnabled above
              IncrLog.DebugFormat( "Call id {0}, got NULL tracker {1} - {2} (included as it's null)", callData.CallId, callData.TrackerIds[i].Name, callData.TrackerIds[i].ForeignId );
            }
            else if ( thresholdRevision.HasValue && ( IncrLog.IsDebugEnabled || includeByNormalIncrLogic ) )
            { // should be InfoFormat despite IsDebugEnabled above
              IncrLog.InfoFormat
              (
                "Call id {0}, got tracker {1} - {2} with foreign time {3}, refresh time {4}, revision {5} ({6}), included: {7}",
                callData.CallId,
                callData.TrackerIds[i].Name,
                callData.TrackerIds[i].ForeignId,
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
        UpdateStatFields( callData.Group, dtNewestForeignTime, newestLat, newestLon );

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
          result.Res = GetResultSeed( callData, nextThresholdRevision );

          if ( thresholdRevision == null )
          {
            result.Src = null;
          }
          else
          {
            result.Src = callData.SourceSeed;

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
                      callData.Group,
                      callData.CallId,
                      callData.TrackerStateHolders.Length,
                      resultTrackers.Count,
                      result.Src,
                      result.Res
                    );
                  IncrLog.Fatal( errMessage );

                  AdminAlerts["Incremental Logic Error"] = errMessage;

                  this.isAlwaysFullGroup = true;
                  throw new ApplicationException( "Incremental updates error" ); // will go to map page status string, so don't make it lengthy
                }
              }

              // reduce data in result struct to save amount of data returned in most cases:
              result.Src = null;
              result.Res = "NIL";
            }
          }
        }

        if ( result.Src == null )
        {
          result.StartTs = callData.StartTs;
        }
        else
        {
          result.StartTs = null;
        }

        if ( IncrLog.IsInfoEnabled )
        {
          IncrLog.InfoFormat(
            "Call id {0}, {1} tracker(s) in total, and returning {2} in this call ({3} by incr.logic). Res.seed is \"{4}\"",
            callData.CallId,
            callData.TrackerStateHolders.Length,
            resultTrackers.Count,
            incrLogicIncludeCount,
            result.Res
          );
        }

        if ( Log.IsDebugEnabled )
        {
          TimeSpan timespan = DateTime.UtcNow - callData.CallStartTime;
          Log.DebugFormat(
            "Finishing EndGetCoordinates, call id {0}, got {1} trackers, took {2} ms.",
            callId,
            resultTrackers.Count,
            ( int ) timespan.TotalMilliseconds
          );
        }

        result.Ver = Settings.Default.Version;

        result.CallId = callData.CallId;


      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "GetTrackerIdResponseForCoords: call id {0}: {1}", this.callId, exc.ToString( ) );
        asyncChainedState.SetAsCompleted( exc );
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

    private Random debugRnd = new Random( );

    private string GetResultSeed( CoordinatesRequestData callData, int nextThresholdRevision )
    {
      return
        callData.ActualGroupVersion.ToString( )
         + ";"
         + nextThresholdRevision.ToString( );
    }

    private void UpdateStatFields( int groupId, DateTime? dtNewestForeignTime, double newestLat, double newestLon )
    {
      Interlocked.Increment( ref AdminAlerts.CoordAccessCount );

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
        sqlCmd.Parameters[0].Value = groupId;

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

    public GroupData EndGetCoordinates( IAsyncResult asyncResult )
    {
      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Entering EndGetCoordinates for call id {0}, group {1}...", this.callId, this.group );

      try
      {
        var groupDataAsyncResult = ( AsyncResult<GroupData> ) asyncResult;

        GroupData result = groupDataAsyncResult.EndInvoke( );

        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Finishing EndGetCoordinates, for call id {0}, group {1} got result: {2}",
            this.callId, this.group, result );

        return result;
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Error in EndGetCoordinates, call id {0}: {1}", callId, exc.Message );
        throw;
      }
      finally
      {
        Interlocked.Decrement( ref SimultaneousCallCount );
      }
    }
  }
}