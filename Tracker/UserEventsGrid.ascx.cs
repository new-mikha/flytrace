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
  public partial class UserEventsGrid : System.Web.UI.UserControl
  {
    protected void Page_Load( object sender , EventArgs e )
    {
      if ( !IsPostBack )
      {
        this.eventsGridView.Sort( "Name" , System.Web.UI.WebControls.SortDirection.Ascending );
      }
    }

    protected void createEventLinkButton_Click( object sender , EventArgs e )
    {
      this.userEventsDataSource.Insert( );
    }

    protected void userEventsDataSource_Inserted( object sender , SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception == null )
      {
        int newEventId = ( int ) e.Command.Parameters[ "@NewEventId" ].Value;
        Response.Redirect( "~/manageEvent.aspx?event=" + newEventId );
      }
    }

    protected void eventsGridView_DataBound( object sender , EventArgs e )
    {
      this.eventsGridView.AllowSorting = this.eventsGridView.Rows.Count > 1;
    }

    protected void eventsGridView_RowDataBound( object sender , GridViewRowEventArgs e )
    {
      if ( e.Row.RowType == DataControlRowType.EmptyDataRow )
      {
        MultiView noEventMultiView = e.Row.FindControl( "noEventMultiView" ) as MultiView;
        if ( noEventMultiView != null )
        {
          TrackerDataSetTableAdapters.GroupTableAdapter groupTableAdapter =
            new TrackerDataSetTableAdapters.GroupTableAdapter( );

          int totalUserGroupsCount = groupTableAdapter.GetUserGroupsCount( Global.UserId ).Value;

          if ( totalUserGroupsCount == 0 )
          {
            noEventMultiView.ActiveViewIndex = 0;
          }
          else
          {
            noEventMultiView.ActiveViewIndex = 1;

            // for some reason it's possible that in case of the empty grid 
            // there were some data and RowType was equal to DataControlRowType.DataRow.
            // So roll back everything we possible did in this case: 
            this.createEventPanel.Visible = false;
            this.defaultAnnotationPanel.Visible = false;
            this.switchToSingleEventModePanel.Visible = false;
          }
        }
      }
      else if ( e.Row.RowType == DataControlRowType.DataRow )
      {
        // !!!!!!!!!!!!!!!!!!
        // See above actions for EmptyDataRow. It's possible that everything that we're 
        // doing here will need a roll back. So if anything is made visible here - hide it 
        // in the case for EmptyDataRow above.
        // !!!!!!!!!!!!!!!!!!
        DataRowView drv = e.Row.DataItem as DataRowView;
        if ( ( bool ) drv[ "IsDefault" ] )
        {
          this.defaultAnnotationPanel.Visible = true;
        }
        this.switchToSingleEventModePanel.Visible = true;
        this.createEventPanel.Visible = true;
      }
    }

    public event EventHandler EventsModelChanged;

    protected void switchToSimpleButton_Click( object sender , EventArgs e )
    {
      Global.IsSimpleEventsModel = true;
      if ( EventsModelChanged != null )
      {
        EventsModelChanged( this , EventArgs.Empty );
      }
    }
  }
}