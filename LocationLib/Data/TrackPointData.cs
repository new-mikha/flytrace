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

namespace FlyTrace.LocationLib.Data
{
  /// <summary>
  /// Point with its time, location type and optinally user message
  /// </summary>
  public class TrackPointData
  {
    internal TrackPointData(
      string locationType,
      double latitude,
      double longitude,
      DateTime foreignTime,
      string userMessage
    )
    {
      LocationType = locationType;
      Latitude = latitude;
      Longitude = longitude;
      ForeignTime = foreignTime;
      UserMessage = userMessage;
      // If adding new field/property - don't forget to add it to equalityExpression below
    }

    public readonly string LocationType;
    public readonly double Latitude;
    public readonly double Longitude;
    public readonly DateTime ForeignTime;
    public readonly string UserMessage;

    // EqualityExpressionCheck checks that all fields/properties of the type are included into the expression
    private static Func<TrackPointData, TrackPointData, bool> equalityExpression =
      Utils.EqualityExpressionCheck<TrackPointData>(
        ( x, y ) =>
          x.LocationType == y.LocationType &&
          x.Latitude == y.Latitude &&
          x.Longitude == y.Longitude &&
          x.ForeignTime == y.ForeignTime &&
          x.UserMessage == y.UserMessage
      );

    internal static bool ArePointsEqual( TrackPointData x, TrackPointData y )
    {
      if ( ReferenceEquals( x, y ) ) return true;

      // at this point at least one is not null because of ReferenceEquals above.
      if ( x == null || y == null ) return false;

      // so now both are not nulls and different instances.

      return equalityExpression( x, y );
    }

    internal static bool ArePointArraysEqual( TrackPointData[] x, TrackPointData[] y )
    {
      if ( ReferenceEquals( x, y ) ) return true;

      // at this point at least one is not null because of ReferenceEquals above.
      if ( x == null || y == null ) return false;

      // so now both are not nulls and different instances.

      if ( x.Length != y.Length )
      {
        return false;
      }

      for ( int i = 0; i < x.Length; i++ )
      {
        if ( !ArePointsEqual( x[i], y[i] ) )
        {
          return false;
        }
      }

      return true;
    }

    public override string ToString( )
    {
      return
        string.Format(
          "{0}, {1}, {2} ({3}), '{4}'",
          Latitude,
          Longitude,
          ForeignTime.ToString( "u" ),
          Tools.GetAgeStr( ForeignTime ),
          UserMessage
        );
    }
  }
}