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

namespace FlyTrace.Service.Subservices
{
  public abstract class TrackerServiceBase<T> : CommonBase
  {
    protected readonly int Group;
    protected readonly DateTime CallStartTime = DateTime.UtcNow;

    protected TrackerServiceBase( int group )
    {
      Group = group;
    }

    private readonly GroupFacade groupFacade = new GroupFacade( );

    protected static TrackerDataManager2 DataManager
    {
      get { return TrackerDataManager2.Singleton; }
    }

    protected AsyncResult<T> BeginGetGroupTrackerIds( AsyncCallback outerCallback, object outerState )
    {
      AsyncChainedState<T> asyncChainedState = new AsyncChainedState<T>( outerCallback, outerState );

      this.groupFacade.BeginGetGroupTrackerIds( Group, OnEndGettingGroupTrackerIds, asyncChainedState );

      return asyncChainedState.FinalAsyncResult;
    }

    protected abstract T GetResult( GroupConfig groupConfig );

    private void OnEndGettingGroupTrackerIds( IAsyncResult ar )
    {
      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Finishing getting group trackers id for coords, sync flag {0}...", ar.CompletedSynchronously );

      AsyncChainedState<T> asyncChainedState = ( AsyncChainedState<T> ) ar.AsyncState;

      try
      {
        if ( Log.IsDebugEnabled )
          Log.DebugFormat(
            "Finishing getting group trackers ids with call id {0}, group {1}...", CallId, Group );

        asyncChainedState.CheckSynchronousFlag( ar.CompletedSynchronously );

        GroupConfig groupConfig = this.groupFacade.EndGetGroupTrackerIds( ar );

        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Got {0} tracker(s) for call id {1}, group {2}",
            groupConfig.TrackerNames == null ? "null" : groupConfig.TrackerNames.Count.ToString( ),
            CallId, Group );

        if ( groupConfig.TrackerNames == null )
          throw new InvalidOperationException( "Unexpected null value for GroupConfig.TrackerIds" );

        T result = GetResult( groupConfig );

        asyncChainedState.SetAsCompleted( result );
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "OnEndGettingGroupTrackerIds: call id {0}: {1}", CallId, exc );
        asyncChainedState.SetAsCompleted( exc );
      }
    }

    /// <summary>
    /// Snapshots in result correspond to elements in <paramref name="trackerNames"/>, but can be null
    /// if snapshot was not tracked at the moment of call.
    /// </summary>
    /// <param name="trackerNames"></param>
    /// <param name="groupConfig"></param>
    /// <returns></returns>
    protected RevisedTrackerState[] GetSnapshots( List<TrackerName> trackerNames, GroupConfig groupConfig )
    {
      if ( Log.IsDebugEnabled )
      {
        TimeSpan timespan = DateTime.UtcNow - CallStartTime;
        Log.DebugFormat(
          "Got {0} trackers ids for call id {1}, group {2} with version {3} in {4} ms, getting their data now...",
          trackerNames.Count, CallId, Group, groupConfig.VersionInDb, ( int ) timespan.TotalMilliseconds );
      }

      TrackerStateHolder[] holders = new TrackerStateHolder[trackerNames.Count];
      RevisedTrackerState[] snapshots = new RevisedTrackerState[trackerNames.Count];

      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Acquiring read lock for call id {0}, group {1}...", CallId, Group );

      DataManager.RwLock.AttemptEnterReadLock( );
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
        for ( int i = 0; i < trackerNames.Count; i++ )
        {
          TrackerName trackerId = trackerNames[i];
          TrackerStateHolder trackerStateHolder;

          // so i'th elements might be null if the tracker in not in dataManager yet:
          if ( DataManager.Trackers.TryGetValue( trackerId.ForeignId, out trackerStateHolder ) )
          {
            notNullCount++;
            holders[i] = trackerStateHolder;
            snapshots[i] = trackerStateHolder.Snapshot;
          }
        }

        if ( Log.IsDebugEnabled )
          Log.DebugFormat(
            "Got {0} tracker(s) ({1} not null) under read lock for call id {2}",
            trackerNames.Count, notNullCount, CallId );
      }
      finally
      {
        DataManager.RwLock.ExitReadLock( );
        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Left lock in GetTrackerIdResponse for call id {0}", CallId );
      }

      foreach ( TrackerStateHolder holder in holders )
      {
        // that's a bit of a semantics break for rwLock used for MT operations with holders because it's out 
        // of lock, but AccessTimestamp is used for diag purposes only by a admins. For purists reason, it should
        // be under write lock, but it would slow down the things so cutting the corner here:
        Interlocked.Exchange( ref holder.ThreadDesynchronizedAccessTimestamp, DateTime.UtcNow.ToFileTime( ) );
      }

      if ( holders.Any( h => h == null ) )
      {
        // add it in a separate thread to return the call asap (adding might be blocked by another thread)
        ThreadPool.QueueUserWorkItem( unused => DataManager.AddMissingTrackers( trackerNames ) );
      }

      return snapshots;
    }

    protected static int CalcAgeInSeconds( DateTime time )
    {
      if ( time == default( DateTime ) )
        return 0;

      TimeSpan locationAge = DateTime.UtcNow - time;
      return Math.Max( 0, ( int ) locationAge.TotalSeconds ); // to fix potential error in this server time settings
    }

  }
}