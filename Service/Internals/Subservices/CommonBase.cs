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
using System.Threading;

using log4net;

using FlyTrace.LocationLib;

namespace FlyTrace.Service.Internals.Subservices
{
  // This class serves as a common non-generic base of all subservices. Needed only 
  // because TrackerServiceBase is generic so static fields in there are not shared
  // across TrackerServiceBase's descendants. 
  internal class CommonBase
  {
    private static long CallIdSource;

    public long CallId { get; private set; }

    public CommonBase( )
    {
      CallId = Interlocked.Increment( ref CallIdSource );
    }

    protected static readonly ILog IncrTestLog = LogManager.GetLogger( "IncrTest" );
    protected static readonly ILog InfoLog = LogManager.GetLogger( "InfoLog" );
    protected static readonly ILog IncrLog = LogManager.GetLogger( "TDM.IncrUpd" );
    protected static readonly ILog Log = LogManager.GetLogger( "TDM" );

    private static int SimultaneousCallCount;

    protected static void IncrementCallCount( )
    {
      int callCount = Interlocked.Increment( ref SimultaneousCallCount );

      if ( !Log.IsDebugEnabled ) return;

      if ( callCount > 100 )
      {
        Log.DebugFormat( "Got callCount > 100 , i.e. {0}", callCount );
      }
      else if ( callCount > 10 )
      {
        Log.DebugFormat( "Got callCount > 10 , i.e. {0}", callCount );
      }
      else if ( callCount > 5 )
      {
        Log.DebugFormat( "Got callCount > 5 , i.e. {0}", callCount );
      }
      else if ( callCount > 3 )
      {
        Log.DebugFormat( "Got callCount > 3 , i.e. {0}", callCount );
      }
    }

    /// <summary>
    /// Count of current simultaneous calls.
    /// Value is changed by multiple threads, so use for debug/log purposes only.
    /// </summary>
    public int DebugCallCount
    {
      get
      {
        // Incremental doesn't have an overload of Read for Int32 because it's atomic anyway
        return SimultaneousCallCount;
      }
    }

    protected static void DecrementCallCount( )
    {
      Interlocked.Decrement( ref SimultaneousCallCount );
    }

    public static int CalcAgeInSeconds( DateTime time )
    {
      if ( time == default( DateTime ) )
        return 0;

      TimeSpan locationAge = TimeService.Now - time;
      return Math.Max( 0, ( int ) locationAge.TotalSeconds ); // to fix potential error in this server time settings
    }
  }
}