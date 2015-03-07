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
using System.Data;
using System.Linq;
using System.Text;

namespace FlyTrace.Service
{
  public static class ServiceFacade
  {
    public static void Init( string revisionFilePath )
    {
      Internals.ForeignRequestsManager.Init( revisionFilePath );
    }

    public static void Deinit( )
    {
      Internals.ForeignRequestsManager.Deinit( );
    }

    public static GroupData GetCoordinates( int group, string srcSeed )
    {
      return Internals.TrackerFacade.GetCoordinates( group, srcSeed );
    }

    public static List<TrackResponseItem> GetTracks( int group, TrackRequest trackRequest )
    {
      return Internals.TrackerFacade.GetTracks( group, trackRequest );
    }

    public static void TestCheck( string msg )
    {
      Internals.TrackerFacade.TestCheck( msg );
    }

    public static AdminStat GetAdminStat( )
    {
      return Internals.AdminFacade.GetAdminStat( );
    }

    public static DataSet GetStatistics( )
    {
      return Internals.ForeignRequestsManager.Singleton.GetStatistics( );
    }

    public static List<LocationLib.TrackerState> GetAdminTrackers( )
    {
      return Internals.ForeignRequestsManager.Singleton.GetAdminTrackers( );
    }
  }
}