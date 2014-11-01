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
using System.Web.UI.WebControls;
using System.Data;

namespace FlyTrace
{
  public partial class DefaultPage : System.Web.UI.Page
  {
    protected void Page_Load( object sender, EventArgs e )
    {
      if ( !IsPostBack )
      {
        if ( Global.IsAuthenticated )
        {
          this.othersGroupsPanel.Visible = false;
        }

        this.groupsGridView.Sort( "NewestCoordTs", SortDirection.Descending );
      }
    }

    public object NullableUserId
    {
      get
      {
        if ( Global.IsAuthenticated )
          return Global.UserId;

        return DBNull.Value;
      }
    }

    protected void groupsGridView_DataBound( object sender, EventArgs e )
    {
      this.groupsGridView.AllowSorting = this.groupsGridView.Rows.Count > 1;
    }

    protected void showOtherGroupsButton_click( object sender, EventArgs e )
    {
      this.othersGroupsPanel.Visible = !this.othersGroupsPanel.Visible;
      if ( this.othersGroupsPanel.Visible )
      {
        ( ( Button ) sender ).Text = "Hide Others' Public Groups";
      }
      else
      {
        ( ( Button ) sender ).Text = "Show Others' Public Groups";
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
        if ( drv["NewestCoordTs"] != DBNull.Value )
        {
          DateTime newestCoordTs = ( DateTime ) drv["NewestCoordTs"];
          updateTsLabel.Text = newestCoordTs.ToString( Resources.Resources.AgeFormat ) + " UTC";

          double totalDays;
          ageLabel.Text = LocationLib.Tools.GetAgeStr( newestCoordTs, false, out totalDays );

          agePanel.Visible = totalDays >= 0.0 && totalDays < 1.0;
        }
      }
    }
  }
}