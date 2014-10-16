using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using FlyTrace.LocationLib;
using FlyTrace.LocationLib.ForeignAccess;
using log4net;

namespace FlyTrace.Service
{
  internal class Scheduler
  {
    public readonly IDictionary<ForeignId, TrackerStateHolder> Trackers =
      new Dictionary<ForeignId, TrackerStateHolder>( );

    public readonly ReaderWriterLockSlimEx HolderRwLock = new ReaderWriterLockSlimEx( 1000 );

    /// <summary>
    /// 1. Waits until when it's time to request the returned trackers.
    /// 2. Returned result can be empty.
    /// 3. Modifies trackers that was passed as ctor parameter.
    /// 4. Can enter either or both read/write modes for RW-lock passed as ctor parameter.
    /// 5. Stops waiting and returns with empty result if WaitHandle passed as ctor parameter is signaled.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<TrackerStateHolder> WaitForMomentOfNextRequestAndRemoveOldTrackers( WaitHandle waitHandle )
    {
      bool isTimedOutOrBoringDetected;

      // key is foreign type (Spot, DeLorme etc), value is the tracker that needs an update most urgently:
      IDictionary<string, ForeignStat> trackerStat = GetForeignStats( out isTimedOutOrBoringDetected );

      if ( isTimedOutOrBoringDetected )
        CancelTimeoutsAndRemoveBoringTrackers( );

      // Now find out which trackers from trackerStat have min time to wait. Could be more than one if
      // they all have same minimum time (there is also some tolerance for a time being "the same")
      int minWaitingTime;
      var trackersWithMinWatingTime = GetTrackersWithMinWatingTime( trackerStat, out minWaitingTime );

      if ( waitHandle.WaitOne( minWaitingTime ) )
      { // event signaled which means something has changed - so the logic above needs to rerun
        return Enumerable.Empty<TrackerStateHolder>( );
      }

      foreach ( TrackerStateHolder holder in trackersWithMinWatingTime )
        holder.ScheduledTime = DateTime.UtcNow;

      return trackersWithMinWatingTime;
    }

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

    private void CancelTimeoutsAndRemoveBoringTrackers( )
    {
      HolderRwLock.AttemptEnterWriteLock( );
      try
      {
        foreach ( TrackerStateHolder holder in Trackers.Values )
        {
          ForeignStat foreignStat;
          if ( holder.CurrentRequest != null &&
              IsTimedOut( holder.CurrentRequest ) )
          {
            holder.CurrentRequest.SafelyAbortRequest( );
          }
          {
            foreignStat.InCallCount++;

            if ( IsTimedOut( holder.CurrentRequest ) ||
                 IsBoring( holder ) )
              isTimedOutOrBoringDetected = true;

            continue;
          }

          if ( foreignStat.MostStaleTracker == null )
            foreignStat.MostStaleTracker = holder;
          else
            foreignStat.MostStaleTracker = GetMoreStaleTracker( holder, foreignStat.MostStaleTracker );

          trackerStat[holder.ForeignId.Type] = foreignStat;
        }

      }
      finally
      {
        HolderRwLock.ExitWriteLock( );
      }
    }

    private bool IsTimedOut( LocationRequest locationRequest )
    {
      int msTimeout =
        Math.Max(
          AsyncResultNoResult.DefaultEndWaitTimeout,
          60 * 1000  // def timeout or 60 seconds if it's not set
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