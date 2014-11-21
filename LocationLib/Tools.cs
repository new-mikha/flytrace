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
using System.Configuration;
using System.Globalization;
using System.Threading;

using log4net;
using System.IO;


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
    public static CultureInfo DefaultCulture = CultureInfo.GetCultureInfo( "en-AU" );

    internal static void ConfigureThreadCulture( )
    {
      try
      {
        Thread.CurrentThread.CurrentCulture = DefaultCulture;
        Thread.CurrentThread.CurrentUICulture = DefaultCulture;
      }
      catch ( Exception exc )
      {
        LogManager.GetLogger( typeof( Tools ) ).Error( "Can't set culture to the thread", exc );
      }
    }

    public static string GetAgeStr( DateTime dateTime, bool withSec )
    {
      double unusedTotalDays;

      return GetAgeStr( dateTime, withSec, out unusedTotalDays );
    }

    public static string GetAgeStr( DateTime dateTime, bool withSec, out double totalDays )
    {
      DateTime utcNow = TimeService.Now;

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

    public static void ConfigureLog4Net( string appRootFolder )
    {
      string errorToLog = null;

      try
      {
        string prevCurrDir = Directory.GetCurrentDirectory( );
        Directory.SetCurrentDirectory( appRootFolder );

        try
        {
          try
          {
            const string logFolderSettingName = "log_folder";
            string logFolderRelativePath = ConfigurationManager.AppSettings[logFolderSettingName];
            string fullLogRootPath = Path.GetFullPath( logFolderRelativePath );

            if ( !fullLogRootPath.EndsWith( @"\" ) &&
                 !fullLogRootPath.EndsWith( @"/" ) )
            {
              fullLogRootPath = fullLogRootPath + @"\";
            }

            Environment.SetEnvironmentVariable( logFolderSettingName, fullLogRootPath );
          }
          catch ( Exception exc )
          {
            errorToLog = "Can't set up log root folder: " + exc.Message;
          }

          {
            string configFilePath = ConfigurationManager.AppSettings["LogConfig"];
            if ( string.IsNullOrEmpty( configFilePath ) )
            { // try to fall back to normal web.config:
              log4net.Config.XmlConfigurator.Configure( );
            }
            else
            {
              log4net.Config.XmlConfigurator.ConfigureAndWatch( new FileInfo( configFilePath ) );
            }
          }
        }
        finally
        {
          Directory.SetCurrentDirectory( prevCurrDir );
        }
      }
      catch ( Exception exc )
      {
        log4net.Config.XmlConfigurator.Configure( );
        LogManager.GetLogger( typeof( Tools ) ).Error( exc );
      }

      if ( errorToLog != null )
        LogManager.GetLogger( typeof( Tools ) ).Error( errorToLog );
    }
  }
}