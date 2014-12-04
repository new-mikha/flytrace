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
using System.Text;
using System.Threading;
using FlyTrace.LocationLib;
using FlyTrace.LocationLib.Data;

namespace FlyTrace.Service.Subservices
{
  public class TracksService : TrackerServiceBase<List<TrackResponseItem>>
  {
    private readonly TrackRequestItem[] trackRequests;

    public TracksService( int group, TrackRequest trackRequest )
      : base( group )
    {
      this.trackRequests = trackRequest.Items;
    }

    /// <summary>TODO: remove unused* fields</summary>
    public IAsyncResult BeginGetTracks(
      int unsused1,
      TrackRequestItem[] unused2,
      AsyncCallback callback,
      object asyncState,
      out long callId
    )
    {
      Global.ConfigureThreadCulture( );

      try
      {
        callId = CallId; // TODO: remove callId out param after removing ITrackerService interface

        IncrementCallCount( );

        Log.DebugFormat(
          "Getting tracks for group {0}, call id {1}, tracks requested: {2}, call count: {3}",
          Group,
          CallId,
          this.trackRequests == null ? 0 : this.trackRequests.Length,
          DebugCallCount
        );

        if ( this.trackRequests == null || this.trackRequests.Length == 0 )
        {
          Log.DebugFormat( "Call id {0}, group {1}: request is empty, so completing now with empty reply", CallId, Group );

          var asyncResult = new AsyncResult<List<TrackResponseItem>>( callback, asyncState );
          asyncResult.SetAsCompleted( new List<TrackResponseItem>( ), true );

          return asyncResult;
        }
        if ( this.trackRequests.Length > 10 )
          throw new ApplicationException( "Number of requested full tracks can't exceed 10" );

        StringBuilder names = new StringBuilder( 10 * this.trackRequests.Length );
        for ( int iReq = 0; iReq < this.trackRequests.Length; iReq++ )
        {
          TrackRequestItem req = this.trackRequests[iReq];

          if ( string.IsNullOrEmpty( req.TrackerName ) )
            throw new ApplicationException( "Tracker name cannot be empty." );

          if ( iReq > 0 )
            names.Append( ", " );
          names.Append( req.TrackerName );
        }

        Log.InfoFormat(
          "Getting full tracks for trackers for call id {0}, group {1}: {2}", CallId, Group, names );

        return BeginGetGroupTrackerIds( callback, asyncState );
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Error in BeginGetTracks: {0}", exc );
        DecrementCallCount( );
        throw;
      }
    }

    protected override List<TrackResponseItem> GetResult( GroupConfig groupConfig )
    {
      // this not called very often if compared with GetOccrdinates, so should be ok to
      // use Linq
      List<TrackerName> trackerNames =
        groupConfig
          .TrackerNames
          .Where(
            req =>
              this.trackRequests.Any(
                tr => string.Equals( tr.TrackerName, req.Name, StringComparison.InvariantCultureIgnoreCase )
              )
          )
          .ToList( );

      if ( trackerNames.Count == 0 )
        return new List<TrackResponseItem>( );

      RevisedTrackerState[] snapshots = GetSnapshots( trackerNames, groupConfig );

      List<TrackResponseItem> result = new List<TrackResponseItem>( snapshots.Length );
      for ( int iResult = 0; iResult < trackerNames.Count; iResult++ )
      {
        string trackerName = trackerNames[iResult].Name;
        TrackResponseItem trackResponseItem;
        trackResponseItem.TrackerName = trackerName;
        trackResponseItem.Track = null;

        //ForeignId trackerForeignId = trackerIds[iResult].ForeignId;

        bool isReqFound = false;
        TrackRequestItem req = default( TrackRequestItem );
        for ( int iReq = 0; iReq < this.trackRequests.Length; iReq++ )
        {
          if ( string.Equals( this.trackRequests[iReq].TrackerName, trackerName, StringComparison.InvariantCultureIgnoreCase ) )
          {
            isReqFound = true;
            req = this.trackRequests[iReq];
            break;
          }
        }

        if ( !isReqFound )
        { // Normally should not happen. All in TrackerNames came from TrackRequests.
          throw new InvalidOperationException( string.Format( "Can't find track request data for {0}", trackerName ) );
        }

        // No need to lock since it's the only snapshot reading here.
        TrackerState snapshot = snapshots[iResult];
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
              trackPoint.Age = CalcAgeInSeconds( trackPointData.ForeignTime );

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
      }

      if ( Log.IsDebugEnabled )
        Log.DebugFormat(
          "Finishing TracksService.GetResult, call id {0}, got {1} tracks", CallId, result.Count );

      return result;
    }

    public List<TrackResponseItem> EndGetTracks( IAsyncResult asyncResult )
    {
      try
      {
        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Entering EndGetTracks for call id {0}, group {1}...", CallId, Group );

        var tracksAsyncResult = ( AsyncResult<List<TrackResponseItem>> ) asyncResult;

        List<TrackResponseItem> result = tracksAsyncResult.EndInvoke( );

        if ( Log.IsDebugEnabled )
          Log.DebugFormat( "Finishing EndGetTracks, for call id {0}, group {1} got result: {2}",
            CallId, Group, result );

        return result;
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Error in EndGetTracks, call id {0}: {1}", CallId, exc.Message );
        throw;
      }
      finally
      {
        DecrementCallCount( );
      }
    }
  }
}