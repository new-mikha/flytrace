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
using System.Web.Security;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using FlyTrace.CoordControls;

namespace FlyTrace
{
  public partial class ManageWaypointsForm : System.Web.UI.Page
  {
    // need to be a public property because accessed by EvalParameter - it can't 
    // access non-public stuff, and requires a property rather than a field.
    public int EventId { get; set; }

    protected string EventName;
    protected string LoadWaypointsMsg;
    protected string UpdateWaypointsMsg;
    protected string InsertWaypointMsg;

    protected void Page_PreInit( object sender , EventArgs e )
    {
      string strEventId = Request.QueryString[ "event" ];
      int eventId;

      TrackerDataSet.EventRow eventRow = null;

      if ( strEventId != null && int.TryParse( strEventId , out eventId ) )
      {
        // To make sure that there is no hacking, check that the event 
        // belongs to the current user before setting this.EventId property:
        if ( Global.IsAuthenticated )
        {
          TrackerDataSetTableAdapters.EventTableAdapter eventAdapter = new FlyTrace.TrackerDataSetTableAdapters.EventTableAdapter( );
          TrackerDataSet.EventDataTable eventTable = eventAdapter.GetDataByEventId( eventId );
          if ( eventTable.Count > 0 )
          {
            eventRow = eventTable[ 0 ];
            if ( Global.UserId == eventRow.UserId )
            {
              EventId = eventId;
              EventName = eventRow.Name;
            }
          }
        }
      }

      if ( EventId == 0 )
      {
        Response.Redirect( "~/default.aspx" , true );
      }
      else if ( Global.IsSimpleEventsModel && !eventRow.IsDefault )
      {
        Response.Redirect( "~/default.aspx" , true );
      }
      else if ( !IsPostBack )
      {
        this.waypointsGridView.Sort( "Name" , System.Web.UI.WebControls.SortDirection.Ascending );

        this.coordFormatTopDropDownList.SelectedValue =
          this.coordFormatBottomDropDownList.SelectedValue =
          Global.CoordFormat.ToString( );
      }
    }

    protected void Page_Load( object sender , EventArgs e )
    {
      if ( this.fileUpload.HasFile )
      {
        try
        {
          int updatedRecord =
            Tools.WaypointsLoader.LoadWaypoints(
              EventId ,
              this.fileUpload ,
              this.rbExistingWptUploadRule.SelectedValue == "replace"
            );

          if ( updatedRecord == 0 )
          {
            LoadWaypointsMsg = "File processed, but no waypoints added or updated.";
          }
        }
        catch ( Exception exc )
        {
          LoadWaypointsMsg = "Error in processing the uploaded file:<br />" + exc.Message;
        }

        this.waypointsGridView.DataBind( );
      }
    }

    protected void SignOutLinkButton_Click( object sender , EventArgs e )
    {
      Response.Clear( );
      FormsAuthentication.SignOut( );
      Response.Redirect( "~/default.aspx" , true );
    }

    private string deletingName;

    protected void waypointsGridView_RowDeleting( object sender , GridViewDeleteEventArgs e )
    {
      this.deletingName = e.Values[ "Name" ] as string;
    }

    protected void waypointsDataSource_Deleted( object sender , SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception != null )
      {
        string msg = GetWaypointsUpdateErrorMessage( e );
        e.ExceptionHandled = true;
        UpdateWaypointsMsg = msg;
      }
    }

    protected void waypointsGridView_RowUpdating( object sender , GridViewUpdateEventArgs e )
    {
      GridViewRow row = this.waypointsGridView.Rows[ e.RowIndex ];

      e.NewValues[ "Lat" ] = GetCoordsControl( row , "latEditMultiView" ).Value;
      e.NewValues[ "Lon" ] = GetCoordsControl( row , "lonEditMultiView" ).Value;

      this.selectedRowIndex = e.RowIndex;
    }

    protected void waypointsDataSource_Updated( object sender , SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception != null )
      {
        string msg = GetWaypointsUpdateErrorMessage( e );
        e.ExceptionHandled = true;
        UpdateWaypointsMsg = msg;
      }
    }

    protected void waypointsGridView_RowUpdated( object sender , GridViewUpdatedEventArgs e )
    {
      if ( !string.IsNullOrEmpty( UpdateWaypointsMsg ) )
      {
        this.scrollHiddenField.Value = ""; // because error string is on top of the form
        e.KeepInEditMode = true;
        this.selectedRowIndex = -1; // don't need selection if something failed
      }
    }

    protected void waypointsGridView_RowDeleted( object sender , GridViewDeletedEventArgs e )
    {
      if ( !string.IsNullOrEmpty( UpdateWaypointsMsg ) )
      {
        this.scrollHiddenField.Value = ""; // because error string is on top of the form
      }
    }

    private const string CoordFormatViewStateName = "CoordFormat";

    protected void formView_OnDataBound( object sender , EventArgs e )
    {
      MultiView addWptFormatMultiView = ( MultiView ) formView.FindControl( "addWptFormatMultiView" );
      View addWptDeg = ( View ) formView.FindControl( "addWptDeg" );
      View addWptDegMin = ( View ) formView.FindControl( "addWptDegMin" );
      View addWptDegMinSec = ( View ) formView.FindControl( "addWptDegMinSec" );

      CoordFormat coordFormat = Global.CoordFormat;

      switch ( coordFormat )
      {
        case CoordFormat.DegMin:
          addWptFormatMultiView.SetActiveView( addWptDegMin );
          break;

        case CoordFormat.DegMinSec:
          addWptFormatMultiView.SetActiveView( addWptDegMinSec );
          break;

        default:
          addWptFormatMultiView.SetActiveView( addWptDeg );
          break;
      }

      ViewState[ CoordFormatViewStateName ] = coordFormat;
    }

    protected void formView_ItemInserting( object sender , FormViewInsertEventArgs e )
    {
      { // trim passed Name
        object objName = e.Values[ "Name" ];
        if ( objName is string )
        {
          string name = objName.ToString( );
          e.Values[ "Name" ] = name.Trim( );
        }
      }

      { // trim passed Description
        object objDescr = e.Values[ "Description" ];
        if ( objDescr is string )
        {
          string descr = objDescr.ToString( );
          e.Values[ "Description" ] = descr.Trim( );
        }
      }

      CoordFormat coordFormat = ( CoordFormat ) ViewState[ CoordFormatViewStateName ];
      switch ( coordFormat )
      {
        case CoordFormat.DegMin:
          e.Values[ "Lat" ] = GetCoordControlValue( this.formView , "latDegMin" );
          e.Values[ "Lon" ] = GetCoordControlValue( this.formView , "lonDegMin" );
          break;

        case CoordFormat.DegMinSec:
          e.Values[ "Lat" ] = GetCoordControlValue( this.formView , "latDegMinSec" );
          e.Values[ "Lon" ] = GetCoordControlValue( this.formView , "lonDegMinSec" );
          break;

        default: // CoordFormat.Deg
          e.Values[ "Lat" ] = GetCoordControlValue( this.formView , "latDeg" );
          e.Values[ "Lon" ] = GetCoordControlValue( this.formView , "lonDeg" );

          break;
      }
    }

    private static double GetCoordControlValue( Control container , string coordControlName )
    {
      CoordControlBase coordControl =
        ( CoordControlBase ) container.FindControl( coordControlName );

      if ( coordControl == null )
        throw new ApplicationException(
          string.Format( "Can't find control '{0}'" , coordControlName )
        );

      return coordControl.Value;
    }

    protected void waypointsDataSource_Inserted( object sender , SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception != null )
      {
        string msg = GetWaypointsUpdateErrorMessage( e );
        e.ExceptionHandled = true;
        InsertWaypointMsg = msg;
      }
    }

    private string GetWaypointsUpdateErrorMessage( SqlDataSourceStatusEventArgs e )
    {
      string msg = null;
      if ( Global.IsSqlDuplicateError( e.Exception ) )
      {
        string name = e.Command.Parameters[ "@Name" ].Value.ToString( );
        msg = string.Format( "'{0}' has already been used as a waypoint name" , name );
      }
      else if ( e.Exception is SqlException )
      {
        SqlException sqlException = ( e.Exception as SqlException );
        foreach ( SqlError sqlError in sqlException.Errors )
        {
          if ( sqlError.Number == 547 && sqlError.Message.Contains( "FK_Task_Waypoint" ) )
          {
            msg = string.Format( "Cannot delete the waypoint '{0}' because it's used in the task" , this.deletingName );
          }
        }
      }

      if ( msg == null )
      {
        msg = e.Exception.Message;
      }

      return msg;
    }

    protected void formView_ItemInserted( object sender , FormViewInsertedEventArgs e )
    {
      if ( string.IsNullOrEmpty( InsertWaypointMsg ) )
      {
        this.justInsertedName = e.Values[ "Name" ] as string;
        double lat = Convert.ToDouble( e.Values[ "Lat" ] );
        double lon = Convert.ToDouble( e.Values[ "Lon" ] );
        Global.SetDefHemispheres( lat , lon );
      }
      else
      {
        e.KeepInInsertMode = true;
      }
    }

    private string justInsertedName = null;

    protected void waypointsGridView_RowDataBound( object sender , GridViewRowEventArgs e )
    {
      // There is some overhead in calling Global.CoordFormat, so call it once:
      CoordFormat coordFormat = Global.CoordFormat;

      if ( e.Row.RowType == DataControlRowType.EmptyDataRow )
      {
        this.manageEventLinkTopPanel.Visible =
          this.manageEventLinlBottomPanel.Visible = false;
      }
      else
      {
        this.manageEventLinkTopPanel.Visible =
          this.manageEventLinlBottomPanel.Visible = Global.IsSimpleEventsModel;

        if ( this.justInsertedName != null )
        {
          DataRowView drv = e.Row.DataItem as DataRowView;
          if ( drv != null &&
               string.Compare( drv[ "Name" ] as string , this.justInsertedName ) == 0 )
          {
            this.selectedRowIndex = e.Row.RowIndex;
          }
        }

        if ( e.Row.RowType == DataControlRowType.DataRow )
        {
          if ( e.Row.RowIndex == this.waypointsGridView.EditIndex )
          {
            FillEditRow( e , coordFormat );
          }
          else
          {
            FillStaticRow( e , coordFormat );
          }
        }
      }
    }

    private void FillEditRow( GridViewRowEventArgs e , CoordFormat coordFormat )
    {
      SetCoordsControlValue( e , "latEditMultiView" , "Lat" );
      SetCoordsControlValue( e , "lonEditMultiView" , "Lon" );
    }

    private void SetCoordsControlValue( GridViewRowEventArgs e , string editMultiViewName , string columnName )
    {
      DataRowView drv = e.Row.DataItem as DataRowView;

      if ( drv == null )
        throw new InvalidOperationException( "Can't get DataRowView" );

      CoordControlBase coordControl = GetCoordsControl( e.Row , editMultiViewName );
      coordControl.Value = Convert.ToDouble( drv[ columnName ] ); ;
    }

    private static CoordControlBase GetCoordsControl( GridViewRow row , string editMultiViewName )
    {
      MultiView editMultiView = ( MultiView ) row.FindControl( editMultiViewName );

      if ( editMultiView == null )
        throw new InvalidOperationException(
          string.Format( "Can't find editing multi view '{0}'" , editMultiViewName ) );

      foreach ( Control ctrl in editMultiView.GetActiveView( ).Controls )
      {
        if ( ctrl is CoordControlBase )
        {
          return ctrl as CoordControlBase;
        }
      }

      throw new InvalidOperationException(
        string.Format( "Can't find CoordControlBase in controls of the active view of '{0}'" , editMultiViewName ) );
    }

    private void FillStaticRow( GridViewRowEventArgs e , CoordFormat coordFormat )
    {
      DataRowView drv = e.Row.DataItem as DataRowView;
      Label latLabel = ( Label ) e.Row.FindControl( "latLabel" );
      Label lonLabel = ( Label ) e.Row.FindControl( "lonLabel" );

      if ( drv == null ||
           latLabel == null ||
           lonLabel == null ) return;

      double lat = Convert.ToDouble( drv[ "Lat" ] );
      double lon = Convert.ToDouble( drv[ "Lon" ] );

      if ( coordFormat == CoordFormat.DegMin )
      {
        latLabel.Text = CoordToDegMin( lat , 'S' , 'N' );
        lonLabel.Text = CoordToDegMin( lon , 'W' , 'E' );
      }
      else if ( coordFormat == CoordFormat.DegMinSec )
      {
        latLabel.Text = CoordToDegMinSec( lat , 'S' , 'N' );
        lonLabel.Text = CoordToDegMinSec( lon , 'W' , 'E' );
      }
      else
      {
        latLabel.Text = lat.ToString( "F5" );
        lonLabel.Text = lon.ToString( "F5" );
      }
    }

    private string CoordToDegMin( double coord , char negPrefix , char posPrefix )
    {
      char prefix;
      int deg;
      int min;
      int minFraction;

      DegMin.CoordToDegMin( coord , negPrefix , posPrefix , out prefix , out deg , out min , out minFraction );

      return
        string.Format
        (
          "{0}&nbsp;{1}&deg;&nbsp;{2:D2}.{3:D3}&#39;" ,
          prefix ,
          deg ,
          min ,
          minFraction
        );
    }

    private string CoordToDegMinSec( double coord , char negPrefix , char posPrefix )
    {
      char prefix;
      int deg;
      int min;
      int sec;
      int secFraction;

      DegMinSec.CoordToDegMinSec(
        coord ,
        negPrefix ,
        posPrefix ,
        out prefix ,
        out deg ,
        out min ,
        out sec ,
        out secFraction
      );

      return
        string.Format
        (
          "{0}{1}&deg;&nbsp;{2:D2}&#39;&nbsp;{3:D2}.{4:D1}&quot;" ,
          prefix ,
          deg ,
          min ,
          sec ,
          secFraction
        );
    }

    private int selectedRowIndex = -1;

    protected void waypointsGridView_DataBound( object sender , EventArgs e )
    {
      // For unknown reasons setting SelectedIndex doesn't work in RowDataBound event handler, so doing that here:
      this.waypointsGridView.SelectedIndex = selectedRowIndex;
    }

    protected void deleteBtn_Click( object sender , EventArgs e )
    {
      TrackerDataSetTableAdapters.WaypointTableAdapter waypointTableAdapter =
        new TrackerDataSetTableAdapters.WaypointTableAdapter( );

      waypointTableAdapter.DeleteEventWaypoints( EventId );
      this.DataBind( );
    }

    protected void coordFormatDropDownList_SelectedIndexChanged( object sender , EventArgs e )
    {
      DropDownList ddl = ( DropDownList ) sender;
      Global.CoordFormat = ( CoordFormat ) Enum.Parse( typeof( CoordFormat ) , ddl.SelectedValue );
      this.coordFormatTopDropDownList.SelectedValue = ddl.SelectedValue;
      this.coordFormatBottomDropDownList.SelectedValue = ddl.SelectedValue;
      this.waypointsGridView.DataBind( );
      this.formView.DataBind( );
    }

    protected void Page_PreRender( object sender , EventArgs e )
    {
    }

    protected int EditViewIndex
    {
      get
      {
        switch ( Global.CoordFormat )
        {
          case CoordFormat.Deg:
            return 0;

          case CoordFormat.DegMin:
            return 1;

          case CoordFormat.DegMinSec:
            return 2;
        }

        throw new ApplicationException( "Unknown CoordFormat " + Global.CoordFormat.ToString( ) );
      }
    }
  }
}