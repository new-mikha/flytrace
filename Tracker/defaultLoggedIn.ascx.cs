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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;
using System.Data.SqlClient;
using System.Data;
using System.Threading;
using System.Drawing;

namespace FlyTrace
{
  public partial class defaultLoggedIn : System.Web.UI.UserControl
  {
    protected void Page_Load( object sender, EventArgs e )
    {
    }

    protected void Page_PreRender( object sender, EventArgs e )
    {
      if ( Global.IsAdmin )
      {
        ShowServiceStatus( );
      }
    }

    protected void SignOutLinkButton_Click( object sender, EventArgs e )
    {
      Response.Clear( );
      FormsAuthentication.SignOut( );
      Response.Redirect( Request.RawUrl, true );
    }

    protected void events_EventsModelChanged( object sender, EventArgs e )
    {
      if ( Global.IsSimpleEventsModel )
      {
        this.eventsMultiView.SetActiveView( this.todayTaskView );
        this.userTodayTask.DataBind( );
      }
      else
      {
        this.eventsMultiView.SetActiveView( this.eventsGridView );
        this.userEventsGrid.DataBind( );
      }
    }

    protected string GetAdminsList( )
    {
      string[] admins = Roles.GetUsersInRole( Global.AdminRole );
      string result = "";
      for ( int i = 0; i < admins.Length; i++ )
      {
        if ( i > 0 )
          result += "&nbsp;/&nbsp;";

        result += string.Format( "<a href='Administration/userInformation.aspx?user={0}'>{0}</a>", admins[i] );
      }

      return result;
    }

    private readonly static Color okColor = Color.FromArgb( 0x00, 0xCC, 0x00 );
    private readonly static Color warnColor = Color.FromArgb( 0xFF, 0x99, 0x33 );
    private readonly static Color errColor = Color.Red;

    protected void ShowServiceStatus( )
    {
      try
      {
        Service.AdminStat adminStat = Service.ServiceFacade.GetAdminStatBrief( );

        ServiceStatToDisplayMode( adminStat.ForeignSourcesStat );

        ShowAdminStatMessages( adminStat.Messages );

        AddAdminMessageRow( "Current revision", adminStat.CurrentRevision.ToString( ) );
        AddAdminMessageRow( "GetCoords calls", adminStat.CoordAccessCount.ToString( ) );
        AddAdminMessageRow( "Start time (UTC)", adminStat.StartTime.ToString( ) + " UTC" );
        AddAdminMessageRow( "Uptime", FlyTrace.LocationLib.Tools.GetAgeStr( adminStat.StartTime, false ) );
      }
      catch ( Exception exc )
      {
        this.serviceStatShortStatus.Visible = true;
        this.serviceStatShortStatus.Text = exc.Message;
        this.serviceStatShortStatus.ForeColor = errColor;
      }
    }

    private void ShowAdminStatMessages( Service.AdminMessage[] adminMessages )
    {
      foreach ( Service.AdminMessage adminMessage in adminMessages )
      {
        AddAdminMessageRow( adminMessage.Key, adminMessage.Message );
      }
    }

    private void AddAdminMessageRow( string key, string message )
    {
      TableRow row = new TableRow( );

      {
        TableCell keyCell = new TableCell( );
        keyCell.Text = key + " :";
        row.Cells.Add( keyCell );
      }

      {
        TableCell messageCell = new TableCell( );
        messageCell.Text = message;

        row.Cells.Add( messageCell );
      }

      if ( ( message + "_" + key ).ToLower( ).Contains( "error" ) )
        row.ForeColor = errColor;
      else if ( ( message + "_" + key ).ToLower( ).Contains( "warning" ) )
        row.ForeColor = warnColor;

      this.adminStatMessagesTable.Rows.Add( row );
    }

    private void ServiceStatToDisplayMode( Service.ForeignSourceStat[] foreignSourcesStat )
    {
      foreach ( Service.ForeignSourceStat stat in foreignSourcesStat )
      {
        List<string> lines = new List<string>( );
        lines.Add( stat.Name );
        if ( !string.IsNullOrEmpty( stat.Stat ) )
          lines.AddRange( stat.Stat.Split( '\n' ) );

        foreach ( string line in lines )
        {
          TableRow row = new TableRow( );

          int iTab = line.IndexOf( '\t' );
          {
            TableCell feedNameCell = new TableCell( );

            if ( iTab >= 0 )
            {
              feedNameCell.Text = line.Remove( iTab );
              feedNameCell.HorizontalAlign = HorizontalAlign.Right;
            }
            else
            {
              feedNameCell.Text = line;
              feedNameCell.ColumnSpan = 2;
            }

            if ( !stat.IsOk )
              feedNameCell.ForeColor = Color.Gray;
            row.Cells.Add( feedNameCell );
          }

          if ( iTab >= 0 )
          {
            TableCell feedTimeCell = new TableCell( );

            feedTimeCell.HorizontalAlign = HorizontalAlign.Right;
            feedTimeCell.Text =
              ( iTab < line.Length - 1 )
              ? line.Substring( iTab + 1 )
              : "None";

            row.Cells.Add( feedTimeCell );
          }

          this.serviceStatDisplayTable.Rows.Add( row );
        }
      }
    }
  }
}