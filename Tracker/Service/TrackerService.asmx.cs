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
  public class TrackerService : System.Web.Services.WebService
  {
    private bool isNew = true;

    private static readonly ILog IncrErrorsLog = LogManager.GetLogger( "IncrErrors" );
    private static readonly ILog Log = LogManager.GetLogger( "TDM" );

    [WebMethod]
    public GroupData GetCoordinates( int group, string srcSeed, DateTime scrTime /* this parameter to preven client-side response caching */ )
    {
      Subservices.ICoordinatesService trackerService =
        MgrService.GetCoordinatesService( group, srcSeed );

      // TODO: remove group, srcSeed params (already in constructor)
      IAsyncResult ar = trackerService.BeginGetCoordinates( group, srcSeed, null, null );
      ar.AsyncWaitHandle.WaitOne( );

      return trackerService.EndGetCoordinates( ar );
    }

    [WebMethod]
    public List<TrackResponseItem> GetTracks( int group, TrackRequest trackRequest, DateTime scriptCurrTimet )
    {
      Subservices.ITracksService trackerService =
        MgrService.GetTracksService( group, trackRequest );

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

    [WebMethod]
    public void TestCheck( string msg )
    { // not just a debug method, this one is used from main.js script
      IncrErrorsLog.Error( msg );
    }
  }
}