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
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.IO;
using System.Threading;

using log4net;

namespace FlyTrace.Service
{
  public class Global : System.Web.HttpApplication
  {
    internal static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo( "en-AU" );

    internal static void SetDefaultCultureToThread( )
    {
      Thread.CurrentThread.CurrentCulture = DefaultCulture;
      Thread.CurrentThread.CurrentUICulture = DefaultCulture;
    }


    private readonly DateTime StartTime = DateTime.Now;

    private readonly ILog Log = LogManager.GetLogger( "Service.Global" );

    private Mutex serviceMutex;

    protected void Application_Start( object sender, EventArgs e )
    {
      log4net.Config.XmlConfigurator.Configure( );

      string appAuxLogFolder;
      if ( Properties.Settings.Default.WriteSuccRequestFlagFile )
      {
        appAuxLogFolder = Path.Combine( HttpRuntime.AppDomainAppPath, "logs" );
        Log.InfoFormat( "Use '{0}' as a path for succ flag files", appAuxLogFolder );
      }
      else
      {
        appAuxLogFolder = null;
        Log.InfoFormat( "Succ flag files will not be created." );
      }

      LocationLib.ForeignAccess.ForeignAccessCentral.InitAux( 
        appAuxLogFolder ,
        Properties.Settings.Default.SpotConsequentRequestsErrorCountThresold,
        Properties.Settings.Default.SpotConsequentTimedOutRequestsThresold
      );

      LocationLib.Tools.DefaultCulture = DefaultCulture;

      try
      { // that's a service feauture, so don't stop if it fails
        this.serviceMutex = new Mutex( true, "FlyTraceService" );
      }
      catch ( Exception exc )
      {
        Log.Error( "Failed to created mutex", exc );
      }

      AsyncResultNoResult.DefaultEndWaitTimeout = 60000; // There are no operations that should run longer.

      // It seems that messages are not logging right at the service start, 
      // so delay "start" message to make sure it goes to the log:
      System.Threading.Timer timer =
        new System.Threading.Timer(
          OnStartTimer,
          null,
          10000,
          System.Threading.Timeout.Infinite );
    }

    private void OnStartTimer( object state )
    {
      Log.InfoFormat( "Service started at {0}", StartTime );
    }

    protected void Session_Start( object sender, EventArgs e )
    {

    }

    protected void Application_BeginRequest( object sender, EventArgs e )
    {

    }

    protected void Application_AuthenticateRequest( object sender, EventArgs e )
    {

    }

    protected void Application_Error( object sender, EventArgs e )
    {

    }

    protected void Session_End( object sender, EventArgs e )
    {

    }

    protected void Application_End( object sender, EventArgs e )
    {
      TrackerDataManager.Singleton.Stop( );

      try
      {
        if ( this.serviceMutex != null )
        {
          this.serviceMutex.Close( );
          this.serviceMutex = null;
        }
      }
      catch ( Exception exc )
      {
        Log.Error( "Failed to release mutex", exc );
      }

      // sometimes it's get called, sometimes not.
      Log.Info( "Service stopped." );
      log4net.LogManager.Shutdown( );
    }
  }
}