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
using System.Text;
using System.Globalization;
using System.Web.Security;

namespace FlyTrace
{
  public partial class MapForm : System.Web.UI.Page
  {
    private const string GroupDef = "groupDef";

    private const string GoogleMapDef = "googleMapDef";

    protected int GroupId;

    protected Guid OwnerId;

    protected string GroupName;

    protected double? MapCenterLat;

    protected double? MapCenterLon;

    public static Guid AdvRiderUserId = new Guid( "4CC933D2-5974-4593-9C1C-6027839456FF" );

    protected void Page_Load( object sender, EventArgs e )
    {
      int eventId = 0;
      try
      {
        GroupId = Convert.ToInt32( this.Request.Params["group"] );

        if ( GroupId != 1 ) // 1 is demo group
        {
          TrackerDataSetTableAdapters.GroupTableAdapter groupAdapter =
            new FlyTrace.TrackerDataSetTableAdapters.GroupTableAdapter( );

          TrackerDataSet.GroupDataTable groupTable = groupAdapter.GetDataByGroupId( GroupId );
          if ( groupTable.Count == 0 )
          {
            throw new ApplicationException( );
          }

          TrackerDataSet.GroupRow groupRow = groupTable[0];

          GroupName = groupRow.Name;

          OwnerId = groupRow.UserId;

          TrackerDataSetTableAdapters.ProcsAdapter procsAdapter = new TrackerDataSetTableAdapters.ProcsAdapter( );
          procsAdapter.UpdateGroupViewsNum( GroupId );

          if ( !groupRow.IsEventIdNull( ) )
          {
            eventId = groupRow.EventId;
          }

          if ( !groupRow.IsNewestLatNull( ) && !groupRow.IsNewestLonNull( ) )
          {
            MapCenterLat = groupRow.NewestLat;
            MapCenterLon = groupRow.NewestLon;
          }
        }
      }
      catch
      {
        Response.Redirect( "~/default.aspx", true );
        return;
      }

      string taskArrayDeclaration;

      if ( eventId == 0 )
      {
        taskArrayDeclaration = "null";
      }
      else
      {
        try
        { // task
          TrackerDataSetTableAdapters.TaskWaypointTableAdapter taskWaypointAdapter =
            new FlyTrace.TrackerDataSetTableAdapters.TaskWaypointTableAdapter( );
          TrackerDataSet.TaskWaypointDataTable taskWaypointTable = taskWaypointAdapter.GetDataByEventId( eventId );

          taskArrayDeclaration = "";
          foreach ( TrackerDataSet.TaskWaypointRow row in taskWaypointTable )
          {
            if ( taskArrayDeclaration != "" )
              taskArrayDeclaration += ",";

            taskArrayDeclaration +=
              string.Format(
                CultureInfo.GetCultureInfo( "en-US" ),
                "'{0}',{1},{2},{3}",
                row.Name, row.Lat, row.Lon, row.Radius );
          }
        }
        catch
        {
          taskArrayDeclaration = "null";
          // ignore any error, task is just an additional feature
        }
      }
      Page.ClientScript.RegisterArrayDeclaration( "_task", taskArrayDeclaration );
    }

    protected void LinkButton1_Click( object sender, EventArgs e )
    {
      Response.Clear( );
      FormsAuthentication.SignOut( );
      Response.Redirect( Request.RawUrl, true );
    }
  }
}