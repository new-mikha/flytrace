using System;
using System.Collections.Generic;

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

  public class TracksService : TrackerServiceBase, ITrackerService
  {
    private readonly TrackRequest trackRequest;

    public TracksService( int group, TrackRequest trackRequest )
      : base( group )
    {
      this.trackRequest = trackRequest;
    }

    /// <summary>TODO: remove unused* fields</summary>
    public IAsyncResult BeginGetTracks( int unsused1, TrackRequestItem[] unused2, AsyncCallback callback, object asyncState, out long callId )
    {
      callId = CallId; // TODO: remove callId out param

      throw new NotImplementedException( );
    }

    public List<TrackResponseItem> EndGetTracks( IAsyncResult asyncResult )
    {
      throw new NotImplementedException( );
    }
  }
}