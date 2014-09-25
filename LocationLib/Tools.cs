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
using System.Globalization;
using System.Threading;

namespace FlyTrace.LocationLib
{
  public static class Tools
  {
    /// <summary>
    /// Historically our hosting located in France hence without special care for the culture
    /// errors can look like "La reference d'objet n'est pas definie a une instance d'un objet". 
    /// This field is initially set to the system culture, but the caller can (and does) set it 
    /// to some English culture.
    /// </summary>
    private static CultureInfo defaultCulture = CultureInfo.CurrentCulture;

    public static CultureInfo DefaultCulture
    {
      get { return defaultCulture; }
      set { defaultCulture = value; }
    }

    internal static void SetUpThreadCulture( )
    {
      Thread.CurrentThread.CurrentCulture = DefaultCulture;
      Thread.CurrentThread.CurrentUICulture = DefaultCulture;
    }

    public static string GetAgeStr( DateTime dateTime, bool withSec = true )
    {
      double unusedTotalDays;

      return GetAgeStr( dateTime, withSec, out unusedTotalDays );
    }

    public static string GetAgeStr( DateTime dateTime, bool withSec, out double totalDays )
    {
      DateTime utcNow = DateTime.UtcNow;

      DateTime endTime;
      DateTime startTime;
      string addOn;
      int sign;
      if ( utcNow >= dateTime )
      {
        startTime = dateTime;
        endTime = utcNow;
        addOn = "ago";
        sign = 1;
      }
      else
      {
        startTime = utcNow;
        endTime = dateTime;
        addOn = " ahead";
        sign = -1;
      }

      TimeSpan timeSpan = endTime - startTime;
      totalDays = timeSpan.TotalDays * sign;

      string result = "";
      if ( timeSpan.Days > 0 )
      {
        result += timeSpan.Days.ToString( ) + " d ";
      }

      if ( timeSpan.Hours > 0 || timeSpan.Days > 0 )
      {
        result += timeSpan.Hours.ToString( ) + " hr ";
      }

      if ( !withSec )
      {
        result += timeSpan.Minutes.ToString( ) + " min";
      }
      else
      {
        if ( timeSpan.Minutes > 0 || timeSpan.Hours > 0 || timeSpan.Days > 0 )
        {
          result += timeSpan.Minutes.ToString( ) + " min ";
        }

        result += timeSpan.Seconds.ToString( ) + " s";
      }

      result += " " + addOn;

      return result;
    }
  }
}