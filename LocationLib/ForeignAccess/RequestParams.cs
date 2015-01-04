using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlyTrace.LocationLib.ForeignAccess
{
  public struct RequestParams
  {
    /// <summary>Foregn ID for a specific system</summary>
    public string Id;

    /// <summary>
    /// If a track has already been loaded, it can be used by the request to get trailing points which might
    /// be unavailable from the foreign system anymore (or would require a separate efforts to re-get).
    /// <para>Can be null, in this case the request should do all that it can to get all the points.</para>
    /// <para>See Remarks comment section for details.</para>
    /// </summary>
    /// <remarks>
    /// This is used when e.g. requesting SPOT, because it spits data out by pages of 50 points. If the 
    /// older track is already known, no need to request the next page after the newest one is obtained.
    /// But other systems also can use that to optimise their requests.
    /// </remarks>
    public IEnumerable<Data.TrackPointData> ExistingTrack;
  }
}
