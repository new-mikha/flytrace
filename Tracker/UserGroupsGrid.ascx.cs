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
using System.Drawing;

namespace FlyTrace
{
  public partial class UserGroupsGrid : System.Web.UI.UserControl
  {
    protected void Page_Load( object sender, EventArgs e )
    {
      if ( !IsPostBack )
      {
        this.groupsGridView.Sort( "Name", System.Web.UI.WebControls.SortDirection.Ascending );
      }
    }

    protected void createGroupLinkButton_Click( object sender, EventArgs e )
    {
      this.allUserGroupsDataSource.Insert( );
    }

    protected void allUserGroupsDataSource_Inserted( object sender, SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception == null )
      {
        int newGroupId = ( int ) e.Command.Parameters["@NewGroupId"].Value;
        Response.Redirect( "~/manageGroup.aspx?group=" + newGroupId );
      }
    }

    protected void groupsGridView_DataBound( object sender, EventArgs e )
    {
      this.groupsGridView.AllowSorting = this.groupsGridView.Rows.Count > 1;
      this.createGroupLinkPanel.Visible = this.groupsGridView.Rows.Count > 0;
    }

    private readonly DateTime utcNow = DateTime.Now.ToUniversalTime( );

    public GridView Grid { get { return this.groupsGridView; } }

    protected void allUserGroupsDataSource_Deleting( object sender, SqlDataSourceCommandEventArgs e )
    {
      this.emptyTaskNotePanel.Visible = false;
    }

    private bool? shouldShowTasksColumn;

    protected bool ShouldShowTasksColumn
    {
      get
      {
        if ( !this.shouldShowTasksColumn.HasValue )
        { // following Global methods are a bit expensive. So cache the result:
          this.shouldShowTasksColumn =
            !Global.IsSimpleEventsModel || Global.DefEventLoadedWptsCount > 0;
        }

        return this.shouldShowTasksColumn.Value;
      }
    }

    protected void groupsGridView_RowDataBound( object sender, GridViewRowEventArgs e )
    {
      if ( e.Row.RowType == DataControlRowType.DataRow )
      {
        Label updateTsLabel = ( Label ) e.Row.FindControl( "updateTsLabel" );
        Label ageLabel = ( Label ) e.Row.FindControl( "ageLabel" );
        Panel agePanel = ( Panel ) e.Row.FindControl( "agePanel" );

        DataRowView drv = ( DataRowView ) e.Row.DataItem;

        if ( ShouldShowTasksColumn &&
             drv["EventId"] != System.DBNull.Value &&
             ( int ) drv["TaskWptCount"] < 2 )
        {
          this.emptyTaskNotePanel.Visible = true;
        }

        if ( drv["NewestCoordTs"] != DBNull.Value )
        {
          DateTime newestCoordTs = ( DateTime ) drv["NewestCoordTs"];
          updateTsLabel.Text = newestCoordTs.ToString( "ddd d MMM H:mm UTC" );
          if ( utcNow > newestCoordTs )
          {
            TimeSpan timeSpan = utcNow - newestCoordTs;
            if ( timeSpan.TotalDays < 1.0 )
            {
              agePanel.Visible = true;
              if ( timeSpan.Days > 0 )
              {
                ageLabel.Text += timeSpan.Days.ToString( ) + " d ";
              }

              if ( timeSpan.Hours > 0 || timeSpan.Days > 0 )
              {
                ageLabel.Text += timeSpan.Hours.ToString( ) + " hr ";
              }

              ageLabel.Text += timeSpan.Minutes.ToString( ) + " min ago";
            }
          }
        }
      }
    }
  }
}