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

    public readonly DateTime RefreshTime = DateTime.UtcNow;

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

    /// <summary>
    /// TODO: remove from here after upgrading to the new scheduler because it was moved to TrackerState
    /// Merges data from existing tracker state that was loaded earlier and updated state that just came from the
    /// foreign server. It is actually "merge" because if old location is not null and new error is not null than 
    /// the result would have old location and new error. Also it checks if result is the same as the existing tracker 
    /// state, and if so just returns old result, keeping the version of the tracker data. Otherwise result has brand 
    /// new version.
    /// </summary>
    /// <remarks>
    /// TODO unit test:
    /// check that RefreshTime always initilized inside this method, not the one taken from oldResult (which is possible
    /// e.g. if make type of oldResult RevisedTrackerState and returning it under some circumstances)
    /// </remarks>
    /// <param name="oldResult"></param>
    /// <param name="newResult"></param>
    /// <returns></returns>
    public static TrackerState Merge( TrackerState oldResult, TrackerState newResult )
    {
      if ( oldResult == null && newResult == null ) return null;

      if ( oldResult == null && newResult != null )
        return new TrackerState( newResult.Position, newResult.Error, newResult.Tag, UpdateReason.BrandNew );

      if ( oldResult != null && newResult == null )
        return new TrackerState( oldResult );

      LocationLib.Data.Position position;
      if ( oldResult.Position != null && newResult.Position != null )
      {
        if ( oldResult.Position.CurrPoint.ForeignTime > newResult.Position.CurrPoint.ForeignTime )
        { // should be a rare case, but still possible. E.g. it could be like this:
          // - unoffical request with very fresh data succeeded (see LocationRequests internals)
          // - 15 seconds later unoffical request fails, and official started, returning old data.
          position = oldResult.Position;
        }
        else if ( oldResult.Position.CurrPoint.ForeignTime < newResult.Position.CurrPoint.ForeignTime )
        {
          position = newResult.Position;
        }
        else
        { // foreign times are equal here.
          position = newResult.Position;
        }
      }
      else if ( oldResult.Position != null )
      {
        position = oldResult.Position;
      }
      else
      { // oldResult.Location is null, but newResult.Location could be either null or not null here.
        position = newResult.Position;
      }

      /* We need to ensure that result has at either Location or Error set (or both).
       * 
       * - if location is still null here, then both results Location set to null (see checks above). 
       *   But neither constructor allows both Location and Error to be null => both Errors are not null,
       *   So newResult have Error set, and the merged one will have at least Error set to the new 
       *   result Error (and probably Location).
       *   
       * - if location is not null, then it doesn't matter if newResult.Error is null or not - the merged 
       *   one still have Location set (and probably error - if the new one has it).
       */

      LocationLib.Data.Error error = newResult.Error;

      /* Now find out what modificationTs is. If no change happens, then modificationTs is oldResult.modificationTs, 
       * otherwise it's Now.ToFileTimeUtc. Note that the final result of the method is made out of location variable 
       * and newResult.Error (see below). So following is possible:
       * - Location is obtained for a tracker, resulting in Tracker( Location=loc1, Error=null )
       * - On a next query to the SPOT server, error is received for the tracker so newResult
       *   is Tracker(Location=null, Error=err1). But this method merges data, so final return result will
       *   be Tracker(Location=loc1, Error=err1). 
       * That's why oldResult should be checked against merged result (which is not ready at this point) to find 
       * resulting modificationTs.
       * So below oldResult.Location is compared against location variable (NOT newResult.Location), and oldResult.Error is 
       * compared against newResult.Error
       */

      bool arePositionsEqual = LocationLib.Data.Position.ArePositionsEqual( oldResult.Position, position );
      bool areErrorsEqual = LocationLib.Data.Error.AreErrorsEqual( oldResult.Error, error );

      bool areTrackerRequestResultsEqual = arePositionsEqual && areErrorsEqual;

      if ( areTrackerRequestResultsEqual )
      {
        // Create a new object with old Pos and Err - to have new RefreshTime. Note that updating oldResult.RefreshTime is
        // not allowed because every RevisedTrackerState instance can be referenced as a snapshot in many places, which 
        // is considered immutable. That's why this field (as all others) is read-only.
        return new RevisedTrackerState( oldResult );
      }

      UpdateReason updatedPart;
      if ( arePositionsEqual && areErrorsEqual )
        updatedPart = UpdateReason.AllNew;
      else if ( arePositionsEqual )
        updatedPart = UpdateReason.NewErr;
      else
        updatedPart = UpdateReason.NewPos;

      return new RevisedTrackerState( position, error, newResult.Tag, updatedPart );
    }
  }
}