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

namespace FlyTrace.LocationLib.ForeignAccess.Test
{
  public class TestLocationRequestFactory : LocationRequestFactory
  {
    public override LocationRequest CreateRequest( string foreignId )
    {
      string testXml = TestSource.Singleton.GetFeed( foreignId );

      return new TestLocationRequest( foreignId, testXml );
    }

    public override string GetStat( out bool isOk )
    {
      isOk = true;
      return "test stat";
    }

    public override int MaxCallsInPack
    {
      get { return 1; }
    }

    public override TimeSpan MinTimeFromPrevStart
    {
      get { return TimeSpan.Zero; }
    }

    public override TimeSpan MinCallsGap
    {
      get { return TimeSpan.FromSeconds( 5 ); }
    }

    public override TimeSpan SameFeedHitInterval
    {
      get { return TimeSpan.Zero; }
    }
  }
}