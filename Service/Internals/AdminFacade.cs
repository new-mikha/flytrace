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

using FlyTrace.LocationLib.ForeignAccess;

namespace FlyTrace.Service.Internals
{
  internal static class AdminFacade
  {
    public static AdminStat GetStatBrief( )
    {
      AdminStat result;
      result.ForeignSourcesStat = new ForeignSourceStat[ForeignAccessCentral.LocationRequestFactories.Count];

      int iForeignFactory = 0;
      foreach ( KeyValuePair<string, LocationRequestFactory> kvp in
        ForeignAccessCentral.LocationRequestFactories )
      {
        ForeignSourceStat stat;
        stat.Name = kvp.Key;
        stat.Stat = kvp.Value.GetStat( out stat.IsOk );

        result.ForeignSourcesStat[iForeignFactory++] = stat;
      }

      result.Messages =
        ForeignRequestsManager
        .Singleton
        .AdminAlerts
        .GetMessages( )
        .Select(
          kvp =>
            new AdminMessage { Key = kvp.Key, Message = kvp.Value }
        )
        .ToArray( );

      result.CurrentRevision = 0; // TODO: replace with new rev.persister result

      result.CoordAccessCount = ForeignRequestsManager.Singleton.AdminAlerts.CoordAccessCount;

      result.StartTime = ForeignRequestsManager.Singleton.AdminAlerts.StartTime;

      return result;
    }

    public static System.Data.DataSet GetCallStatistics( )
    {
      return ForeignRequestsManager.Singleton.GetCallStatistics( );
    }

    public static List<AdminTracker> GetAdminTrackers( )
    {
      return ForeignRequestsManager.Singleton.GetAdminTrackers( );
    }
  }
}