using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlyTrace.LocationLib
{
  public static class TimeService
  {
    public static Func<DateTime> DebugReplacement;

    public static DateTime Now
    {
      get
      {
        return
          DebugReplacement == null
            ? DateTime.UtcNow
            : DebugReplacement( );
      }
    }
  }
}
