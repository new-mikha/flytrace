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

using log4net;

namespace FlyTrace.Service.Internals
{
  /// <summary>
  /// RevisedTrackerState means "TrackerState with Revision" (where Revision is a number like SVN revision)
  /// </summary>
  internal class RevisedTrackerState : LocationLib.TrackerState
  {
    public enum UpdateReason { BrandNew, NewPos, NewErr, NoChange };

    private static readonly ILog IncrLog = LogManager.GetLogger( "TDM.IncrUpd" );

    /// <summary>
    /// Identifies Position and Error data inside, but NOT the object itself. There could be more than one instances 
    /// sharing the same Revision number. Such instances would have same coordinates and error, 
    /// but most probably different RefreshTime value.
    /// </summary>
    public readonly int? DataRevision;

    public readonly UpdateReason UpdatedPart;

    /// <summary>Sets pos, err & revision from parameters, and sets NEW RefreshTime</summary>
    private RevisedTrackerState
    (
      LocationLib.Data.Position position,
      LocationLib.Data.Error error,
      string tag,
      int? revision,
      UpdateReason updatedPart
    )
      : base( position, error, tag )
    {
      DataRevision = revision;
      UpdatedPart = updatedPart;
    }

    public override string ToString( )
    {
      return string.Format( "{0} \\ r{1} \\ {2}", base.ToString( ), DataRevision, UpdatedPart );
    }

    public static RevisedTrackerState Merge(
      RevisedTrackerState oldResult,
      LocationLib.TrackerState newResult,
      int? newRevisionToUse
    )
    {
      if ( oldResult == null && newResult == null ) return null;

      if ( oldResult == null ) // means that newResult is not null (see the check above)
        return new RevisedTrackerState(
          newResult.Position,
          newResult.Error,
          newResult.Tag,
          newRevisionToUse,
          UpdateReason.BrandNew
        );

      if ( newResult == null ) // means that oldResult is not null (see the check above)
        return oldResult;

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
      { // oldResult.Position is null, but newResult.Position could be either null or not null here.
        position = newResult.Position;
      }

      /* We need to ensure that result has at either Position or Error set (or both).
       * 
       * - if position is still null here, then both results Position set to null (see checks above). 
       *   But neither constructor allows both Position and Error to be null => both Errors are not null,
       *   So newResult have Error set, and the merged one will have at least Error set to the new 
       *   result Error (and probably Position).
       *   
       * - if position is not null, then it doesn't matter if newResult.Error is null or not - the merged 
       *   one still have Position set (and probably error - if the new one has it).
       */

      LocationLib.Data.Error error = newResult.Error;

      bool arePositionsEqual = LocationLib.Data.Position.ArePositionsEqual( oldResult.Position, position );
      bool areErrorsEqual = LocationLib.Data.Error.AreErrorsEqual( oldResult.Error, error );

      if ( arePositionsEqual && areErrorsEqual )
        return oldResult;

      UpdateReason updatedPart;
      if ( arePositionsEqual )
        updatedPart = UpdateReason.NewErr;
      else
        updatedPart = UpdateReason.NewPos;

      return new RevisedTrackerState( position, error, newResult.Tag, newRevisionToUse, updatedPart );
    }
  }
}