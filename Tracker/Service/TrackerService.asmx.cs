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
    public GroupData GetCoordinates
    (
      int group,
      string srcSeed,
      DateTime scrTime // this parameter is to prevent client-side response caching, not used really on the server side.
    )
    {
      return ServiceFacade.GetCoordinates( group, srcSeed );
    }

    [WebMethod]
    public List<TrackResponseItem> GetTracks( int group, TrackRequest trackRequest, DateTime scriptCurrTime )
    {
      return ServiceFacade.GetTracks( group, trackRequest );
    }

    [WebMethod]
    public void TestCheck( string msg )
    { // not just a debug method, this one is used from main.js script
      ServiceFacade.TestCheck( msg );
    }
  }
}