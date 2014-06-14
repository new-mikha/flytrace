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
using System.Linq.Expressions;

namespace FlyTrace.LocationLib.Data
{
  public class Position
  {
    internal Position( IEnumerable<TrackPointData> fullTrack )
    {
      if ( fullTrack == null )
        throw new ArgumentNullException( "fullTrack" );

      CurrPoint = fullTrack.First( ); // throws an exception if it's empty - it's ok (because it shouldn't be empty)
      PreviousPoint = fullTrack.Skip( 1 ).FirstOrDefault( ); // could be null
      FullTrack = fullTrack.ToArray( );
      // If adding new field/property - don't forget to add it to equalityExpression below
    }

    public readonly TrackPointData CurrPoint;

    /// <summary>It's a type from the tracker, it could be any so don't make it strong-typed.</summary>
    public string Type
    {
      get { return CurrPoint.LocationType; }
    }

    public string UserMessage
    {
      get { return CurrPoint.UserMessage; }
    }

    public readonly TrackPointData PreviousPoint;

    public readonly TrackPointData[] FullTrack;

    // EqualityExpressionCheck checks that all fields/properties of the type are included into the expression.
    private static Func<Position, Position, bool> equalityExpression =
      Utils.EqualityExpressionCheck<Position>(
        ( x, y ) =>
          x.Type == y.Type &&  // Although Type and UserMessage are functions of Curr, which in turn is a function 
          x.UserMessage == y.UserMessage && // of FullTrack, include it here for completeness. So if later it is 
          TrackPointData.ArePointsEqual( x.CurrPoint, y.CurrPoint ) && // made independent property(s) we still have it here.
          TrackPointData.ArePointsEqual( x.PreviousPoint, y.PreviousPoint ) &&
          TrackPointData.ArePointArraysEqual( x.FullTrack, y.FullTrack )
      );

    public override string ToString( )
    {
      return string.Format( "{0} {1} {2} {3}",
        CurrPoint.Latitude, CurrPoint.Longitude, CurrPoint.ForeignTime, Type );
    }

    public static bool ArePositionsEqual( Position x, Position y )
    {
      if ( ReferenceEquals( x, y ) ) return true;

      // at this point at least one is not null because of ReferenceEquals above.
      if ( x == null || y == null ) return false;

      // so now both are not nulls and different instances.

      return equalityExpression( x, y );
    }
  }
}