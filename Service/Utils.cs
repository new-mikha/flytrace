using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlyTrace.Service.Properties;
using log4net;

namespace FlyTrace.Service
{
  public class Utils
  {
    private static ILog Log = LogManager.GetLogger(typeof(Utils));

    public static DateTime ToAdminTimezone(DateTime time)
    {
      try
      {
        DateTime destTime =
          TimeZoneInfo.ConvertTimeBySystemTimeZoneId(time, Settings.Default.AdminTimezone);

        return destTime;
      }
      catch (Exception exc)
      {
        Log.Warn("Can't convert to the admin time zone", exc);
        return time;
      }
    }
  }
}
