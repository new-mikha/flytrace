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
using FlyTrace.LocationLib;

namespace FlyTrace.Service.RequestsSchedule
{
  /// <summary>
  /// Used to obtain TimeSpans between calls to <see cref="GetSpan"/> for a specific category.
  /// <para>Thread safety: NOT THREAD SAFE.</para>
  /// </summary>
  /// <typeparam name="T">Category type</typeparam>
  public class StatTimer<T>
  {
    private readonly Dictionary<T, DateTime> prevTimes = new Dictionary<T, DateTime>( );

    /// <summary>
    /// If it's called for the first time for given category, returns null.
    /// Otherwise returns time span from the previous call for the same category,
    /// (or if <see cref="Reset"/> was called, from that call)
    /// <para>Thread safety: NOT THREAD SAFE.</para>
    /// </summary>
    public TimeSpan? GetSpan( T category, DateTime currTime = default (DateTime) )
    {
      if ( currTime == default( DateTime ) )
        currTime = TimeService.Now;

      TimeSpan? result;

      DateTime prevTime;
      if ( this.prevTimes.TryGetValue( category, out prevTime ) )
      {
        result = currTime - prevTime;
      }
      else
      {
        result = null;
      }

      this.prevTimes[category] = currTime;

      return result;
    }

    /// <summary>
    /// Optional supplement to <see cref="GetSpan"/> method. Can start counting 
    /// from the time of that call.
    /// <para>Thread safety: NOT THREAD SAFE.</para>
    /// </summary>
    public void Reset(T category, DateTime currTime = default (DateTime))
    {
      if ( currTime == default( DateTime ) )
        currTime = TimeService.Now;

      this.prevTimes[category] = currTime;
    }
  }
}