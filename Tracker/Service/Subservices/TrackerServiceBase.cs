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
            groupConfig.TrackerIds == null ? "null" : groupConfig.TrackerIds.Count.ToString( ),
            CallId, Group );

        if ( groupConfig.TrackerIds == null )
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
  }
}