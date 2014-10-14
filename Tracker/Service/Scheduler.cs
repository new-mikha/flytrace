using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlyTrace.Service
{
  public class Scheduler
  {
    public static TrackerStateHolder GetMoreStaleTracker( TrackerStateHolder h1, TrackerStateHolder h2 )
    {
      bool unused;
      return GetMoreStaleTracker( h1, h2, out unused );
    }

    /// <summary>
    /// - Holders with ScheduledTime set are always less stale than others, because it's trackers
    ///   which were scheduled but for some reasons (error?) didn't started requests. So avoid 
    ///   scheduling those trackers on and on again (failing to request due to the same error) 
    ///   while there are others waiting.
    /// - Holder without RefreshTime is always more stale than a holder with a RefreshTime. I.e.
    ///   trackers that just requested by clients should be refreshed first.
    /// 
    /// * From two holders without ScheduledTime and without RefreshTime, more stale is one with 
    ///   earlier <see cref="TrackerStateHolder.AddedTime"/>.
    /// * From two holders without ScheduledTime and with RefreshTime, more stale is one with 
    ///   earlier <see cref="TrackerStateHolder.RefreshTime"/>.
    /// * From two holders with ScheduledTime, more stale is one with earlier ScheduledTime.
    /// </summary>
    public static TrackerStateHolder GetMoreStaleTracker(
      TrackerStateHolder x,
      TrackerStateHolder y,
      out bool areEqual
      )
    {
      DateTime t1, t2;

      if ( x.ScheduledTime != null && y.ScheduledTime != null )
      {
        t1 = x.ScheduledTime.Value;
        t2 = y.ScheduledTime.Value;
      }
      else if ( x.ScheduledTime != null ) // means "and y.ScheduledTime == null"
      {
        areEqual = false;
        return y;
      }
      else if ( y.ScheduledTime != null ) // means "and x.ScheduledTime == null"
      {
        areEqual = false;
        return x;
      }
      // at this point x.ScheduledTime == null && y.ScheduledTime == null )
      else if ( x.RefreshTime != null && y.RefreshTime != null )
      {
        t1 = x.RefreshTime.Value;
        t2 = y.RefreshTime.Value;
      }
      else if ( x.RefreshTime != null ) // means "and y.RefreshTime == null"
      {
        areEqual = false;
        return y;
      }
      else if ( y.RefreshTime != null ) // means "and x.RefreshTime == null"
      {
        areEqual = false;
        return x;
      }
      else
      {
        // at this point x.ScheduledTime == null && y.ScheduledTime == null 
        //            && x.RefreshTime == null && y.RefreshTime == null
        t1 = x.AddedTime;
        t2 = y.AddedTime;
      }

      areEqual = t1 == t2;

      return t1 < t2 ? x : y;
    }
  }
}