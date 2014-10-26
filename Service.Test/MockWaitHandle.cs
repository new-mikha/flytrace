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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Service.Test
{
  public class MockWaitHandle : WaitHandle
  {
    private readonly int expectedWaitMs;
    private readonly bool waitResult;

    public MockWaitHandle( int expectedWaitMs, bool waitResult )
    {
      this.expectedWaitMs = expectedWaitMs;
      this.waitResult = waitResult;
    }

    public bool IsWaitSucceeded { get; private set; }

    public override bool WaitOne( int millisecondsTimeout )
    {
      Assert.AreEqual( this.expectedWaitMs, millisecondsTimeout );

      IsWaitSucceeded = true;

      return this.waitResult;
    }

    public override bool WaitOne( TimeSpan timeout )
    {
      return WaitOne( ( int ) ( timeout.TotalMilliseconds ) );
    }
  }
}
