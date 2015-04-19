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

using log4net;

namespace FlyTrace.Service.Internals
{
  /// <summary>
  /// Each instances serves one request to the tracker service
  /// </summary>
  internal static class TrackerFacade
  {
    private static readonly ILog IncrErrorsLog = LogManager.GetLogger( "IncrErrors" );
    private static readonly ILog Log = LogManager.GetLogger( "TDM" );

    public static GroupData GetCoordinates( int group, string srcSeed )
    {
      if ( Log.IsDebugEnabled )
      {
        Log.DebugFormat( "GetCoordinates call start for {0} ({1})", group, Properties.Settings.Default.GroupDefCacheEnabled );
        System.Threading.Thread.MemoryBarrier( );
      }

      var coordinatesService = new Subservices.CoordinatesService( group, srcSeed );

      IAsyncResult ar = coordinatesService.BeginGetCoordinates( null, null );

      bool handleSignaled;
      if ( AsyncResultNoResult.DefaultEndWaitTimeout > 0 )
        handleSignaled = ar.AsyncWaitHandle.WaitOne( AsyncResultNoResult.DefaultEndWaitTimeout );
      else
        handleSignaled = ar.AsyncWaitHandle.WaitOne( );

      if ( !handleSignaled )
      {
        Log.FatalFormat( "GetCoordinates call has timed out for call id {0}.", coordinatesService.CallId );
        return default( GroupData );
      }

      GroupData result = coordinatesService.EndGetCoordinates( ar );

      if ( Log.IsDebugEnabled )
      {
        System.Threading.Thread.MemoryBarrier( );
        Log.DebugFormat( "GetCoordinates call end for {0}", group );
      }

      return result;
    }

    public static List<TrackResponseItem> GetTracks( int group, TrackRequest trackRequest )
    {
      var trackerService =
        new Subservices.TracksService( group, trackRequest );

      long callId;
      IAsyncResult ar = trackerService.BeginGetTracks( group, trackRequest.Items, null, null, out callId );

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

      return trackerService.EndGetTracks( ar );
    }

    public static void TestCheck( string msg )
    { // not just a debug method, this one is used from main.js script
      IncrErrorsLog.Error( msg );
    }
  }
}