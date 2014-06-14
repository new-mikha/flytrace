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

using log4net;

namespace FlyTrace.LocationLib.ForeignAccess
{
  public abstract class LocationRequest
  {
    public static ILog TimedOutRequestsLog = LogManager.GetLogger( "TimedOutRequests" );

    public readonly ForeignId ForeignId;

    public long Lrid { get; protected set; }

    public LocationRequest( ForeignId foreignId )
    {
      ForeignId = foreignId;
    }

    internal event Action<LocationRequest, TrackerState> ReadLocationFinished;

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
