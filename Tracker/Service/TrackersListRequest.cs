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
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;

using log4net;

using FlyTrace.LocationLib;
using FlyTrace.LocationLib.ForeignAccess;

namespace FlyTrace.Service
{

  internal class TrackersListRequest
  {
    private readonly List<LocationRequest> requests = new List<LocationRequest>( );

    private AsyncChainedState<Dictionary<ForeignId, TrackerState>> asyncChainedState;

    private int multiCallCheck;

    public Dictionary<ForeignId, TrackerState> GetTrackersLocations( IEnumerable<ForeignId> trackerForeignIds )
    {
      IAsyncResult ar = BeginGetTrackersLocations( trackerForeignIds, null, null );
      return EndGetTrackersLocations( ar );
    }

    private static readonly ILog Log = LogManager.GetLogger( "TDM.ListReq" );

    public IAsyncResult BeginGetTrackersLocations( IEnumerable<ForeignId> trackerForeignIds, AsyncCallback callback, object state )
    {
      int syncMultiCallCheck = Interlocked.Increment( ref multiCallCheck );

      try
      {
        if ( syncMultiCallCheck > 1 )
          throw new InvalidOperationException(
            "The BeginGetTrackersLocations method cannot be called for the second time on the same " +
             "instance until the EndGetTrackersLocations method has been called." );

        this.asyncChainedState = new AsyncChainedState<Dictionary<ForeignId, TrackerState>>( callback, state );

        if( AsyncResultNoResult.DefaultEndWaitTimeout > 0 )
        {
          // Allow underlying requests to timeout and then report it to this async operation. 
          // Otherwise it could be this one timing out only because underlying requests is timing out 
          // a fraction of second later.
          this.asyncChainedState.FinalAsyncResult.EndWaitTimeout = AsyncResultNoResult.DefaultEndWaitTimeout + 5000;
        }

        // Blog search: http://www.google.com.au/search?tbm=blg&hl=en&source=hp&biw=1440&bih=738&q=http%3A%2F%2Fshare.findmespot.com%2Fshared%2Ffaces%2F&btnG=Search&gbv=2#q=http://share.findmespot.com/shared/faces/&hl=en&gbv=2&tbm=blg&source=lnt&tbs=qdr:w&sa=X&ei=3cXYTurKGojSmAX-4LXnCw&ved=0CBEQpwUoBA&bav=on.2,or.r_gc.r_pw.,cf.osb&fp=536e09847cd89619&biw=1440&bih=738

        string appAuxLogFolder = Path.Combine( HttpRuntime.AppDomainAppPath, "logs" );
        foreach ( ForeignId trackerForeignId in trackerForeignIds )
        { // We fill the this.requests list PRIOR to actually starting any web request. List is thread-safe for 
          // reading when none writes to it, so later we don't care about sycnronization when accessing it.
          try
          {
            LocationRequestFactory requestFactory =
              ForeignAccessCentral.LocationRequestFactories[trackerForeignId.Type];

            LocationRequest locRequest;

            if ( trackerForeignId.Id.StartsWith( Test.TestSource.TestIdPrefix ) )
            {
              string testXml = Test.TestSource.Singleton.GetFeed( trackerForeignId.Id );
              locRequest = requestFactory.CreateTestRequest( trackerForeignId.Id, testXml );
            }
            else
            {
              locRequest = requestFactory.CreateRequest( trackerForeignId.Id );
            }

            this.requests.Add( locRequest );
          }
          catch ( Exception exc )
          {
            Log.ErrorFormat( "Can't read location for {0}: {1}", trackerForeignId.Id, exc.ToString( ) );
            AddTrackerError( trackerForeignId, exc );
          }
        }

        // And only when we have the list of the Request instances prepared, begin the actual web requests:
        foreach ( LocationRequest locationRequest in this.requests )
        {
          try
          {
            locationRequest.BeginReadLocation( ReadLocationCallback, locationRequest );
          }
          catch ( Exception exc )
          {
            Log.ErrorFormat( "Can't read location for {0}: {1}", locationRequest.Id, exc.ToString( ) );
            AddTrackerError( locationRequest.ForeignId, exc );
          }
        }
      }
      catch ( Exception exc2 )
      {
        Log.ErrorFormat( "Can't get trackers locations: {0}", exc2.ToString( ) );
        Interlocked.Decrement( ref multiCallCheck );
        this.asyncChainedState.SetAsCompleted( exc2 );
      }

      return this.asyncChainedState.FinalAsyncResult;
    }

    public Dictionary<ForeignId, TrackerState> EndGetTrackersLocations( IAsyncResult ar )
    {
      AsyncResult<Dictionary<ForeignId, TrackerState>> asyncResult =
        ( AsyncResult<Dictionary<ForeignId, TrackerState>> ) ar;

      try
      {
        return asyncResult.EndInvoke( );
      }
      catch ( Exception exc )
      {
        LocationRequest.TimedOutRequestsLog.Error( "Can't end getting trackers locations", exc );

        bool hasAbortedRequests = false;

        Dictionary<ForeignId, TrackerState> substResult = new Dictionary<ForeignId, TrackerState>( );
        lock ( this.result )
        {
          foreach ( LocationRequest locReq in this.requests )
          {
            TrackerState trackerState;
            if ( this.result.TryGetValue( locReq.ForeignId, out trackerState ) )
            {
              substResult.Add( locReq.ForeignId, trackerState );
            }
            else
            {
              LocationRequest.TimedOutRequestsLog.ErrorFormat( "Location request hasn't finished for lrid {0}, tracker id {1}", locReq.Lrid, locReq.Id );
              ThreadPool.QueueUserWorkItem( AsyncAbortRequest, locReq );
              LocationRequest.TimedOutRequestsLog.ErrorFormat( "Location request with lrid {0} queued for abort.", locReq.Lrid );

              hasAbortedRequests = true;

              substResult.Add( locReq.ForeignId, new TrackerState( exc.Message, "None" ) );
            }
          }
        }

        if ( hasAbortedRequests )
        {
          CheckForTimedOutAborts( );
        }

        return substResult;
      }
      finally
      {
        // Check that this is the correct instance of asyncResult, i.e. the one that was returned 
        // earlier in the Begin... method:
        if ( asyncResult == this.asyncChainedState.FinalAsyncResult )
        {
          // And set recusrionCheck to zero if this instance was in processing of the request:
          Interlocked.CompareExchange( ref this.multiCallCheck, 0, 1 );

          // (actually we need also another syncMultiCallCheck, only for this method. But it 
          //  looks too excessive, it's not a public library after all)
        }
      }
    }

    private static readonly Dictionary<long, AbortStat> queuedAborts = new Dictionary<long, AbortStat>( );

    private static readonly ILog TimedOutAbortsLog = LogManager.GetLogger( "TimedOutAborts" );

    private static void CheckForTimedOutAborts( )
    {
      try
      {
        DateTime threshold = DateTime.UtcNow.AddMinutes( -5 );

        KeyValuePair<long, AbortStat>[] timedOutAborts;
        lock ( queuedAborts )
        {
          timedOutAborts =
            queuedAborts
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

          lock ( queuedAborts )
          {
            foreach ( long lrid in timedOutAborts.Select( kvp => kvp.Key ) )
            {
              if ( queuedAborts.ContainsKey( lrid ) )
                queuedAborts.Remove( lrid );
            }
          }
        }
      }
      catch ( Exception exc )
      {
        TimedOutAbortsLog.Error( "Error in CheckForTimedOutAborts", exc );
      }
    }


    private static void AsyncAbortRequest( object state )
    {
      Global.ConfigureThreadCulture( );

      long? lrid = 0;
      try
      {
        LocationRequest locReq = ( LocationRequest ) state;
        lrid = locReq.Lrid;

        AbortStat abortStat;
        lock ( queuedAborts )
        {
          abortStat = new AbortStat( );
          queuedAborts.Add( lrid.Value, abortStat );
        }

        locReq.SafelyAbortRequest( abortStat );
        LocationRequest.TimedOutRequestsLog.InfoFormat( "SafelyAbortRequest finished for lrid {0}", lrid );
      }
      catch ( Exception exc )
      {
        LocationRequest.TimedOutRequestsLog.ErrorFormat( "AsyncAbortRequest error for lrid {0}: {1}", lrid, exc );
      }
      finally
      {
        if ( lrid.HasValue )
        {
          lock ( queuedAborts )
          {
            queuedAborts.Remove( lrid.Value );
          }
        }
      }
    }

    private void ReadLocationCallback( IAsyncResult ar )
    {
      try
      {
        LocationRequest locationRequest = ( LocationRequest ) ar.AsyncState;
        this.asyncChainedState.CheckSynchronousFlag( ar.CompletedSynchronously );
        try
        {
          TrackerState trackerState = locationRequest.EndReadLocation( ar );
          AddTrackerData( locationRequest.ForeignId, trackerState );
        }
        catch ( Exception exc )
        {
          Log.Error( "Can't end reading locations", exc );
          AddTrackerError( locationRequest.ForeignId, exc );
        }
      }
      catch ( Exception exc2 )
      {
        Log.Error( "Can't end reading locations #2", exc2 );
        this.asyncChainedState.SetAsCompleted( exc2 );
      }
    }

    private readonly Dictionary<ForeignId, TrackerState> result = new Dictionary<ForeignId, TrackerState>( );

    private void AddTrackerData( ForeignId trackerForeignId, TrackerState trackerState )
    {
      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "AddTrackerData: acquiring lock for {0}...", trackerForeignId );

      try
      {
        lock ( this.result )
        {
          if ( Log.IsDebugEnabled )
            Log.DebugFormat( "AddTrackerData: inside lock for {0}, result count is {1}.", trackerForeignId, this.result.Count );

          if ( this.result.ContainsKey( trackerForeignId ) )
          {
            return;
          }

          this.result.Add( trackerForeignId, trackerState );

          if ( this.result.Count == this.requests.Count )
          {
            if ( Log.IsDebugEnabled )
              Log.DebugFormat( "AddTrackerData: on {0} list request is finishing for {1} trackers.", trackerForeignId, this.requests.Count );

            this.asyncChainedState.SetAsCompleted( result );
          }
        }
      }
      finally
      {
        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "AddTrackerData: out of lock for {0}.", trackerForeignId );
      }
    }

    private void AddTrackerError( ForeignId trackerForeignId, Exception exc )
    {
      AddTrackerData( trackerForeignId, new TrackerState( exc.Message, "None" ) );
    }
  }
}