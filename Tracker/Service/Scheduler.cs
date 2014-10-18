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
      throw new NotImplementedException( );
    }

    private static TrackerStateHolder GetMoreStaleTracker( TrackerStateHolder h1, TrackerStateHolder h2 )
    {
      // for the main application it doesn't matter which tracker to take if times are equal,
      // so do not use out parameter:
      bool unused;
      return GetMoreStaleTracker( h1, h2, out unused );
    }

    /// <summary>
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
    /// </summary>
    /// <remarks>
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