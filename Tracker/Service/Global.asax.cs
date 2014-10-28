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
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web;
using FlyTrace.LocationLib;
using log4net;

namespace FlyTrace.Service
{
  public class Global : HttpApplication
  {
    internal readonly static CultureInfo DefaultCulture = CultureInfo.GetCultureInfo( "en-AU" );

    private readonly static ILog Log = LogManager.GetLogger( "Service.Global" );

    internal static void ConfigureThreadCulture( )
    {
      try
      {
        Thread.CurrentThread.CurrentCulture = DefaultCulture;
        Thread.CurrentThread.CurrentUICulture = DefaultCulture;
      }
      catch ( Exception exc )
      {
        Log.Error( "Can't set culture to the thread", exc );
      }
    }

    private Mutex serviceMutex;

    private readonly DateTime startTime = TimeService.Now;

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
        appAuxLogFolder,
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
      DelayAction( 10000,
        ( ) => Log.InfoFormat( "Service started at {0}", startTime.ToLocalTime( ) )
      );

      // SystemEvents needs message pump, so start it
      new Thread( RunMessagePump ).Start( );
    }

    public static void DelayAction( int milliseconds, Action action )
    {
      // ReSharper disable once ObjectCreationAsStatement
      new Timer(
        unused =>
        {
          ConfigureThreadCulture( );
          action( );
        },
        null,
        milliseconds,
        Timeout.Infinite );
    }

    private Tools.SystemEventsHiddenForm hiddenForm;

    private void RunMessagePump( )
    {
      ILog timeLog = LogManager.GetLogger( "TimeChange" );
      Thread.Sleep( 10000 );
      try
      {
        // Idea taken from http://msdn.microsoft.com/en-us/library/microsoft.win32.systemevents.aspx
        // But the message is catched at a lower level, see form comments for some details

        this.hiddenForm = new Tools.SystemEventsHiddenForm( );
        this.hiddenForm.TimeChanged += hiddenForm_TimeChanged;

        System.Windows.Forms.Application.Run( this.hiddenForm );
        timeLog.Info( "Message pump stopped" );
      }
      catch ( Exception exc )
      {
        // As noted above the messages are not logging right at the service start, so delaying that:
        DelayAction(
          10000,
          ( ) =>
            timeLog.Error( "Can't start message pump", exc ) );
      }
    }

    private void hiddenForm_TimeChanged( object sender, EventArgs e )
    {
      ILog timeLog = LogManager.GetLogger( "TimeChange" );
      timeLog.WarnFormat( "System time has been adjusted, clearing the cache..." );

      try
      {
        MgrService.ClearCache();
        timeLog.InfoFormat( "Cache has been cleared after system time adjusted" );
      }
      catch ( Exception exc )
      {
        const int pauseInSeconds = 60;

        timeLog.ErrorFormat(
          "Can't clear the cache after the time change, will restart the service in {0} seconds. {1}",
          pauseInSeconds,
          exc );

        // Wait before restarting to give time to loggers (including SMTP one) to do their job.
        // But avoid Sleeping for such a long time, because e.g. no guarantee what would happen
        // with the log entry above in case of Sleep(). Or there could be other problems of any 
        // kind due to blocking the thread for 1 minute. So just set up a timer:
        DelayAction(
          pauseInSeconds * 1000,
          HttpRuntime.UnloadAppDomain );
      }
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
      MgrService.Stop();
      

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
      LogManager.Shutdown( );

      if ( this.hiddenForm != null )
        hiddenForm.TimeChanged -= hiddenForm_TimeChanged;

      System.Windows.Forms.Application.Exit( ); // to stop Application.Run that was started earlier
    }
  }
}