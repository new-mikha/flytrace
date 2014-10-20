using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
