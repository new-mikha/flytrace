/******************************************************************************
 * Flytrace, online viewer for GPS trackers.
 * Copyright (C) 2011-2014 Mikhail Karmazin
 * 
 * This file is part of Flytrace.
 * 
 * Flytrace is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * Flytrace is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Linq;

using log4net;

using FlyTrace.LocationLib;
using System.Text;
using FlyTrace.Service.Properties;
using System.Data.SqlClient;
using FlyTrace.LocationLib.Data;
using log4net.Repository;
using log4net.Appender;
using FlyTrace.LocationLib.ForeignAccess;

namespace FlyTrace.Service
{
  internal class TrackerDataManager2
  {
    /* There is a number of parameters controlling how calls to the foreign servers are scheduled.
     * 
     * One of those parameters is a maximum number of simultaneous polling calls to the foreign servers. 
     * This number can be 1 or more. When it's 1, only 1 call at a time is possible (which would be VERY 
     * slow for a reasanoble amount of trackers to watch, but such config is still possible)
     * 
     * Below "calls pack" is a set of simultaneously running (overlapping) calls. Here is an example of 
     * overlapping calls starting at separate moments in time:
     * 
     * Number of     0|callllll
     * calls in the  1|...call
     * current call  2|   ..calllllll
     * pack at the   2|     ..callll
     * moment of the 2|       ..calll
     * call start    3|         ..callllllll
     *               1|           ....call
     *                 ---------------------
     *                 012345678901234567890
     *                       time, sec
     * 
     * Assuming there is a queue of required but not started yet calls, here are the parameters to schedule 
     * those calls:
     * 
     * - MaxCallsInPack: int value >=1, defines maximum number of calls in current pack. When a call in 
     *    the pack is finished, another call can be started at the time defined by parameters described 
     *    above. In the example above this parameter could be 4 (see that 4 calls at the 11th and 12th 
     *    seconds).
     *    
     * - TimeFromPrevStart: int value in milliseconds, >=0, defines how often calls can be started. A next
     *   call from the queue can be started only after this time has passed after the START of the previous
     *   one. In the example above this parameter could be 2000.  Note that the queue of required calls could 
     *   be empty when the number of calls in the pack goes below limit, that's why in the example above some 
     *   calls are started with interval of 3 or 4 seconds after prev.start. And of course a call pack could 
     *   even be empty if there is no trackers to update.
     *   
     * - CallsGap: int value in milliseconds, >=0, in use only when there is just one call per time is 
     *    possible, i.e. when MaxCallsInPack (below) is 1. If value of CallsGap > 0, next call from the 
     *    queue can be started only after this time has passed after the END of the previous one. In the 
     *    example above this parameter is ignored because MaxCallsInPack>1.
     *    
     * Below is an example where CallsGap is in use, with MaxCallsInPack=1, TimeFromPrevStart=7000 
     * and CallsGap=2000:
     *      calllll..calll...callllllll..calll...call....call....call....
     *      01234567.0123456701234567....012345670123456701234567
     * Probably it doesn't make much sense to use both TimeFromPrevStart>0 and CallsGap>0, but to keep 
     * the algorithm simple they both are allowed to be non-zero.
     * 
     * Another parameter that controls which trackers need polling calls at all, i.e. which calls form a
     * queue that need to be scheduled in according to the rules above:
     * - RefreshInterval: time in seconds, >0, specifies when a tracker requires a call
     *   to the foreign server to refresh the tracker's data. It's time span that needs to pass after previous
     *   call for the same tracker (always counted from the prev.call start)
     */

    static TrackerDataManager2( )
    {
      // TODO: remove
      Log.InfoFormat(
        "AvgAllowedMsBetweenCalls at the moment of this instance start: {0}",
        Settings.Default.AvgAllowedMsBetweenCalls
      );
    }

    /// <summary>Constructor is private to make the instance accessible only via the <see cref="Singleton"/> field.</summary>
    private TrackerDataManager2( )
    {
      try
      {
        InitRevisionPersister( );

        this.refreshThread = new Thread( RefreshThreadWorker );
        this.refreshThread.Name = "LocWorker";
        this.refreshThread.IsBackground = true;
        this.refreshThread.Start( ); // it waits until refreshThreadEvent set.

        this.refreshThreadEvent.Set( );
      }
      catch ( Exception exc )
      {
        Log.Error( "Error on starting-up", exc );
        throw;
      }

      InfoLog.Info( "Started." );
    }

    private Thread refreshThread;

    private Dictionary<ForeignId, TrackerStateHolder> trackers =
      new Dictionary<ForeignId, TrackerStateHolder>( );

    internal Dictionary<ForeignId, TrackerStateHolder> Trackers { get { return this.trackers; } }

    private AutoResetEvent refreshThreadEvent = new AutoResetEvent( false );

    private void RefreshThreadWorker( )
    {
      Global.SetUpThreadCulture( );

      // It's OK to use "new" everywhere in this method and in methods it 
      // calls, because it's doesn't hit too often.
      // LINQ and enumerators are safe here for the same reason.

      while ( true )
      {
        try
        {
          PokeLog4NetBufferingAppendersSafe( );

          List<TrackerStateHolder> trackersToRequest = WaitForTimeOfNextRequest( );

          if ( this.isStoppingWorkerThread )
            break;

          foreach ( TrackerStateHolder trackerStateHolder in trackersToRequest )
          {
            BeginRequest( trackerStateHolder );
          }
        }
        catch ( Exception exc )
        {
          Log.Error( "RefreshThreadWorker", exc );

          // Normally there should be no exception and this catch is the absolutely last resort.
          // So it's possible that such an unexpected error is not a single one and moreover 
          // it prevents the thread from waiting the reasonable time until requesting the 
          // next tracker etc. So put a Sleep here to prevent possible near-100% processor 
          // load of this thread. If the error is just something occasional, this Sleep is
          // not a problem because it wouldn't block any IO or user interaction, just the 
          // refresh would be delayed a bit:
          Thread.Sleep( 1000 );
        }
      }

      Log.Info( "Finishing worker thread..." );
    }

    private volatile bool isStoppingWorkerThread = false;

    private int requestCounter = 0;

    private List<TrackerStateHolder> WaitForTimeOfNextRequest( )
    {
      return null;
    }

    private ReaderWriterLockSlimEx rwLock = new ReaderWriterLockSlimEx( 1000 );

    private void BeginRequest( TrackerStateHolder trackerStateHolder )
    {
      LocationRequest locationRequest =
        ForeignAccessCentral
          .LocationRequestFactories[trackerStateHolder.ForeignId.Type]
          .CreateRequest( trackerStateHolder.ForeignId.Id );

      if ( !this.rwLock.TryEnterWriteLock( ) )
      {
        Log.ErrorFormat( "Can't enter read mode in BeginRequest for {0}", trackerStateHolder.ForeignId );
        return;
      }

      try
      {
        trackerStateHolder.CurrentRequest = locationRequest;
      }
      finally
      {
        this.rwLock.ExitWriteLock( );
      }

      // Despite TrackerStateHolder having CurrentRequest field, locationRequest still need to 
      // be passed as a implicit parameter because there is no guarantee that at the moment when 
      // OnEndReadLocation is called there will be no other request started. E.g. this request 
      // can be stuck, aborted, and then suddenly pop in again in OnEndReadLocation. It's unlikely
      // but still possible. So pass locationRequest to avoid any chance that EndReadLocation is
      // called on wrong locationRequest.
      var paramTuple =
        new Tuple<TrackerStateHolder, LocationRequest>( trackerStateHolder, locationRequest );

      locationRequest.BeginReadLocation( OnEndReadLocation, paramTuple );
    }

    private int trackerStateRevision;

    private void OnEndReadLocation( IAsyncResult ar )
    {
      Global.SetUpThreadCulture( );

      RevisedTrackerState newTrackerState = null;
      ForeignId foreignId = default( ForeignId );
      long? lrid = null;

      try
      {
        LocationRequest locationRequest;
        TrackerStateHolder trackerStateHolder;
        {
          var paramTuple = ( Tuple<TrackerStateHolder, LocationRequest> ) ar.AsyncState;

          trackerStateHolder = paramTuple.Item1;
          locationRequest = paramTuple.Item2;
        }

        lrid = locationRequest.Lrid;
        foreignId = trackerStateHolder.ForeignId;

        // Need to end it even if trackerStateHolder.CurrentRequest != locationRequest (see below)
        TrackerState trackerState = locationRequest.EndReadLocation( ar );

        this.rwLock.AttemptEnterUpgradeableReadLock( );
        // when we're here, other thread can be in read mode & no other can be in write or upgr. modes

        try
        {
          // It's possible that this request was just too slow, and it's not the current one any longer:
          if ( trackerStateHolder.CurrentRequest != locationRequest )
            return;

          // Do not save new one back to the persister yet because it's not write mode yet
          // (logically can do that - see above the note about current mode - but it's not nice)
          int? newRevisionToUse =
              this.revisionPersister.IsActive
                ? ( this.revisionPersister.ThreadUnsafeRevision + 1 )
                : ( int? ) null;

          // This doesn't write anything too, just a local var so far:
          RevisedTrackerState mergedResult =
            RevisedTrackerState.Merge( trackerStateHolder.Snapshot, trackerState, newRevisionToUse );

          this.rwLock.AttemptEnterWriteLock( );

          try
          { // Rolling through the write mode as quick as possible:

            // If an exception thrown before and it didn't come to this point, the request will 
            // be aborted & set to null after a timeout. But normally it's set to null here:
            trackerStateHolder.CurrentRequest = null;

            // Merge can return the old tracker if no change occured:
            if ( !ReferenceEquals( trackerStateHolder.Snapshot, mergedResult ) )
            {
              trackerStateHolder.Snapshot = newTrackerState = mergedResult;

              if ( newRevisionToUse.HasValue ) // if so the revisionPersister.IsActive==true too
                this.revisionPersister.ThreadUnsafeRevision = newRevisionToUse.Value;
            }

            trackerStateHolder.RefreshTime = DateTime.UtcNow;
          }
          finally
          {
            this.rwLock.ExitWriteLock( );
          }
        }
        finally
        {
          this.rwLock.ExitUpgradeableReadLock( );
        }
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Can't end read location for lrid {0}, {1}: {2}", lrid, foreignId, exc );
      }
      finally
      {
        // Keep logging out of rwLock to make latter as short as possible
        if ( newTrackerState != null && IncrLog.IsDebugEnabled )
          IncrLog.DebugFormat( "New data obtained by lrid {0}: {1}", lrid, newTrackerState );
      }
    }

    private static ILog Log = LogManager.GetLogger( "TDM" );

    /// <summary>
    /// Supposed to be always in at least for INFO level, i.e. don't use it too often. E.g. start/stop messages could go there.
    /// </summary>
    private static ILog InfoLog = LogManager.GetLogger( "InfoLog" );

    private static ILog IncrLog = LogManager.GetLogger( "TDM.IncrUpd" );

    // It could be just a static class, but I don't want to bother with 'static' everywhere.
    // So making it just a singleton. Note that initializing of this field shouldn't be 
    // protected by 'lock', 'volatile' or whatever, because it's guaranteed by CLR to be 
    // atomic set. No thread can use it until it's set & initialized.
    public static readonly TrackerDataManager2 Singleton = new TrackerDataManager2( );

    public AdminAlerts AdminAlerts = new AdminAlerts( );

    private bool isAlwaysFullGroup;

    private readonly RevisionPersister revisionPersister = new RevisionPersister( );

    private void InitRevisionPersister( )
    {
      if ( !Settings.Default.AllowIncrementalUpdates )
      {
        this.isAlwaysFullGroup = true;
        IncrLog.Info( "Starting in 'always full group' mode" );
        return;
      }

      try
      {

        string revisionFilePath = HttpContext.Current.Server.MapPath( @"~/App_Data/revision.bin" );
        string initWarnings;

        if ( this.revisionPersister.Init( revisionFilePath, out initWarnings ) )
        {
          // Log it as an error (while it's actually not) to make sure it's logged:
          IncrLog.InfoFormat(
            "Revgen restored from '{0}' successfuly: current value is {1}",
            revisionFilePath,
            this.revisionPersister.ThreadUnsafeRevision
          );
        }
        else
        { // If Init didn't throw an error, then it's was inited
          IncrLog.ErrorFormat(
            "Revgen failed to restore from '{0}', so re-init it starting from {1}, and will try now to update all group versions in DB",
            revisionFilePath,
            this.revisionPersister.ThreadUnsafeRevision
          );

          string connString = Data.GetConnectionString( );

          SqlConnection sqlConn = new SqlConnection( connString );
          sqlConn.Open( );
          // Can't wrap sqlCmd into using because it's asynchronous
          SqlCommand sqlCmd = new SqlCommand( "UPDATE [Group] SET [Version] = [Version] + 1", sqlConn );

          sqlCmd.ExecuteNonQuery( );

          IncrLog.Warn( "All groups versions increased after restarting Revgen." );
        }

        if ( initWarnings != null )
        {
          AdminAlerts["Revgen init warning"] = initWarnings;
        }

        AdminAlerts["Revgen initialised at"] = RevisionPersister.Revision.ToString( );
      }
      catch ( Exception exc )
      {
        IncrLog.ErrorFormat( "Can't init Revgen properly or update all groups versions: {0}", exc );

        this.isAlwaysFullGroup = true;

        if ( this.revisionPersister.IsActive )
          this.revisionPersister.Shutdown( );

        AdminAlerts["Revgen Init Error"] = exc.Message;
      }
    }

    public void Stop( )
    {
      this.isStoppingWorkerThread = true;

      int closingRevision = -1;
      if ( this.revisionPersister.IsActive )
        closingRevision = this.revisionPersister.Shutdown( );

      this.refreshThreadEvent.Set( );
      if ( this.refreshThread.Join( 30000 ) )
      {
        // Log it as error to make sure it's logged
        InfoLog.InfoFormat( "Worker thread stopped, closing revision {0}.", closingRevision );
      }
      else
      {
        Log.Error( "Can't stop the worker thread" );
      }
    }

    private int simultaneousCallCount = 0;

    public IAsyncResult BeginGetCoordinates( int group, string clientSeed, AsyncCallback callback, object state )
    {
      Global.SetUpThreadCulture( );

      int callCount = Interlocked.Increment( ref this.simultaneousCallCount );

      try
      {
        /* TODO
         * A bottleneck: too many object are created during the call. Object pools should be used instead of "new".
         * But not right now :)
         */

        CallData2 callData = new CallData2( group, clientSeed, callback, state );

        if ( IncrLog.IsInfoEnabled )
          IncrLog.InfoFormat( "Call id {0}, for group {1}, client seed is \"{2}\"", callData.CallId, callData.Group, clientSeed );

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
                              group, callData.SourceSeed, callData.CallId, testId, callCount );
        }

        callData.GroupFacade.BeginGetGroupTrackerIds( group, GetTrackerIdResponseForCoords, callData );

        return callData.FinalAsyncResult;
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Error in BeginGetCoordinates: {0}", exc.ToString( ) );
        Interlocked.Decrement( ref this.simultaneousCallCount );
        throw;
      }
    }

    private void GetTrackerIdResponseForCoords( IAsyncResult ar )
    {
      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Finishing getting group trackers id, sync flag {0}...", ar.CompletedSynchronously );

      CallData2 callData = ( CallData2 ) ar.AsyncState;

      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Finishing getting group trackers ids with call id {0}, group {1}...", callData.CallId, callData.Group );

      callData.CheckSynchronousFlag( ar.CompletedSynchronously );

      try
      {
        List<TrackerId> trackerIds =
          callData.GroupFacade.EndGetGroupTrackerIds
          (
            ar,
            out callData.ActualGroupVersion,
            out callData.ShowUserMessages,
            out callData.StartTs
           );
        callData.TrackerIds = trackerIds;

        if ( Log.IsDebugEnabled )
        {
          TimeSpan timespan = DateTime.UtcNow - callData.CallStartTime;
          Log.DebugFormat(
            "Got {0} trackers ids for call id {1}, group {2} with version {3} in {4} ms, getting their data now...",
            callData.TrackerIds.Count,
            callData.CallId,
            callData.Group,
            callData.ActualGroupVersion,
            ( int ) timespan.TotalMilliseconds
          );
        }

        TrackerIdsReady( callData );
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "GetTrackerIdResponseForCoords: call id {0}: {1}", callData.CallId, exc.ToString( ) );
        callData.SetAsCompleted( exc );
      }
    }

    /// <summary>
    /// Called when callData.TrackerIds list is ready. Gets existing or add new TrackerStateHolder for each tracker.
    /// If there are only existing trackers, calls <see cref="FinishGetCoordsCall"/> immediately.
    /// Otherwise returns and lets main working thread to fill newly added TrackerStateHolder and call 
    /// FinishGetCoordsCall when everything's ready.
    /// </summary>
    /// <param name="callData"></param>
    private void TrackerIdsReady( CallData2 callData )
    {
      callData.TrackerStateHolders = new TrackerStateHolder[callData.TrackerIds.Count];

      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Acquiring lock in TrackerIdsReady for call id {0}, group {1}...", callData.CallId, callData.Group );

      lock ( this.trackers )
      {
        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Inside lock in TrackerIdsReady for call id {0}, group {1}...", callData.CallId, callData.Group );

        for ( int i = 0; i < callData.TrackerIds.Count; i++ )
        {
          TrackerId trackerId = callData.TrackerIds[i];

          TrackerStateHolder trackerStateHolder;
          if ( !this.trackers.TryGetValue( trackerId.ForeignId, out trackerStateHolder ) )
          {
            trackerStateHolder = new TrackerStateHolder( trackerId.ForeignId ); // no data yet, so leave its Snapshot field null for the moment.
            this.trackers.Add( trackerId.ForeignId, trackerStateHolder );
          }

          callData.TrackerStateHolders[i] = trackerStateHolder;
        }
      } // lock ( this.trackers )

      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Lock has been released in TrackerIdsReady for call id {0}, group {1}...", callData.CallId, callData.Group );

      FinishGetCoordsCall( callData );
    }

    private void FinishGetCoordsCall( CallData2 callData )
    {
      try
      {
        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Call id {0}, group {1}: setting callData.SetAsCompleted", callData.CallId, callData.Group );

        callData.SetAsCompleted( callData );

        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Call id {0}, group {1}: done setting callData.SetAsCompleted", callData.CallId, callData.Group );
      }
      catch ( Exception e )
      {
        Log.ErrorFormat(
          "Call id {0}, group {1}: error: {2}", callData.CallId, callData.Group, e.ToString( )
        );
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

    public GroupData EndGetCoordinates( IAsyncResult asyncResult )
    {
      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Entering EndGetCoordinates..." );

      long callId = -1;

      try
      {
        AsyncResult<CallData2> finalAsyncResult = ( AsyncResult<CallData2> ) asyncResult;

        CallData2 callData = finalAsyncResult.EndInvoke( );

        callId = callData.CallId;

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

        return result;
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Error in EndGetCoordinates, call id {0}: {1}", callId, exc.ToString( ) );
        throw;
      }
      finally
      {
        Interlocked.Decrement( ref this.simultaneousCallCount );
      }
    }

    private string GetResultSeed( CallData2 callData, int nextThresholdRevision )
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

    private void TimerCallback( object ignored )
    {
      // wake up main thread:
      this.refreshThreadEvent.Set( );
    }
    private DateTime prevBufferingAppendersPokingTs = DateTime.UtcNow;

    /// <summary>
    /// See <see cref="PokeLog4NetBufferingAppendersSafe"/> method. Defines time between flushes.
    /// This time is in minutes;
    /// </summary>
    private const int BufferingAppendersFlushPeriod = 30;

    private void PokeLog4NetBufferingAppendersSafe( )
    {
      // Method to flush events in buffered appenders like SmtpAppender. Problem is that it can keep 
      // an unfrequent single event in the buffer until the service is stopping, even if TimeEvaluator
      // is in use. The latter would flush the buffer only when another event is coming after specified 
      // time. But if a single important event comes into the buffer, it would just stay there.
      //
      // This method solves the problem flushing each BufferingAppenderSkeleton in case it's not Lossy, 
      // after every 30 minutes.

      try
      {
        if ( ( DateTime.UtcNow - prevBufferingAppendersPokingTs ).TotalMinutes > BufferingAppendersFlushPeriod )
        {
          prevBufferingAppendersPokingTs = DateTime.UtcNow;

          // queue it into the thread pool to avoid potential delays in log4net in processing that stuff:
          ThreadPool.QueueUserWorkItem( Log4NetBufferingAppendersFlushWorker );
        }
      }
      catch ( Exception exc )
      {
        Log.Error( "PokeLog4NetBufferingAppendersSafe", exc );
      }
    }

    private void Log4NetBufferingAppendersFlushWorker( object state )
    {
      Global.SetUpThreadCulture( );
      ILog log = LogManager.GetLogger( "LogFlush" );

      string errName = "";
      try
      {
        // TODO: experimental feature, all hard-coded values to be removed later:
        DateTime destTime = DateTime.Now.AddHours( 8 );

        ILoggerRepository defaultRepository = log4net.LogManager.GetRepository( );

        foreach ( IAppender appender in defaultRepository.GetAppenders( ) )
        {
          string logName = appender.GetType( ).Name;

          errName = logName;
          if ( appender is BufferingAppenderSkeleton )
          {
            log.InfoFormat( "Flushing {0} at {1}", logName, destTime );
            BufferingAppenderSkeleton bufferingAppender = appender as BufferingAppenderSkeleton;
            if ( !bufferingAppender.Lossy )
            {
              bufferingAppender.Flush( );
            }
          }
        }
      }
      catch ( Exception exc )
      {
        log.ErrorFormat( "Can't poke buffering appenders, error happened for '{0}': {1}", errName, exc );
      }
    }

    private DateTime GetRefreshThreshold( string foreignServerType )
    {
      Dictionary<string, LocationRequestFactory> factories =
        ForeignAccessCentral.LocationRequestFactories;

      DateTime threshold =
        DateTime
        .UtcNow
        .ToUniversalTime( )
        .AddSeconds( -factories[foreignServerType].RefreshInterval );

      return threshold;
    }

    public IAsyncResult BeginGetTracks
    (
      int group,
      TrackRequestItem[] trackRequests,
      AsyncCallback callback,
      object asyncState,
      out long callId // temporary debug thing, to be removed.
    )
    {
      Global.SetUpThreadCulture( );

      int callCount = Interlocked.Increment( ref this.simultaneousCallCount );

      try
      {
        CallData2 callData = new CallData2( group, trackRequests, callback, asyncState );

        Log.DebugFormat(
          "Getting tracks for group {0}, call id {1}, tracks requested: {2}, call count: {3}",
          group,
          callData.CallId,
          trackRequests == null ? 0 : trackRequests.Length,
          callCount
        );

        callId = callData.CallId;

        if ( trackRequests == null || trackRequests.Length == 0 )
        {
          Log.DebugFormat( "Call id {0}: it's zero, so calling SetAsCompleted..." );
          callData.SetAsCompleted( callData );
          Log.DebugFormat( "Call id {0}: it's zero, so SetAsCompleted called." );
        }
        else
        {
          if ( trackRequests.Length > 10 )
          {
            throw new ApplicationException( "Number of requested full tracks can't exceed 10" );
          }

          StringBuilder names = new StringBuilder( 10 * trackRequests.Length );
          for ( int iReq = 0; iReq < trackRequests.Length; iReq++ ) // avoid "forech" in freq.operation because it means too much "new" operations
          {
            TrackRequestItem req = trackRequests[iReq];

            if ( string.IsNullOrEmpty( req.TrackerName ) )
            {
              throw new ApplicationException( "Tracker name cannot be empty." );
            }

            if ( iReq > 0 )
              names.Append( ", " );
            names.Append( req.TrackerName );
          }

          Log.InfoFormat( "Getting full track for trackers for call id {0}, group {1}: {2}", callData.CallId, group, names );

          callData.GroupFacade.BeginGetGroupTrackerIds( group, GetTrackerIdResponseForTracks, callData );
        }

        return callData.FinalAsyncResult;
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Error in BeginGetTracks: {0}", exc.ToString( ) );
        Interlocked.Decrement( ref this.simultaneousCallCount );
        throw;
      }

    }

    private void GetTrackerIdResponseForTracks( IAsyncResult ar )
    {
      Global.SetUpThreadCulture( );

      var callData = ( CallData2 ) ar.AsyncState;

      callData.CheckSynchronousFlag( ar.CompletedSynchronously );

      try
      {
        int unusedGroupVersion;
        bool unusedShowUserMessages;

        List<TrackerId> trackerIds =
          callData.GroupFacade.EndGetGroupTrackerIds
          (
            ar,
            out unusedGroupVersion,
            out unusedShowUserMessages,
            out callData.StartTs
          );

        callData.TrackerIds = new List<TrackerId>( );

        // We need only those TrackerIds whose names present in TrackRequests array. So intersect both lists.
        // Avoid "foreach" and LINQ in frequent operation because both use too much "new" operatons
        for ( int iTrackerId = 0; iTrackerId < trackerIds.Count; iTrackerId++ )
        {
          TrackerId trackerId = trackerIds[iTrackerId];

          for ( int iReq = 0; iReq < callData.TrackRequests.Length; iReq++ )
          {
            TrackRequestItem req = callData.TrackRequests[iReq];

            if ( string.Equals( req.TrackerName, trackerId.Name, StringComparison.InvariantCultureIgnoreCase ) )
            {
              callData.TrackerIds.Add( trackerId );
              break;
            }
          }
        }

        if ( callData.TrackerIds.Count == 0 )
        {
          callData.SetAsCompleted( callData );
        }
        else
        {
          TrackerIdsReady( callData );
        }
      }
      catch ( Exception exc )
      {
        Log.Error( "GetGroupTrackerIdsResponse", exc );
        callData.SetAsCompleted( exc );
      }
    }

    public List<TrackResponseItem> EndGetTracks( IAsyncResult asyncResult )
    {
      Log.DebugFormat( "Entering EndGetTracks..." );

      long callId = -1;

      try
      {
        // Thread.Sleep( 2000 );
        AsyncResult<CallData2> finalAsyncResult = ( AsyncResult<CallData2> ) asyncResult;

        CallData2 callData = finalAsyncResult.EndInvoke( );

        callId = callData.CallId;

        if ( callData.TrackerIds == null || callData.TrackerIds.Count == 0 )
        {
          Log.Warn( "Empty result by GetFullTrack" );
          return new List<TrackResponseItem>( );
        }

        List<TrackResponseItem> result = new List<TrackResponseItem>( callData.TrackerStateHolders.Length );
        for ( int iResult = 0; iResult < callData.TrackerStateHolders.Length; iResult++ )
        {
          string trackerName = callData.TrackerIds[iResult].Name;
          TrackResponseItem trackResponseItem;
          trackResponseItem.TrackerName = trackerName;
          trackResponseItem.Track = null;

          ForeignId trackerForeignId = callData.TrackerIds[iResult].ForeignId;
          TrackerStateHolder trackerStateHolder = callData.TrackerStateHolders[iResult];

          bool isReqFound = false;
          TrackRequestItem req = default( TrackRequestItem );
          for ( int iReq = 0; iReq < callData.TrackRequests.Length; iReq++ )
          {
            if ( string.Equals( callData.TrackRequests[iReq].TrackerName, trackerName, StringComparison.InvariantCultureIgnoreCase ) )
            {
              isReqFound = true;
              req = callData.TrackRequests[iReq];
              break;
            }
          }

          if ( !isReqFound )
          { // Normally should not happen. All in TrackerIds came from TrackRequests.
            throw new InvalidOperationException( string.Format( "Can't find track request data for {0}", trackerName ) );
          }

          // No need to lock on snapshotAccessSync once it's the only snapshot reading here.
          TrackerState snapshot = trackerStateHolder.Snapshot;
          Thread.MemoryBarrier( ); // to make sure that Snapshot (non-volatile field) is read only once. 

          if ( snapshot != null && snapshot.Position != null )
          {
            for ( int iPoint = 0; iPoint < snapshot.Position.FullTrack.Length; iPoint++ )
            {
              TrackPointData trackPointData = snapshot.Position.FullTrack[iPoint];
              if ( !req.LaterThan.HasValue ||
                   trackPointData.ForeignTime > req.LaterThan.Value )
              {
                TrackPoint trackPoint;
                trackPoint.Lat = trackPointData.Latitude;
                trackPoint.Lon = trackPointData.Longitude;
                trackPoint.Ts = trackPointData.ForeignTime;
                trackPoint.Age = CalcAge( trackPointData.ForeignTime );

                if ( iPoint == 0 )
                {
                  trackPoint.Type = snapshot.Position.Type;
                }
                else
                {
                  trackPoint.Type = null;
                }

                if ( trackResponseItem.Track == null )
                  trackResponseItem.Track = new List<TrackPoint>( snapshot.Position.FullTrack.Length );

                trackResponseItem.Track.Add( trackPoint );
              }
            }

            if ( trackResponseItem.Track != null &&
                 trackResponseItem.Track.Count > 0 ) // Ensure that only not-empty tracks go to the result
            {
              // note that trackResponseItem is a struct, so changes made to trackResponseItem variable 
              // after this point wouldn't go anywhere:
              result.Add( trackResponseItem );
            }
          }

          // Interlocked used to make sure the operation is atomic:
          Interlocked.Exchange( ref trackerStateHolder.AccessTimestamp, DateTime.UtcNow.ToFileTime( ) );
        }

        Log.DebugFormat( "Finishing EndGetTracks, call id {0}, got {1} tracks", callId, result.Count );

        return result;
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Error in EndGetTracks, call id {0}: {1}", callId, exc.ToString( ) );
        throw;
      }
      finally
      {
        Interlocked.Decrement( ref this.simultaneousCallCount );
      }
    }

    internal void ClearCache( )
    {
      lock ( this.trackers )
      {
        this.trackers.Clear( );
      }
    }
  }
}