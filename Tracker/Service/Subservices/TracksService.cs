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

namespace FlyTrace.Service.Subservices
{
  // TODO: remove
  internal interface ITrackerService
  {
    IAsyncResult BeginGetTracks
    (
      int group,
      TrackRequestItem[] trackRequests,
      AsyncCallback callback,
      object asyncState,
      out long callId
    );

    List<TrackResponseItem> EndGetTracks( IAsyncResult asyncResult );
  }

  public class TracksService : TrackerServiceBase<List<TrackResponseItem>>, ITrackerService
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
          callCount
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
        Log.ErrorFormat( "Error in BeginGetTracks: {0}", exc.ToString( ) );
        DecrementCallCount( );
        throw;
      }
    }

    protected override List<TrackResponseItem> GetResult( GroupConfig groupConfig )
    {
      var trackerIds =
        groupConfig
          .TrackerIds
          .Where(
            req =>
              this.trackRequests.Any(
                tr => string.Equals( tr.TrackerName, req.Name, StringComparison.InvariantCultureIgnoreCase )
              )
          )
          .ToList( );

      if ( trackerIds.Count == 0 )
        return new List<TrackResponseItem>( );

      return null;
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
        DecrementCallCount();
      }
    }
  }
}