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
  /// <summary>
  /// The entry point for all communication between a service host and the coordinates 
  /// service, including requests from the end clients - those request are assumed to be 
  /// exposed going through the host which e.g. exposes corresponding web methods to end
  /// clients.
  /// </summary>
  public static class ServiceFacade
  {
    /// <summary>
    /// Initialises the coordinates service.
    /// </summary>
    /// <param name="dataFolderPath"></param>
    public static void Init( string dataFolderPath )
    {
      Internals.ForeignRequestsManager.Init( dataFolderPath );
    }

    /// <summary>
    /// Deinitialises the coordinates service.
    /// </summary>
    public static void Deinit( )
    {
      Internals.ForeignRequestsManager.Deinit( );
    }

    /// <summary>
    /// Returns coordinates of the trackers for a specific group. This method doesn't return tracks,
    /// it's for current coordinates only. For tracks, see <see cref="GetTracks"/> method. See also 
    /// Remarks section for details.
    /// </summary>
    /// <param name="group">group id</param>
    /// <param name="srcSeed">Seed value used for incremental group updates. Can be null. 
    /// See Remarks section for details.</param>
    /// <returns></returns>
    /// <remarks>Seed value is used for incremental updates, which in turn are used to reduce amount
    /// of the transferred data by including only updated trackers. When the seed is null, coords of
    /// all trackers in the group are returned. For a subsequent call, a client could specify the 
    /// value of the <see cref="GroupData.Res"/> parameter returned in the PREVIOUS call (NOT just 
    /// the first one!) to get an incremental update.</remarks>
    public static GroupData GetCoordinates( int group, string srcSeed )
    {
      return Internals.TrackerFacade.GetCoordinates( group, srcSeed );
    }

    /// <summary>
    /// Returns tracks for specific trackers in a specific group.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="trackRequest"></param>
    /// <returns></returns>
    public static List<TrackResponseItem> GetTracks( int group, TrackRequest trackRequest )
    {
      return Internals.TrackerFacade.GetTracks( group, trackRequest );
    }

    /// <summary>
    /// A client can call this method to log a client-side error
    /// </summary>
    /// <param name="msg"></param>
    public static void TestCheck( string msg )
    {
      Internals.TrackerFacade.TestCheck( msg );
    }

    /// <summary>
    /// Resets cache of group definitions (names/ids of trackers in the group, group data like "show messages")
    /// Should be called after a group (just any group) is changed in DB
    /// </summary>
    public static void ResetGroupsDefCache( )
    {
      Internals.GroupFacade.ResetCache( );
    }

    /// <summary>
    /// Resets trackers cache, so the service enters the state like it's just initialised.
    /// Required to be called when a system time has changed (the service doesn't check it itself)
    /// </summary>
    public static void ResetTrackersCache( )
    {
      Internals.ForeignRequestsManager.Singleton.ClearTrackers( );
    }

    /// <summary>
    /// Returns current most important diag messages
    /// </summary>
    /// <returns></returns>
    public static AdminStat GetAdminStatBrief( )
    {
      return Internals.AdminFacade.GetStatBrief( );
    }

    /// <summary>
    /// Returns internal call statistics
    /// </summary>
    /// <returns></returns>
    public static DataSet GetAdminCallStatistics( )
    {
      return Internals.AdminFacade.GetCallStatistics( );
    }

    /// <summary>
    /// Returns diag data for each tracker that is currently cached by the service
    /// </summary>
    /// <returns></returns>
    public static List<AdminTracker> GetAdminTrackers( )
    {
      return Internals.AdminFacade.GetAdminTrackers( );
    }
  }
}