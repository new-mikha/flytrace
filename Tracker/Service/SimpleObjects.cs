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
using System.Xml.Serialization;

using FlyTrace.LocationLib;
using System.Runtime.Serialization;

namespace FlyTrace.Service
{
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

  public struct TrackRequest
  {
    public TrackRequestItem[] Items;
  }

  public struct TrackRequestItem
  {
    public string TrackerName;

    public DateTime? LaterThan;
  }

  public struct TrackResponseItem
  {
    public string TrackerName;

    public List<TrackPoint> Track;
  }

  /// <summary>
  /// Following kinds of data are supported:
  /// 1. Full non-empty
  /// 2. Full empty (when group doesn't contain trackers)
  /// 3. Incremental non-empty
  /// 4. Incremental empty (no update)
  /// 
  /// empty/non-empty: Trackers field (null is Empty)
  /// Full/incremental: SrcSeed field (null if Full)
  /// 
  /// empty incremental: only Res has a value which is "nil"
  /// </summary>
  public struct GroupData
  {
    public List<CoordResponseItem> Trackers;

    public string Src;

    public string Res;

    public int Ver;

    public bool IncrSurr;

    public DateTime? StartTs;

    public long CallId;
  }

  /// <summary>
  /// This type is serialized just as "Tracker". This name is really overloaded here,
  /// so giving the class a different name.
  /// </summary>
  [XmlType( "Tracker" )]
  public struct CoordResponseItem
  {
    public string Name;

    public double Lat;

    public double Lon;

    public string Type;

    public bool IsOfficial;

    public string UsrMsg;

    public DateTime Ts;

    public int Age;

    public string Error;

    public double PrevLat;

    public double PrevLon;

    public DateTime PrevTs;

    public int PrevAge;

    public bool IncrTest;

    public bool IsHidden;

    #region ShouldSerializeXXX methods
    // looks like undocumented feature, or I didn't find the proper doc in MSDN. But it works (found on net)

    public bool ShouldSerializeIsHidden( )
    {
      return IsHidden == true;
    }

    public bool ShouldSerializeLat( )
    {
      return Lat != 0.0;
    }

    public bool ShouldSerializeLon( )
    {
      return Lon != 0.0;
    }

    public bool ShouldSerializeIsOfficial( )
    {
      return IsOfficial == true;
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
}