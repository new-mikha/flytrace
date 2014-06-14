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
using System.Web;
using System.Web.Services;
using FlyTrace.LocationLib;
using System.Threading;
using FlyTrace.LocationLib.ForeignAccess;

namespace FlyTrace.Service.Administration
{
  /// <summary>
  /// Summary description for AdminFacade
  /// </summary>
  [WebService( Namespace = "http://flytrace.com/" )]
  [WebServiceBinding( ConformsTo = WsiProfiles.BasicProfile1_1 )]
  [System.ComponentModel.ToolboxItem( false )]
  // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
  // [System.Web.Script.Services.ScriptService]
  public class AdminFacade : System.Web.Services.WebService
  {
    public struct AdminMessage
    {
      public string Key;

      public string Message;
    }

    public struct ForeignSourceStat
    {
      public string Name;
      public string Stat;
      public bool IsOk;
    }

    public struct AdminStat
    {
      public ForeignSourceStat[] ForeignSourcesStat;

      public AdminMessage[] Messages;

      public DateTime StartTime;

      public int CoordAccessCount;

      public int CurrentRevision;
    }

    [WebMethod]
    public AdminStat GetAdminStat( )
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
        TrackerDataManager.Singleton.AdminAlerts
        .GetMessages( )
        .Select(
          kvp =>
            new AdminMessage { Key = kvp.Key, Message = kvp.Value }
        )
        .ToArray( );

      RevisionGenerator.TryGetCurrentRevision( out result.CurrentRevision );

      result.CoordAccessCount = TrackerDataManager.Singleton.AdminAlerts.CoordAccessCount;

      result.StartTime = TrackerDataManager.Singleton.AdminAlerts.StartTime;

      return result;
    }
  }
}