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
using log4net;

namespace FlyTrace.Service
{
  /// <summary>
  /// RevisedTrackerState means "TrackerState with Revision" (where Revision is a number like SVN revision)
  /// </summary>
  public class RevisedTrackerState : LocationLib.TrackerState
  {
    private static ILog IncrLog = LogManager.GetLogger( "TDM.IncrUpd" );

    public enum UpdateReason { BrandNew, NewPos, NewErr, AllNew };

    /// <summary>
    /// Identifies Position and Error data inside, but NOT the object itself. There could be more than one instances 
    /// sharing the same Revision number. Such instances would have same coordinates and error, 
    /// but most probably different RefreshTime value.
    /// </summary>
    public readonly int? DataRevision;

    public readonly UpdateReason UpdatedPart;

    /// <summary>Gets pos and err from parameters, and gets NEW DataRevision and RefreshTime</summary>
    private RevisedTrackerState
    (
      LocationLib.Data.Position position,
      LocationLib.Data.Error error,
      UpdateReason updatedPart
    )
      : base( position, error )
    {
      int revision;
      if ( RevisionGenerator.TryIncrementRevision( out revision ) )
      {
        if ( IncrLog.IsDebugEnabled )
          IncrLog.DebugFormat( "Revision updated to {0}", RevisionGenerator.Revision );

        DataRevision = revision;
        UpdatedPart = updatedPart;
      }
    }

    /// <summary>Only <see cref="RefreshTime"/> is changed, everything else including DataRevision is the same as in <paramref name="oldResult"/>.</summary>
    private RevisedTrackerState
    (
      RevisedTrackerState oldTrackerState
    )
      : base( oldTrackerState.Position, oldTrackerState.Error )
    {
      DataRevision = oldTrackerState.DataRevision;
      UpdatedPart = oldTrackerState.UpdatedPart;
    }

    public override string ToString( )
    {
      return string.Format( "{0} \\ r{1} \\ {2}", base.ToString( ), DataRevision, UpdatedPart );
    }

    /// <summary>
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
    public static RevisedTrackerState Merge( RevisedTrackerState oldResult, LocationLib.TrackerState newResult )
    {
      if ( oldResult == null && newResult == null ) return null;

      if ( oldResult == null && newResult != null )
        return new RevisedTrackerState( newResult.Position, newResult.Error, UpdateReason.BrandNew );

      if ( oldResult != null && newResult == null )
        return new RevisedTrackerState( oldResult );

      LocationLib.Data.Position position;
      if ( oldResult.Position != null && newResult.Position != null )
      {
        if ( oldResult.Position.CurrPoint.ForeignTime > newResult.Position.CurrPoint.ForeignTime )
        { // should be the rare case, but still possible. E.g. it could be like this:
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

      return new RevisedTrackerState( position, error, updatedPart );
    }
  }
}