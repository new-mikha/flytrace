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

namespace FlyTrace.Service
{
  public class DemoTrackers
  {
    public static DemoTrackers Singleton = new DemoTrackers( );

    public GroupData GetDemo( )
    {
      GroupData result;

      result.Trackers = new List<CoordResponseItem>( );
      result.Src = null;
      result.Res = null;

      {
        CoordResponseItem tracker0 = default( CoordResponseItem );
        tracker0.Name = "Attila";
        tracker0.Type = "TRACK";
        tracker0.Lat = -31.03764;
        tracker0.Lon = 150.43362;
        tracker0.IsOfficial = false;
        tracker0.Ts = DateTime.UtcNow.AddMinutes( -5 );
        tracker0.Age = TrackerDataManager.CalcAge( tracker0.Ts );
        tracker0.PrevLat = -30.94232;
        tracker0.PrevLon = 150.53076;
        tracker0.PrevTs = tracker0.Ts.AddMinutes( -5 );
        tracker0.PrevAge = TrackerDataManager.CalcAge( tracker0.PrevTs );

        result.Trackers.Add( tracker0 );
      }

      {
        CoordResponseItem tracker1 = default( CoordResponseItem );
        tracker1.Name = "Alex";
        tracker1.Type = "OK";
        tracker1.Lat = -31.03764 - 0.1;
        tracker1.Lon = 150.43362 - 0.1;
        tracker1.IsOfficial = false;
        tracker1.Ts = DateTime.UtcNow.AddMinutes( -5 );
        tracker1.Age = TrackerDataManager.CalcAge( tracker1.Ts );
        tracker1.PrevLat = -30.94232 - 0.25;
        tracker1.PrevLon = 150.53076 - 0.08;
        tracker1.PrevTs = tracker1.Ts.AddMinutes( -5 );
        tracker1.PrevAge = TrackerDataManager.CalcAge( tracker1.PrevTs );
        tracker1.UsrMsg = "Just landed and would love to see my driver";

        result.Trackers.Add( tracker1 );
      }

      result.Ver = 1;

      result.IncrSurr = false;

      result.CallId = -1;

      result.StartTs = null;

      return result;
    }
  }
}