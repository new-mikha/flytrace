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
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;

namespace FlyTrace
{
  public partial class UserTodayTask : System.Web.UI.UserControl
  {
    protected void Page_PreRender( object sender, EventArgs e )
    {
      if ( Global.DefEventLoadedWptsCount == 0 )
      {
        TrackerDataSetTableAdapters.GroupTableAdapter groupTableAdapter =
          new TrackerDataSetTableAdapters.GroupTableAdapter( );

        int totalUserGroupsCount = groupTableAdapter.GetUserGroupsCount( Global.UserId ).Value;

        if ( totalUserGroupsCount == 0 )
        {
          this.waypointsMultiView.SetActiveView( this.noGroupAtAll );
        }
        else
        {
          this.waypointsMultiView.SetActiveView( this.waypointsNotLoadedView );
        }
        this.taskMultiView.Visible = false;
      }
      else
      {
        this.taskMultiView.Visible = true;
        this.waypointsMultiView.SetActiveView( this.waypointsLoadedView );
        if ( Global.DefEventTaskWptsCount == 0 )
        {
          this.taskMultiView.SetActiveView( this.taskNotSetView );
        }
        else
        {
          this.taskMultiView.SetActiveView( this.taskSetupView );
        }

        this.oneWptWarningLabel.Visible = Global.DefEventTaskWptsCount == 1;
      }
    }

    protected void userEventsDataSource_Inserted( object sender, SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception == null )
      {
        int newEventId = ( int ) e.Command.Parameters["@NewEventId"].Value;
        Response.Redirect( "~/manageEvent.aspx?event=" + newEventId );
      }
    }

    protected void editTaskButton_Click( object sender, EventArgs e )
    {
      Response.Redirect( "manageEvent.aspx?event=" + GetDefaultEventId( ), true );
    }

    protected void editWptsButton_Click( object sender, EventArgs e )
    {
      Response.Redirect( "manageWaypoints.aspx?event=" + GetDefaultEventId( ), true );
    }

    private static int GetDefaultEventId( )
    {
      TrackerDataSetTableAdapters.ProcsAdapter procsAdapter = new TrackerDataSetTableAdapters.ProcsAdapter( );
      int? defaultEventId = null;
      procsAdapter.EnsureDefaultTask( Global.UserId, ref defaultEventId );

      return defaultEventId.Value;
    }

    public event EventHandler EventsModelChanged;

    protected void switchToAdvButton_Click( object sender, EventArgs e )
    {
      Global.IsSimpleEventsModel = false;
      if ( EventsModelChanged != null )
      {
        EventsModelChanged( this, EventArgs.Empty );
      }
    }
  }
}