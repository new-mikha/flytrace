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
    [WebMethod]
    public GroupData GetCoordinates( int group, string srcSeed, DateTime scrTime /* this parameter to preven client-side response caching */ )
    {
      IAsyncResult ar = TrackerDataManager.Singleton.BeginGetCoordinates( group, srcSeed, null, null );
      ar.AsyncWaitHandle.WaitOne( );

      return TrackerDataManager.Singleton.EndGetCoordinates( ar );
    }

    [WebMethod]
    public CoordResponseItem[] GetCoordinates2( int group, DateTime scriptCurrTime )
    {
      ObsoleteCallsLog.InfoFormat( "GetCoordinates2 call for group {0}", group );

      return GetCoordinates( group, "", scriptCurrTime ).Trackers.ToArray( );
    }

    //[WebMethod]
    //public IAsyncResult BeginGetCoordinates( int group , AsyncCallback callback , object asyncState )
    //{
    //  return TrackerDataManager.Singleton.BeginGetCoordinates( group , callback , asyncState );
    //}

    //[WebMethod]
    //public Tracker[ ] EndGetCoordinates( IAsyncResult asyncResult )
    //{
    //  return TrackerDataManager.Singleton.EndGetCoordinates( asyncResult );
    //}

    private ILog Log = LogManager.GetLogger( "Service" );

    private ILog ObsoleteCallsLog = LogManager.GetLogger( "ObsoleteCallsLog" );

    [WebMethod]
    public List<TrackResponseItem> GetTracks( int group, TrackRequest trackRequest, DateTime scriptCurrTimet )
    {
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
    public List<TrackResponseItem> GetTracks2( int group, TrackRequest trackRequest, DateTime scriptCurrTime )
    {
      ObsoleteCallsLog.InfoFormat( "GetTracks2 call for group {0}", group );

      return GetTracks( group, trackRequest, scriptCurrTime );
    }

    private ILog IncrTestLog = LogManager.GetLogger( "IncrTest" );

    [WebMethod]
    public void TestCheck( string msg )
    {
      IncrTestLog.Error( msg );
    }
  }
}