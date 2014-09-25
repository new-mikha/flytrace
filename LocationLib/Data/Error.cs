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
  public enum ErrorType
  {
    ResponseHasNoData,
    ResponseHasBadSchema,
    ResponseIsTooBig,
    BadTrackerId,

    /// <summary>E.g. exception message from WebRequest.BeginGetResponse(...) 
    /// or WebResponse.GetResponseStream, or any other exception happened during request</summary>
    AuxError
  }

  public class Error 
  {
    internal Error( ErrorType type, string auxMessage )
    {
      Type = type;
      AuxMessage = auxMessage;
      // If adding new field/property - don't forget to add it to equalityExpression below
    }

    public readonly ErrorType Type;
    public readonly string AuxMessage;

    // EqualityExpressionCheck checks that all fields/properties of the type are included into the expression
    private static Func<Error, Error, bool> equalityExpression =
      Utils.EqualityExpressionCheck<Error>(
        ( x, y ) =>
          x.Type == y.Type &&
          x.AuxMessage == y.AuxMessage &&
          x.Message == y.Message // Although Message is a function of Type & AuxMessage, include it here for
                                 // completeness. So if later it is made independent property we still have it here.
      );

    public static bool AreErrorsEqual( Error x, Error y )
    {
      if ( ReferenceEquals( x, y ) )  // returns true also if both are null
        return true;

      // at this point at least one is not null because of ReferenceEquals above.
      if ( x == null || y == null ) return false;

      // so now both are not nulls and different instances.

      return equalityExpression( x, y );
    }

    public override string ToString( )
    {
      return string.Format( "{0} {1}", Type, AuxMessage );
    }

    public string Message
    {
      get
      {
        switch ( Type )
        {
          case ErrorType.ResponseHasNoData:
            return "No coordinates found for the tracker.";

          case ErrorType.ResponseHasBadSchema:
            return "Inconsistent SPOT server response schema.";

          case ErrorType.ResponseIsTooBig:
            return "Location data was not recognised for the tracker.";

          case ErrorType.AuxError:
            return "Cannot retrieve location data for the tracker: " + AuxMessage;

          case ErrorType.BadTrackerId:
            return "The tracker ID is not recognized by the SPOT server.";

          default:
            return "Cannot retrieve location data for the tracker: " + Type.ToString( );
        }
      }
    }
  }
}