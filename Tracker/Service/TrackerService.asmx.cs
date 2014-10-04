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
using System.Web.Services;
using System.Net;
using System.IO;
using System.Xml;
using System.Threading;
using System.Diagnostics;
using log4net;

namespace FlyTrace.Service
{
  /// <summary>
  /// Summary description for TrackerService
  /// </summary>
  [WebService( Namespace = "http://flytrace.com/" )]
  [WebServiceBinding( ConformsTo = WsiProfiles.BasicProfile1_1 )]
  [System.ComponentModel.ToolboxItem( false )]
  [System.Web.Script.Services.ScriptService]
  public class TrackerService : System.Web.Services.WebService, ITrackerService
  {
    private bool isNew = false;

    [WebMethod]
    public GroupData GetCoordinates( int group, string srcSeed, DateTime scrTime /* this parameter to preven client-side response caching */ )
    {
      Services.ICoordinatesService trackerService;
      if ( isNew )
        trackerService = new Services.CoordinatesService( group, srcSeed );
      else
        trackerService = TrackerDataManager.Singleton;

      // TODO: remove group, srcSeed params (already in constructor)
      IAsyncResult ar = trackerService.BeginGetCoordinates( group, srcSeed, null, null );
      ar.AsyncWaitHandle.WaitOne( );

      return trackerService.EndGetCoordinates( ar );
    }

    [WebMethod]
    public List<TrackResponseItem> GetTracks( int group, TrackRequest trackRequest, DateTime scriptCurrTimet )
    {
      ITrackerService trackerService;
      if ( isNew )
        trackerService = this;
      else
        trackerService = TrackerDataManager.Singleton;

      long callId;
      IAsyncResult ar = TrackerDataManager.Singleton.BeginGetTracks( group, trackRequest.Items, null, null, out callId );

      bool handleSignaled;
      if ( AsyncResultNoResult.DefaultEndWaitTimeout > 0 )
        handleSignaled = ar.AsyncWaitHandle.WaitOne( AsyncResultNoResult.DefaultEndWaitTimeout );
      else
        handleSignaled = ar.AsyncWaitHandle.WaitOne( );

      if ( !handleSignaled )
      {
        Log.FatalFormat( "GetTracks call has timed out for call id {0}.", callId );
        return null;
      }

      return new List<TrackResponseItem>( TrackerDataManager.Singleton.EndGetTracks( ar ) );
    }

    [WebMethod]
    public void TestCheck( string msg )
    { // not just a debug method, this one is used from main.js script
      IncrTestLog.Error( msg );
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
      Global.ConfigureThreadCulture( );

      int callCount = Interlocked.Increment( ref this.simultaneousCallCount );

      try
      {
        CoordinatesRequestData callData = new CoordinatesRequestData( group, trackRequests, callback, asyncState );

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
      Global.ConfigureThreadCulture( );

      var callData = ( CoordinatesRequestData ) ar.AsyncState;

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
        AsyncResult<CoordinatesRequestData> finalAsyncResult = ( AsyncResult<CoordinatesRequestData> ) asyncResult;

        CoordinatesRequestData callData = finalAsyncResult.EndInvoke( );

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
  }
}