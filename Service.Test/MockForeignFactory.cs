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
using FlyTrace.LocationLib.ForeignAccess;

namespace Service.Test
{
  public class MockForeignFactory : LocationRequestFactory
  {
    public int MaxCallsInPackCount;
    public int MinTimeFromPrevStartMs;
    public int MinCallsGapMs;
    public double? SameFeedHitIntervalMinutes;
    public double? SameFeedHitIntervalSeconds;

    public override LocationRequest CreateRequest( string foreignId )
    {
      throw new NotImplementedException( );
    }

    public override LocationRequest CreateTestRequest( string foreignId, string testSource )
    {
      throw new NotImplementedException( );
    }

    public override string GetStat( out bool isOk )
    {
      throw new NotImplementedException( );
    }

    public override int MaxCallsInPack
    {
      get { return MaxCallsInPackCount; }
    }

    public override TimeSpan MinTimeFromPrevStart
    {
      get { return TimeSpan.FromMilliseconds( MinTimeFromPrevStartMs ); }
    }

    public override TimeSpan MinCallsGap
    {
      get { return TimeSpan.FromMilliseconds( MinCallsGapMs ); }
    }

    public override TimeSpan SameFeedHitInterval
    {
      get
      {
        if ( SameFeedHitIntervalMinutes.HasValue )
          return TimeSpan.FromMinutes( SameFeedHitIntervalMinutes.Value );

        if ( SameFeedHitIntervalSeconds.HasValue )
          return TimeSpan.FromSeconds( SameFeedHitIntervalSeconds.Value );

        throw new InvalidOperationException( "Neither parameter for SameFeedHitInterval has set." );

      }
    }
  }
}
