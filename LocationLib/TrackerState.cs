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
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace FlyTrace.LocationLib
{
  /// <summary>
  /// Tracker state might have location data and/or error description. It always has either location or error, 
  /// and might have both. In latter case location refresh time is always earlier than error refresh time.
  /// It is an inmutable type, just to make sure that once Snapshot field of TrackerStateHolder is taken, it's 
  /// never changed.
  /// </summary>
  public class TrackerState
  {
    public readonly Data.Position Position;

    public readonly Data.Error Error;

    /// <summary>
    /// Debug string specific to the foreign system, e.g. feed kind for SPOT. Used for diagnostics
    /// only.
    /// </summary>
    public readonly string Tag;

    public readonly DateTime RefreshTime = DateTime.Now.ToUniversalTime( );

    /// <summary>
    /// Makes an instance with the lat and lon found in the response from the foreign web server.
    /// </summary>
    /// <param name="fullTrack">Not null, not empty value (i.e. with at least one element).</param>
    /// <param name="tag">Debug string specific to the foreign system, see also <see cref="Tag"/> for details.</param>
    public TrackerState( IEnumerable<Data.TrackPointData> fullTrack, string tag )
    {
      Position = new Data.Position( fullTrack );
      Tag = tag;
    }

    /// <summary>
    /// Makes an instance with a predefined problem description. Note that for aux.error 
    /// (see <see cref="RequestResultState.AuxError"/>) special separate constructor 
    /// <see cref="RequestResult(string)"/> should be used.
    /// </summary>
    /// <param name="errCode"></param>
    /// <param name="tag">Debug string specific to the foreign system, see also <see cref="Tag"/> for details.</param>
    public TrackerState( Data.ErrorType errCode, string tag )
    {
      if ( errCode == Data.ErrorType.AuxError )
        throw new ArgumentException(
          string.Format( "This constructor doesn't accept error state {0}, another constructor should be used for that.", errCode ) );

      Error = new Data.Error( errCode, null );
      Tag = tag;
    }

    /// <summary>Makes an instance with aux.error message.</summary>
    /// <param name="auxErrorMessage"></param>
    /// <param name="tag">Debug string specific to the foreign system, see also <see cref="Tag"/> for details.</param>
    public TrackerState( string auxErrorMessage, string tag )
    {
      if ( auxErrorMessage == null )
        throw new ArgumentNullException( "auxErrorMessage" );

      Error = new Data.Error( Data.ErrorType.AuxError, auxErrorMessage );
      Tag = tag;
    }

    protected TrackerState( Data.Position position, Data.Error error, string tag )
    {
      if ( position == null && error == null )
        throw new ArgumentException( "At least one parameter should be not null" );

      Position = position;
      Error = error;
      Tag = tag;
    }

    public override string ToString( )
    {
      string resultStr;

      if ( Position == null && Error == null )
      {
        resultStr = "(none)";
      }
      else
      {
        resultStr = "";
        if ( Position != null )
          resultStr += Position.ToString( );

        if ( Error != null )
        {
          if ( !string.IsNullOrEmpty( resultStr ) )
          {
            resultStr += " \\ ";
          }
          resultStr += Error.ToString( );
        }
      }

      return resultStr;
    }
  }
}