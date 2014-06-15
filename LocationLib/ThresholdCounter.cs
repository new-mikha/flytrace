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

namespace FlyTrace.LocationLib
{
  /// <summary>
  /// A simple thread-safe counter that:
  /// 1. Can increment its value.
  /// 2. Can reset its value to zero.
  /// 3. Has a threshold and can answer the question "is counter larger than threshold"
  /// </summary>
  public class ThresholdCounter
  {
    private int count;

    private readonly object sync = new object( );

    public readonly int Threshold;

    public ThresholdCounter( int threshold )
    {
      Threshold = threshold;
    }

    public int Increment( out  bool isThresholdReached )
    {
      lock ( this.sync )
      {
        this.count++;

        isThresholdReached = this.count >= Threshold;

        return this.count;
      }
    }

    public void Reset( )
    {
      lock ( this.sync )
      {
        this.count = 0;
      }
    }
  }
}