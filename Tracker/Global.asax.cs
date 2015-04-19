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
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using log4net;
using System.Threading;

namespace FlyTrace
{
  public enum CoordFormat { Deg, DegMin, DegMinSec }

  public class Global : System.Web.HttpApplication
  {
    public const string UserIdSessionKey = "UserId";

    private const string UserNameSessionKey = "UserName";

    public const string AdminRole = "Admins";

    public static bool IsAdmin
    {
      get { return Roles.IsUserInRole( AdminRole ); }
    }

    public const string SpotIdReaderRole = "SpotIdReaders";

    public static bool IsSpotIdReader
    {
      get { return Roles.IsUserInRole( SpotIdReaderRole ); }
    }

    public static readonly CultureInfo EnUsCulture = CultureInfo.GetCultureInfo( "en-US" );

    /// <summary>
    /// We need to make sure that whenever server we run, a standard double representation is parsed correctly.
    /// E.g. initial version were running on French server => Double.Parse required comma instead of dot.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static double ToDouble( string str )
    {
      return Double.Parse( str, EnUsCulture );
    }

    public static bool IsAuthenticated
    {
      get
      {
        return
          HttpContext.Current != null &&
          HttpContext.Current.Session != null &&
          HttpContext.Current.Session[UserIdSessionKey] is Guid;
      }
    }

    public static Guid UserId
    {
      get
      {
        object value = null;

        if ( HttpContext.Current != null &&
             HttpContext.Current.Session != null )
        {
          value = HttpContext.Current.Session[UserIdSessionKey];
        }

        if ( value is Guid )
          return ( Guid ) value;

        throw new InvalidOperationException( "Current session is not authenticated" );
      }
    }

    private class UserProfileData
    {
      public bool IsSimpleEventsModel;

      public CoordFormat CoordFormat;

      public char DefHemisphereNS;

      public char DefHemisphereEW;

      public bool ShowUserMessagesByDefault;

      public bool UserMessagesSettingIsNew;
    }

    private static Dictionary<Guid, UserProfileData> UserProfiles = new Dictionary<Guid, UserProfileData>( );

    private static UserProfileData GetUserProfile( )
    {
      UserProfileData userProfile;

      bool isReady = false;
      lock ( UserProfiles )
      {
        isReady = UserProfiles.TryGetValue( UserId, out userProfile );
      }

      if ( !isReady )
      {
        TrackerDataSetTableAdapters.ProcsAdapter procAdapters =
          new TrackerDataSetTableAdapters.ProcsAdapter( );
        int? notUsed = null;

        bool? tempEventsModel = null;
        bool? showUserMessagesByDefault = null;
        bool? userMessagesSettingIsNew = null;
        string tempCoordFormat = null;
        string defHemisphereNS = null;
        string defHemisphereEW = null;

        procAdapters.GetUserProfile(
          UserId,
          ref notUsed,
          ref tempEventsModel,
          ref tempCoordFormat,
          ref defHemisphereNS,
          ref defHemisphereEW,
          ref showUserMessagesByDefault,
          ref userMessagesSettingIsNew
        );

        userProfile = new UserProfileData( );
        userProfile.IsSimpleEventsModel = tempEventsModel.Value;
        userProfile.CoordFormat = ( CoordFormat ) Enum.Parse( typeof( CoordFormat ), tempCoordFormat );
        userProfile.DefHemisphereNS = defHemisphereNS[0];
        userProfile.DefHemisphereEW = defHemisphereEW[0];
        userProfile.ShowUserMessagesByDefault = showUserMessagesByDefault.Value;
        userProfile.UserMessagesSettingIsNew = userMessagesSettingIsNew.Value;

        lock ( UserProfiles )
        {
          UserProfileData otherValues;
          if ( UserProfiles.TryGetValue( UserId, out otherValues ) )
          { // someone else added the value while we've been on DB call
            userProfile = otherValues;
          }
          else
          {
            UserProfiles.Add( UserId, userProfile );
          }
        }
      }

      return userProfile;
    }

    public static bool IsSimpleEventsModel
    {
      get
      {
        UserProfileData userProfile = GetUserProfile( );

        return userProfile.IsSimpleEventsModel;
      }

      set
      {
        {
          TrackerDataSetTableAdapters.ProcsAdapter procAdapters = new FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter( );
          procAdapters.SetEventsModel( UserId, value );
        }

        lock ( UserProfiles )
        {
          UserProfileData existingProfile;
          if ( UserProfiles.TryGetValue( UserId, out existingProfile ) )
            existingProfile.IsSimpleEventsModel = value;
        }
      }
    }

    public static bool ShowUserMessagesByDefault
    {
      get
      {
        UserProfileData userProfile = GetUserProfile( );

        return userProfile.ShowUserMessagesByDefault;
      }

      set
      {
        {
          TrackerDataSetTableAdapters.ProcsAdapter procAdapters = new FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter( );
          procAdapters.SetUserMessagesFlag( UserId, value );
        }

        lock ( UserProfiles )
        {
          UserProfileData existingProfile;
          if ( UserProfiles.TryGetValue( UserId, out existingProfile ) )
            existingProfile.ShowUserMessagesByDefault = value;
        }
      }
    }

    public static bool UserMessagesSettingIsNew
    {
      get
      {
        UserProfileData userProfile = GetUserProfile( );

        return userProfile.UserMessagesSettingIsNew;
      }

      set
      {
        {
          TrackerDataSetTableAdapters.ProcsAdapter procAdapters = new FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter( );
          procAdapters.SetUserMessagesSettingIsNewFlag( UserId, value );
        }

        lock ( UserProfiles )
        {
          UserProfileData existingProfile;
          if ( UserProfiles.TryGetValue( UserId, out existingProfile ) )
            existingProfile.UserMessagesSettingIsNew = value;
        }
      }
    }

    public static CoordFormat CoordFormat
    {
      get
      {
        UserProfileData userProfile = GetUserProfile( );

        return userProfile.CoordFormat;
      }

      set
      {
        {
          TrackerDataSetTableAdapters.ProcsAdapter procAdapters = new FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter( );
          procAdapters.SetCoordFormat( UserId, value.ToString( ) );
        }

        lock ( UserProfiles )
        {
          UserProfileData existingProfile;
          if ( UserProfiles.TryGetValue( UserId, out existingProfile ) )
            existingProfile.CoordFormat = value;
        }
      }
    }

    /// <summary>
    /// Gets default north/shouth hemisphere. Use <see cref="SetDefHimspheres"/> to set this value.
    /// </summary>
    public static char DefHemisphereNS
    {
      get
      {
        UserProfileData userProfile = GetUserProfile( );

        return userProfile.DefHemisphereNS;
      }
    }

    /// <summary>
    /// Gets default east/west hemisphere. Use <see cref="SetDefHimspheres"/> to set this value.
    /// </summary>
    public static char DefHemisphereEW
    {
      get
      {
        UserProfileData userProfile = GetUserProfile( );

        return userProfile.DefHemisphereEW;
      }
    }

    public static void SetDefHemispheres( double lat, double lon )
    {
      char defHemisphereNS = lat > 0 ? 'N' : 'S';
      char defHemisphereEW = lon > 0 ? 'E' : 'W';

      {
        TrackerDataSetTableAdapters.ProcsAdapter procAdapters = new FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter( );
        procAdapters.SetDefHemispheres(
          UserId,
          defHemisphereNS.ToString( ),
          defHemisphereEW.ToString( )
        );
      }

      lock ( UserProfiles )
      {
        UserProfileData existingProfile;
        if ( UserProfiles.TryGetValue( UserId, out existingProfile ) )
        {
          existingProfile.DefHemisphereNS = lat > 0 ? 'N' : 'S';
          existingProfile.DefHemisphereEW = lon > 0 ? 'E' : 'W';
        }
      }
    }

    private class DefEventStat
    {
      public int LoadedWptsCount;
      public int TaskWptsCount;
    }

    private static DefEventStat GetDefEventStat( )
    {
      DefEventStat defEventStat = HttpContext.Current.Items["DefEventStat"] as DefEventStat;

      if ( defEventStat == null )
      {
        int? loadedWptsCount = null;
        int? taskWptsCount = null;
        int? defEventIdUnused = null;

        TrackerDataSetTableAdapters.ProcsAdapter procsAdapter =
          new TrackerDataSetTableAdapters.ProcsAdapter( );
        procsAdapter.GetDefaultEventParams( Global.UserId, ref defEventIdUnused, ref loadedWptsCount, ref taskWptsCount );

        defEventStat = new DefEventStat( );
        defEventStat.LoadedWptsCount = loadedWptsCount.HasValue ? loadedWptsCount.Value : 0;
        defEventStat.TaskWptsCount = taskWptsCount.HasValue ? taskWptsCount.Value : 0;

        HttpContext.Current.Items["DefEventStat"] = defEventStat;
      }

      return defEventStat;
    }

    public static int DefEventLoadedWptsCount
    {
      get
      {
        DefEventStat defEventStat = GetDefEventStat( );

        return defEventStat.LoadedWptsCount;
      }
    }

    public static int DefEventTaskWptsCount
    {
      get
      {
        DefEventStat defEventStat = GetDefEventStat( );

        return defEventStat.TaskWptsCount;
      }
    }


    private void Application_PostAcquireRequestState( object sender, EventArgs e )
    {
      if ( HttpContext.Current.Session == null )
        return;

      if ( User == null || User.Identity == null || string.IsNullOrEmpty( User.Identity.Name ) )
      {
        Session.Remove( UserIdSessionKey );
        Session.Remove( UserNameSessionKey );
      }
      else
      {
        string userName = User.Identity.Name;

        if ( Session[UserNameSessionKey] == null ||
             StringComparer.InvariantCultureIgnoreCase.Compare( Session[UserNameSessionKey].ToString( ), userName ) != 0 )
        {
          Session.Remove( UserIdSessionKey );
          Session.Remove( UserNameSessionKey );

          using ( SqlConnection connection = new SqlConnection( ) )
          {
            connection.ConnectionString =
              ConfigurationManager.ConnectionStrings["TrackerConnectionString"].ConnectionString;
            connection.Open( );

            SqlCommand sqlCmd = new SqlCommand( "EXEC aspnet_Membership_GetUserByName 'Tracker', @UserName, @CurrentTimeUtc, 1", connection );

            sqlCmd.Parameters.Add( "@UserName", System.Data.SqlDbType.NVarChar, 256 );
            sqlCmd.Parameters["@UserName"].Value = userName;

            sqlCmd.Parameters.Add( "@CurrentTimeUtc", System.Data.SqlDbType.DateTime );
            sqlCmd.Parameters["@CurrentTimeUtc"].Value = DateTime.UtcNow;

            using ( SqlDataReader reader = sqlCmd.ExecuteReader( ) )
            {
              object userId = null;

              if ( reader.HasRows )
              {
                reader.Read( );
                userId = reader["UserId"];
              }

              if ( userId == null )
              {
                FormsAuthentication.SignOut( );
                this.Response.Redirect( "~/default.aspx" );
              }
              else
              {
                Session[UserIdSessionKey] = userId;
                Session[UserNameSessionKey] = userName;
              }
            }
          }
        }
      }
    }

    internal readonly static CultureInfo DefaultCulture = CultureInfo.GetCultureInfo( "en-AU" );

    private readonly ILog InfoLog = LogManager.GetLogger( "InfoLog" );

    protected void Application_Start( object sender, EventArgs e )
    {
      LocationLib.Tools.ConfigureLog4Net( HttpRuntime.AppDomainAppPath );

      try
      {
        LocationLib.Tools.DefaultCulture = DefaultCulture;
        LocationLib.Tools.ConfigureThreadCulture( );

        string dataFolderPath = System.Web.Hosting.HostingEnvironment.MapPath( @"~/App_Data/" );
        Service.ServiceFacade.Init( dataFolderPath );

        // SystemEvents needs message pump, so start it
        new Thread( RunMessagePump ).Start( );

        InfoLog.InfoFormat( "Application started." );
      }
      catch ( Exception exc )
      {
        InfoLog.Error( "Cannot start the service", exc );
        throw;
      }
    }

    public static void DelayAction( int milliseconds, Action action )
    {
      // ReSharper disable once ObjectCreationAsStatement
      new Timer(
        unused =>
        {
          LocationLib.Tools.ConfigureThreadCulture( );
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
        // The messages are could be not logged right at the service start, so delaying that:
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
        Service.ServiceFacade.ResetTrackersCache( );
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

    internal static bool IsSqlDuplicateError( Exception e )
    {
      if ( e is SqlException )
      {
        SqlException sqlException = ( e as SqlException );
        foreach ( SqlError sqlError in sqlException.Errors )
        {
          if ( sqlError.Number == 2601 )
          {
            return true;
          }
        }
      }

      return false;
    }

    protected void Application_End( object sender, EventArgs e )
    {
      // sometimes it got to be called, sometimes not.
      
      try
      {
        Service.ServiceFacade.Deinit( );

        if ( this.hiddenForm != null )
          hiddenForm.TimeChanged -= hiddenForm_TimeChanged;

        System.Windows.Forms.Application.Exit( ); // to stop Application.Run that was started earlier

        InfoLog.Info( "Application stopped." );
      }
      catch ( Exception exc )
      {
        InfoLog.Error( "Cannot shutdown application properly.", exc );
      }

      LogManager.Shutdown( );
    }
  }
}