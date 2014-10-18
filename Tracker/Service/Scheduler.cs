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
using System.Linq;
using System.Threading;

using FlyTrace.LocationLib;
using FlyTrace.LocationLib.ForeignAccess;
using log4net;

namespace FlyTrace.Service
{
  /// <summary>
  /// Used in composition with <see cref="ForeignRequestsManager"/> which delegates and use the public 
  /// stuff of this class. All could be in one class, but scheduling and actual calls are fairly different 
  /// pieces so divided that into those two classes. See Remarks section for the scheduler logic description.
  /// </summary>
  /// <remarks>
  /// There is a number of parameters controlling how calls to the foreign servers are scheduled.
  /// 
  /// One of those parameters is a maximum number of simultaneous polling calls to the foreign servers. 
  /// This number can be 1 or more. When it's 1, only 1 call at a time is possible (which would be VERY 
  /// slow for a reasanoble amount of trackers to watch, but such config is still possible)
  /// 
  /// Below "calls pack" is a set of simultaneously running (overlapping) calls. 
  /// Here is an example of how size of the call pack is changing with time:
  /// 
  /// Number of     0|callllll
  /// calls in the  1|...call
  /// current call  2|   ..calllllll
  /// pack at the   2|     ..callll
  /// moment of the 2|       ..calll
  /// call start    3|         ..callllllll
  ///               1|           ....call
  ///                 ---------------------
  ///                 012345678901234567890
  ///                       time, sec
  /// 
  /// Assuming there is a queue of required but not started yet calls, here are the parameters to schedule 
  /// those calls:
  /// 
  /// - MaxCallsInPack: int value >=1, defines maximum number of calls in current pack. When a call in 
  ///    the pack is finished, another call can be started at the time defined by parameters described 
  ///    above. In the example above this parameter could be 4 (see that 4 calls at the 11th and 12th 
  ///    seconds).
  ///  
  /// - MinTimeFromPrevStart: int value in milliseconds, >=0, defines how often calls can be started. A next
  ///   call from the queue can be started only after this time has passed after the START of the previous
  ///   one. In the example above this parameter could be 2000.  Note that the queue of required calls could 
  ///   be empty when the number of calls in the pack goes below limit, that's why in the example above some 
  ///   calls are started with interval of 3 or 4 seconds after prev.start. And of course a call pack could 
  ///   even be empty if there is no trackers to update.
  /// 
  /// - MinCallsGap: int value in milliseconds, >=0, in use only when there is just one call per time is 
  ///    possible, i.e. when MaxCallsInPack (below) is 1. If value of MinCallsGap > 0, next call from the 
  ///    queue can be started only after this time has passed after the END of the previous one. In the 
  ///    example above this parameter is ignored because MaxCallsInPack>1.
  ///  
  /// Below is an example where MinCallsGap is in use, with MaxCallsInPack=1, MinTimeFromPrevStart=7000 
  /// and MinCallsGap=2000:
  ///      calllll..calll...callllllll..calll...call....call....call....
  ///      01234567.0123456701234567....012345670123456701234567
  /// Probably it doesn't make much sense to use both MinTimeFromPrevStart>0 and MinCallsGap>0, but to keep 
  /// the algorithm simple they both are allowed to be non-zero.
  /// 
  /// Another parameter that controls which trackers need polling calls at all, i.e. which calls from the
  /// queue are needed to be scheduled in according to the rules above:
  /// - RefreshInterval: time in seconds, >0, specifies when a tracker requires a call
  ///   to the foreign server to refresh the tracker's data. It's time span that needs to pass after previous
  ///   call for the same tracker (always counted from the prev.call start)
  /// </remarks>
  public class Scheduler
  {
    public readonly IDictionary<ForeignId, TrackerStateHolder> Trackers =
      new Dictionary<ForeignId, TrackerStateHolder>( );

    public readonly ReaderWriterLockSlimEx HolderRwLock = new ReaderWriterLockSlimEx( 1000 );

    /// <summary>
    /// Finds out the next time when trackers need to be requested and waits till that time. Also does some 
    /// cleanup on the content of <see cref="Trackers"/>. For details, see Remarks section.
    /// </summary>
    /// <remarks>
    /// - Waits until it's time to request the returned trackers.
    /// - Returned result can be empty.
    /// - Modifies Trackers.
    /// - Can enter either or both read and write modes for <see cref="HolderRwLock"/>
    /// - Stops waiting and returns with empty result if WaitHandle passed as parameter is signaled.
    /// - Cleans up trackers that are not on demand and cancels timed out requests.
    /// </remarks>
    public IEnumerable<TrackerStateHolder> ScheduleCleanupWait( WaitHandle waitHandle )
    {
      bool isTimedOutOrBoringDetected;

      // key is foreign type (Spot, DeLorme etc), value is the tracker that needs an update most urgently:
      IDictionary<string, ForeignStat> trackerStat =
        GetForeignStats( out isTimedOutOrBoringDetected );

      if ( isTimedOutOrBoringDetected )
        CancelTimedOutsAndRemoveBoringTrackers( );

      if ( prevTimeAbortsChecked.AddMinutes( 1 ) < DateTime.UtcNow )
      {
        prevTimeAbortsChecked = DateTime.UtcNow;
        ThreadPool.QueueUserWorkItem( CheckForTimedOutAborts );
      }

      // Now find out which trackers from trackerStat have min time to wait. Could be more than one if
      // they all have same minimum time (there is also some tolerance for a time being "the same")
      int minWaitingTime;
      var trackersWithMinWatingTime = GetTrackersWithMinWatingTime( trackerStat, out minWaitingTime );

      if ( waitHandle.WaitOne( minWaitingTime ) )
      {
        // event signaled which means something has changed - so the logic above needs to rerun
        return Enumerable.Empty<TrackerStateHolder>( );
      }

      foreach ( TrackerStateHolder holder in trackersWithMinWatingTime )
        holder.ScheduledTime = DateTime.UtcNow;

      return trackersWithMinWatingTime;
    }

    private DateTime prevTimeAbortsChecked; // on construction set to default DateTime which is quite long time ago.

    private static readonly ILog Log = LogManager.GetLogger( "TDM.Sched" );

    /// <summary>Max time for the thread to sleep before rechecking the situation</summary>
    private const int MaxSleepTimeMs = 3000;

    /// <summary>In minutes. A tracker that has not been accessed for more than the number of minutes 
    /// specified by this constant is considered as "old" (see other properties and methods) and is subject 
    /// to remove from <see cref="Trackers"/></summary>
    private const int TrackerLifetimeWithoutAccess = 20;

    private struct ForeignStat
    {
      public int InCallCount;
      public TrackerStateHolder MostStaleTracker;
    }

    private IDictionary<string, ForeignStat> GetForeignStats( out bool isTimedOutOrBoringDetected )
    {
      // key is foreign type (Spot, DeLorme etc), value is the tracker that needs an update most urgently:
      IDictionary<string, ForeignStat> trackerStat =
        new Dictionary<string, ForeignStat>( StringComparer.InvariantCultureIgnoreCase );

      isTimedOutOrBoringDetected = false;

      HolderRwLock.AttemptEnterReadLock( );
      try
      {
        foreach ( TrackerStateHolder holder in Trackers.Values )
        {
          ForeignStat foreignStat;
          trackerStat.TryGetValue( holder.ForeignId.Type, out foreignStat );
          if ( holder.CurrentRequest != null )
          {
            foreignStat.InCallCount++;

            if ( IsTimedOut( holder.CurrentRequest ) )
              isTimedOutOrBoringDetected = true;

            continue;
          }

          if ( foreignStat.MostStaleTracker == null )
            foreignStat.MostStaleTracker = holder;
          else
            foreignStat.MostStaleTracker = GetMoreStaleTracker( holder, foreignStat.MostStaleTracker );

          trackerStat[holder.ForeignId.Type] = foreignStat;

          if ( IsBoring( holder ) )
            isTimedOutOrBoringDetected = true;
        }
      }
      finally
      {
        HolderRwLock.ExitReadLock( );
      }

      return trackerStat;
    }

    private void CancelTimedOutsAndRemoveBoringTrackers( )
    {
      try
      {
        List<ForeignId> boringIds = new List<ForeignId>( 20 );
        List<LocationRequest> timedOut = new List<LocationRequest>( 20 );

        HolderRwLock.AttemptEnterWriteLock( );
        try
        {
          foreach ( TrackerStateHolder holder in Trackers.Values )
          {
            if ( holder.CurrentRequest != null &&
                 IsTimedOut( holder.CurrentRequest ) )
            {
              LocationRequest locReq = holder.CurrentRequest;
              holder.CurrentRequest = null;
              timedOut.Add( locReq );
            }

            if ( IsBoring( holder ) ) // don't remove elements from Trackers right here 'cause we enumerating it now
              boringIds.Add( holder.ForeignId );
          }

          foreach ( ForeignId boringId in boringIds )
            Trackers.Remove( boringId );
        }
        finally
        {
          HolderRwLock.ExitWriteLock( );
        }


        // Can do it out of lock because Trackers don't reference those LocationRequests anymore
        foreach ( LocationRequest locReq in timedOut )
        {
          LocationRequest.TimedOutRequestsLog.ErrorFormat(
            "Location request hasn't finished in time for lrid {0}, tracker id {1}",
            locReq.Lrid,
            locReq.Id );

          ThreadPool.QueueUserWorkItem( AbortRequest, locReq );
        }
      }
      catch ( Exception exc )
      {
        Log.Error( exc );
      }
    }

    private readonly Dictionary<long, AbortStat> queuedAborts = new Dictionary<long, AbortStat>( );

    private void AbortRequest( object state )
    {
      Global.ConfigureThreadCulture( );

      long? lrid = null;
      try
      {
        LocationRequest locReq = ( LocationRequest ) state;
        lrid = locReq.Lrid;
        LocationRequest.TimedOutRequestsLog.InfoFormat( "Starting AbortRequest for lrid {0}...", lrid );

        // SafelyAbortRequest can hang up, that happened before. Looks like nothing can be done with that, 
        // but at least it should be detected. For that, use QueuedAborts and check it periodically.
        AbortStat abortStat;
        lock ( this.queuedAborts )
        {
          abortStat = new AbortStat( );
          this.queuedAborts.Add( lrid.Value, abortStat );
        }

        locReq.SafelyAbortRequest( abortStat );
        LocationRequest.TimedOutRequestsLog.InfoFormat( "AbortRequest finished for lrid {0}", lrid );
      }
      catch ( Exception exc )
      {
        LocationRequest.TimedOutRequestsLog.ErrorFormat( "AbortRequest error for lrid {0}: {1}", lrid, exc );
      }
      finally
      {
        if ( lrid.HasValue )
        {
          lock ( this.queuedAborts )
          {
            this.queuedAborts.Remove( lrid.Value );
          }
        }
      }
    }

    private static readonly ILog TimedOutAbortsLog = LogManager.GetLogger( "TimedOutAborts" );

    /// <summary>
    /// See the comments in <see cref="AbortRequest"/>
    /// </summary>
    /// <param name="unused"></param>
    private void CheckForTimedOutAborts( object unused )
    {
      try
      {
        DateTime threshold = DateTime.UtcNow.AddMinutes( -5 );

        KeyValuePair<long, AbortStat>[] timedOutAborts;
        lock ( this.queuedAborts )
        {
          timedOutAborts =
            this.queuedAborts
            .Where
            (
              kvp =>
                kvp.Value.Start < threshold
            )
            .ToArray( );
        }

        if ( timedOutAborts.Length > 0 )
        {
          foreach ( KeyValuePair<long, AbortStat> kvp in timedOutAborts )
          {
            TimeSpan timespan = DateTime.UtcNow - kvp.Value.Start;
            TimedOutAbortsLog.ErrorFormat(
              "Abort for lrid {0} timed out at stage {1} for {2}", kvp.Key, kvp.Value.Stage, timespan );
          }

          lock ( this.queuedAborts )
          {
            foreach ( long lrid in timedOutAborts.Select( kvp => kvp.Key ) )
            {
              if ( this.queuedAborts.ContainsKey( lrid ) )
                this.queuedAborts.Remove( lrid );
            }
          }
        }
      }
      catch ( Exception exc )
      {
        TimedOutAbortsLog.Error( "Error in CheckForTimedOutAborts", exc );
      }
    }

    private bool IsTimedOut( LocationRequest locationRequest )
    {
      int msTimeout =
        Math.Max(
          AsyncResultNoResult.DefaultEndWaitTimeout,
          60 * 1000 // def timeout or 60 seconds if it's not set
          );

      return locationRequest.StartTs < DateTime.UtcNow.AddSeconds( -msTimeout );
    }

    private bool IsBoring( TrackerStateHolder holder )
    {
      long boringThreshold =
        DateTime.UtcNow.AddMinutes( -TrackerLifetimeWithoutAccess ).ToFileTime( );

      long accessTimestamp =
        Interlocked.Read( ref holder.ThreadDesynchronizedAccessTimestamp );

      return accessTimestamp < boringThreshold;
    }

    private static List<TrackerStateHolder> GetTrackersWithMinWatingTime
    (
      IDictionary<string, ForeignStat> trackerStat,
      out int minWaitingTime
    )
    {
      List<TrackerStateHolder> trackersWithMinWatingTime = new List<TrackerStateHolder>( );

      minWaitingTime = MaxSleepTimeMs;

      const int waitingTimeDiffTolerance = 50; // this difference doesn't matter.

      foreach ( ForeignStat foreignStat in trackerStat.Values )
      {
        int waitingTime = GetWaitingTime( foreignStat );

        if ( waitingTime < ( minWaitingTime - waitingTimeDiffTolerance ) )
        {
          minWaitingTime = waitingTime;
          trackersWithMinWatingTime.Clear( );
          trackersWithMinWatingTime.Add( foreignStat.MostStaleTracker );
        }
        else if ( Math.Abs( waitingTime - minWaitingTime ) < waitingTimeDiffTolerance )
        {
          trackersWithMinWatingTime.Add( foreignStat.MostStaleTracker );
        }
      }
      return trackersWithMinWatingTime;
    }

    private static int GetWaitingTime( ForeignStat foreignStat )
    {
    }

    private static TrackerStateHolder GetMoreStaleTracker( TrackerStateHolder h1, TrackerStateHolder h2 )
    {
      // for the main application it doesn't matter which tracker to take if times are equal,
      // so do not use out parameter:
      bool unused;
      return GetMoreStaleTracker( h1, h2, out unused );
    }

    /// <summary>
    /// Methid is made public for the unit tests only. See Remarks section for details.
    /// </summary>
    /// <remarks>
    /// - Holders with ScheduledTime set are always less stale than others, because it's trackers
    ///   which were scheduled but for some reasons (error?) didn't started requests. So avoid 
    ///   scheduling those trackers on and on again (failing to request due to the same error) 
    ///   while there are others waiting.
    /// - Holder without RefreshTime is always more stale than a holder with a RefreshTime. I.e.
    ///   trackers that just requested by clients should be refreshed first.
    /// 
    /// * From two holders without ScheduledTime and without RefreshTime, more stale is one with 
    ///   earlier <see cref="TrackerStateHolder.AddedTime"/>.
    /// * From two holders without ScheduledTime and with RefreshTime, more stale is one with 
    ///   earlier <see cref="TrackerStateHolder.RefreshTime"/>.
    /// * From two holders with ScheduledTime, more stale is one with earlier ScheduledTime.
    /// 
    /// Parameter <paramref name="areEqual"/> is needed for sorting only. When both trackers are 
    /// equal then it's not allowed to return any as "most stale" - because if any of the equal 
    /// is returned as "lesser", then it becomes important which of the pair goes as the 1st parameter 
    /// and which goes as the 2nd. In other words, transitivity and conversing rules will be broken, 
    /// which are absolutely critical for sorting which needed in the unit tests. Notice that it's 
    /// not important for the main app where just any of the trackers with equal times can be picked 
    /// up. But for the unit tests equality needs to be recognised as well.
    /// </remarks>
    public static TrackerStateHolder GetMoreStaleTracker(
      TrackerStateHolder x,
      TrackerStateHolder y,
      out bool areEqual
      )
    {
      // The logic in this method is quite fragile, so always run the unit test after making just 
      // any change here.

      DateTime t1, t2;

      if ( x.ScheduledTime != null && y.ScheduledTime != null )
      {
        t1 = x.ScheduledTime.Value;
        t2 = y.ScheduledTime.Value;
      }
      else if ( x.ScheduledTime != null ) // means "and y.ScheduledTime == null"
      {
        areEqual = false;
        return y;
      }
      else if ( y.ScheduledTime != null ) // means "and x.ScheduledTime == null"
      {
        areEqual = false;
        return x;
      }
      // at this point x.ScheduledTime == null && y.ScheduledTime == null )
      else if ( x.RefreshTime != null && y.RefreshTime != null )
      {
        t1 = x.RefreshTime.Value;
        t2 = y.RefreshTime.Value;
      }
      else if ( x.RefreshTime != null ) // means "and y.RefreshTime == null"
      {
        areEqual = false;
        return y;
      }
      else if ( y.RefreshTime != null ) // means "and x.RefreshTime == null"
      {
        areEqual = false;
        return x;
      }
      else
      {
        // at this point x.ScheduledTime == null && y.ScheduledTime == null 
        //            && x.RefreshTime == null && y.RefreshTime == null
        t1 = x.AddedTime;
        t2 = y.AddedTime;
      }

      areEqual = t1 == t2;

      return t1 < t2 ? x : y;
    }

    internal void ClearTrackers( )
    {
      HolderRwLock.AttemptEnterWriteLock( );
      try
      {
        Trackers.Clear( );
      }
      finally
      {
        HolderRwLock.ExitWriteLock( );
      }
    }

    internal bool AddMissingTrackers( List<TrackerName> trackerIds )
    {
      // TODO: test with Thread.Sleep(x000) in the beginning

      bool newTrackersAdded = false;

      try
      {
        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Adding {0} new tracker(s)...", trackerIds.Count );

        HolderRwLock.AttemptEnterWriteLock( );
        try
        {
          foreach ( TrackerName trackerId in trackerIds )
          {
            if ( Trackers.ContainsKey( trackerId.ForeignId ) )
            {
              if ( Log.IsDebugEnabled )
                Log.DebugFormat( "Already added: '{0}' / {1}", trackerId.Name, trackerId.ForeignId );
            }
            else
            {
              if ( Log.IsDebugEnabled )
                Log.DebugFormat( "Adding '{0}' / {1}...", trackerId.Name, trackerId.ForeignId );

              // no data yet, so leave its Snapshot field null for the moment:
              TrackerStateHolder trackerStateHolder = new TrackerStateHolder( trackerId.ForeignId );

              Trackers.Add( trackerId.ForeignId, trackerStateHolder );
              newTrackersAdded = true;
            }
          }
        }
        finally
        {
          HolderRwLock.ExitWriteLock( );
        }

        if ( Log.IsDebugEnabled )
          Log.Debug( "New tracker(s) added" );
      }
      catch ( Exception exc )
      {
        Log.Error( "Error when adding trackers", exc );
      }

      return newTrackersAdded;
    }
  }
}