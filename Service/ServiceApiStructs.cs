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
using System.Xml.Serialization;
using System.Diagnostics;

// Group simple structs into one file, otherwise it would be too many files without much value.

namespace FlyTrace.Service
{
  /// <summary>
  /// This type is serialized just as "Tracker". This name is really overloaded in this project,
  /// so giving the struct a different name.
  /// </summary>
  [XmlType( "Tracker" )]
  public struct CoordResponseItem
  {
    /// <summary>Displayed name of the tracker</summary>
    public string Name;

    /// <summary>Latitude. Can be zero along with <see cref="Lon"/> which 
    /// means "no coordinates at the moment"</summary>
    public double Lat;

    /// <summary>Longitude. Can be zero along with <see cref="Lat"/> which 
    /// means "no coordinates at the moment"</summary>
    public double Lon;

    /// <summary>Altitude. Might be zero even if Lat and Lon are set to non-zero values</summary>
    public int? Alt;

    /// <summary>Type of the position: null, "TRACK", "OK", "CUSTOM", "wait", 
    /// or anything else as "HELP"</summary>
    public string Type;

    /// <summary>Used for internal purposes</summary>
    public bool IsOfficial;

    /// <summary>Custom user message associated with the point</summary>
    public string UsrMsg;

    /// <summary>UTC timestamp of the point</summary>
    public DateTime Ts;

    /// <summary>Age of the point in seconds at the moment it's returned</summary>
    public int Age;
    
    /// <summary>Error currently associated with the tracker</summary>
    public string Error;

    /// <summary>Previous point latitude</summary>
    public double PrevLat;

    /// <summary>Previous point longitude</summary>
    public double PrevLon;

    /// <summary>Previous point timestamp</summary>
    public DateTime PrevTs;

    /// <summary>Previous age of the point in seconds at the moment it's returned</summary>
    public int PrevAge;

    /// <summary>Used for internal purposes</summary>
    public bool IncrTest;

    /// <summary>true when the tracker should be hidden, false otherwise</summary>
    public bool IsHidden;

    public string[] DebugLines;

    #region ShouldSerializeXXX methods
    // looks like undocumented feature, or I didn't find the proper doc in MSDN. But it works (found on net)

    public bool ShouldSerializeIsHidden( )
    {
      return IsHidden;
    }

    public bool ShouldSerializeLat( )
    {
      return Lat != 0.0;
    }

    public bool ShouldSerializeLon()
    {
      return Lon != 0.0;
    }

    public bool ShouldSerializeIsOfficial( )
    {
      return IsOfficial;
    }

    public bool ShouldSerializeTs( )
    {
      return Lat != 0.0;
    }

    public bool ShouldSerializeAge( )
    {
      return Lon != 0.0;
    }

    public bool ShouldSerializePrevLat( )
    {
      return PrevLat != 0.0;
    }

    public bool ShouldSerializePrevLon( )
    {
      return PrevLon != 0.0;
    }

    public bool ShouldSerializePrevTs( )
    {
      return PrevLat != 0.0;
    }

    public bool ShouldSerializePrevAge( )
    {
      return PrevLat != 0.0;
    }

    #endregion
  }

  /// <summary>
  /// Following kinds of data are supported:
  /// 1. Full non-empty
  /// 2. Full empty (when group doesn't contain trackers)
  /// 3. Incremental non-empty
  /// 4. Incremental empty (no update)
  /// 
  /// empty/non-empty: Trackers field (null if Empty)
  /// Full/incremental: Src field (null if Full)
  /// 
  /// empty incremental: only Res has a value which is "nil"
  /// </summary>
  public struct GroupData
  {
    /// <summary>
    /// List of coorindates for the trackers. Can be smaller than actual amount of the 
    /// trackers in case of an incremental update.
    /// </summary>
    public List<CoordResponseItem> Trackers;

    /// <summary>
    /// The seed that has been passed to the call of <see cref="ServiceFacade.GetCoordinates"/> by the client.
    /// </summary>
    public string Src;

    /// <summary>
    /// The seed that the client should maintain and pass to the next call 
    /// of <see cref="ServiceFacade.GetCoordinates"/> for an incremental update.
    /// </summary>
    public string Res;

    public string AltitudeUnits;

    /// <summary>
    /// Threshold time for the group. Trackers with timestamps less than this should be hidden.
    /// </summary>
    public DateTime? StartTs;

    /// <summary>Used for internal purposes</summary>
    public int Ver;

    /// <summary>Used for internal purposes</summary>
    public bool IncrSurr;

    /// <summary>Used for internal purposes</summary>
    public long CallId;

    public override string ToString( )
    {
      return
        string.Format(
          "Trackers: {0}; Res: [{1}]; Src: [{2}]; Ver: {3}; IncrSurr: {4}; StartTs: {5}, CallId: {6}",
          Trackers == null ? 0 : Trackers.Count,
          Res, Src, Ver, IncrSurr, StartTs, CallId 
        );
    }
  }

  public struct TrackRequestItem
  {
    public string TrackerName;

    public DateTime? LaterThan;
  }

  public struct TrackRequest
  {
    public TrackRequestItem[] Items;
  }

  public struct TrackPoint
  {
    public double Lat;

    public double Lon;

    public DateTime Ts;

    public int Age;

    /// <summary>
    /// That one set only for the newest point
    /// </summary>
    public string Type;
  }

  public struct TrackResponseItem
  {
    public string TrackerName;

    public List<TrackPoint> Track;
  }

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

  public struct AdminTracker
  {
    public LocationLib.ForeignId ForeignId;

    public LocationLib.TrackerState TrackerState;

    public DateTime AccessTime;

    public DateTime? RefreshTime;

    public int? Revision;
  }

}