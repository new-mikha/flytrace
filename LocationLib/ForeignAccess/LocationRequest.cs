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

using log4net;

namespace FlyTrace.LocationLib.ForeignAccess
{
  public abstract class LocationRequest
  {
    public static ILog ErrorHandlingLog = LogManager.GetLogger( "TDM.LocReq.ErrorHandling" );

    public readonly DateTime StartTs = TimeService.Now;

    public readonly string Id;

    protected LocationRequest( string id )
    {
      Id = id;
    }

    /// <summary>
    /// Just a helper property, combines <see cref="ForeignType"/> and <see cref="Id"/> into one
    /// </summary>
    public ForeignId ForeignId
    {
      get
      {
        return new ForeignId( ForeignType, Id );
      }
    }

    /// <summary>
    /// Location request id, set by ancestor, unique across all requests in this process.
    /// </summary>
    public long Lrid { get; protected set; }

    internal event Action<LocationRequest, TrackerState> ReadLocationFinished;

    public abstract string ForeignType { get; }

    public abstract IAsyncResult BeginReadLocation( AsyncCallback callback, object state );

    public TrackerState EndReadLocation( IAsyncResult ar )
    {
      // At the moment do not need to report to the Factory any exception thrown 
      // from EndReadLocationProtected, so if it happens just let it go out:
      TrackerState result = EndReadLocationProtected( ar );

      if ( ReadLocationFinished != null )
      {
        ReadLocationFinished( this, result );
      }

      return result;
    }

    protected abstract TrackerState EndReadLocationProtected( IAsyncResult ar );

    /// <summary>Implementation should not throw an error, just 
    /// fill <see cref="AbortStat"/> fields if needed.</summary>
    /// <param name="abortStat"></param>
    public abstract void SafelyAbortRequest( AbortStat abortStat );

    public TrackerState ReadLocation( )
    {
      IAsyncResult ar = BeginReadLocation( null, null );
      ar.AsyncWaitHandle.WaitOne( );
      return EndReadLocation( ar );
    }
  }
}