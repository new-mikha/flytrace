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
using System.Xml;
using System.Globalization;
using System.Data;

namespace FlyTrace
{
  public partial class ManageEventForm : System.Web.UI.Page
  {
    protected string UpdateEventMsg = "";
    protected string LoadWaypointsMsg = "";
    protected string TaskSaveInfo;
    protected string TaskSaveError;

    protected int AllEventWptsCount
    { // Can be changed by LoadAllWaypoints so keep it like this, not as just a class field.
      get { return this.allEventWaypoints.Count; }
    }

    private TrackerDataSet.EventRow eventRow;

    protected string EventName
    {
      get { return this.eventRow.Name; }
    }

    // need to be a public property because accessed by EvalParameter - it can't 
    // access non-public stuff, and requires a property rather than a field.
    public int EventId { get; private set; }

    public bool IsEventDefault { get; private set; }

    TrackerDataSet.TaskDataTable taskTable;

    TrackerDataSetTableAdapters.TaskTableAdapter taskAdapter =
      new FlyTrace.TrackerDataSetTableAdapters.TaskTableAdapter( );

    private TrackerDataSet.WaypointDataTable allEventWaypoints;

    private int assignedGroupsCount;

    private Dictionary<string, string> defaultRadius = new Dictionary<string, string>( );

    protected void taskPanel_Init( object sender, EventArgs e )
    {
      // save default values of radius text boxes (don't know how to get it otherwise)
      for ( int iWp = 0; iWp < MaxNumberOfTaskWaypoints; iWp++ )
      {
        string tbName = "tbRadius" + iWp.ToString( );
        TextBox radiusTextBox = ( TextBox ) this.taskPanel.FindControl( tbName );
        this.defaultRadius[tbName] = radiusTextBox.Text;
      }
    }

    protected void Page_PreInit( object sender, EventArgs e )
    {
      string strEventId = Request.QueryString["event"];
      int eventId;

      if ( strEventId != null && int.TryParse( strEventId, out eventId ) )
      {
        // To make sure that there is no hacking, check that the event 
        // belongs to the current user before setting this.EventId property:
        if ( Global.IsAuthenticated )
        {
          TrackerDataSetTableAdapters.EventTableAdapter eventAdapter = new FlyTrace.TrackerDataSetTableAdapters.EventTableAdapter( );
          TrackerDataSet.EventDataTable eventTable = eventAdapter.GetDataByEventId( eventId );
          if ( eventTable.Count > 0 )
          {
            if ( Global.UserId == eventTable[0].UserId )
            {
              this.eventRow = eventTable[0];
              EventId = this.eventRow.Id;
              IsEventDefault = this.eventRow.IsDefault;
            }
          }
        }
      }

      if ( EventId == 0 )
      {
        Response.Redirect( "~/default.aspx", true );
      }
      else if ( Global.IsSimpleEventsModel && !this.eventRow.IsDefault )
      {
        Response.Redirect( "~/default.aspx", true );
      }
      else
      {
        LoadAllWaypoints( );

        TrackerDataSetTableAdapters.GroupTableAdapter groupTableAdapter = new TrackerDataSetTableAdapters.GroupTableAdapter( );
        this.assignedGroupsCount =
          groupTableAdapter.GetAssignedGroupsCount( Global.UserId, EventId ).Value;

        if ( !IsPostBack )
        {
          SetAssignedGroupsReadView( );
          SetNoGroupWarningVisibility( );
        }
      }
    }

    protected void Page_Load( object sender, EventArgs e )
    {
      if ( this.fileUpload.HasFile )
      {
        try
        {
          int updatedRecord = Tools.WaypointsLoader.LoadWaypoints( EventId, this.fileUpload, true );
          if ( updatedRecord == 0 )
          {
            LoadWaypointsMsg = "No waypoints found in the uploaded file.";
          }
          LoadAllWaypoints( ); // we need to update this.allEventWaypoints
          SetNoGroupWarningVisibility( );
        }
        catch ( Exception exc )
        {
          LoadWaypointsMsg = "Error in processing the uploaded file:<br />" + exc.Message;
        }
      }
    }

    private void LoadAllWaypoints( )
    {
      TrackerDataSetTableAdapters.WaypointTableAdapter waypointsAdapter =
        new FlyTrace.TrackerDataSetTableAdapters.WaypointTableAdapter( );

      this.allEventWaypoints = waypointsAdapter.GetDataByEventId( EventId );
    }

    private void SetNoGroupWarningVisibility( )
    {
      this.noGroupWarningPanel.Visible =
        AllEventWptsCount > 0 &&
        this.assignedGroupsCount == 0;
    }

    private void SetAssignedGroupsReadView( )
    {
      if ( this.assignedGroupsCount == 0 )
      {
        this.assignedTaskMultiView.SetActiveView( this.assignedTaskEmptyView );
      }
      else
      {
        this.assignedTaskMultiView.SetActiveView( this.assignedTaskReadView );
      }
    }

    private const int MaxNumberOfTaskWaypoints = 9;

    protected void SignOutLinkButton_Click( object sender, EventArgs e )
    {
      Response.Clear( );
      FormsAuthentication.SignOut( );
      Response.Redirect( "~/default.aspx", true );
    }

    protected void eventHeaderDataSource_Updated( object sender, SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception != null )
      {
        string msg;
        if ( Global.IsSqlDuplicateError( e.Exception ) )
          msg = string.Format( "Event '{0}' already exists", e.Command.Parameters["@Name"].Value );
        else
          msg = e.Exception.Message;
        e.ExceptionHandled = true;
        UpdateEventMsg = msg;
      }
    }

    protected void eventHeaderDataSource_Deleted( object sender, SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception == null )
      {
        Response.Redirect( "~/default.aspx", true );
      }
      else
      {
        UpdateEventMsg = e.Exception.Message;
        e.ExceptionHandled = true;
      }
    }

    protected void eventHeaderGridView_RowUpdated( object sender, GridViewUpdatedEventArgs e )
    {
      if ( !string.IsNullOrEmpty( UpdateEventMsg ) )
      {
        e.KeepInEditMode = true;
      }
      else
      {
        /* Sequence of events:
         * - Page_PreInit (where is this.eventRow is read to the old values)
         * - this method
         * So we need to update this.eventRow.Name. We can't move loading logic from
         * Page_PreInit to Page_PreRender, because we need to get the ID as eary as possible,
         * e.g. to prevent update with wrong ID.
         */
        this.eventRow.Name = e.NewValues["Name"] as string;
        this.eventRow.AcceptChanges( ); // DB record has already been updated, so we don't need this record as 'Changed'
      }
    }

    protected void saveBtn_Click( object sender, EventArgs e )
    {
      try
      {
        this.taskTable = this.taskAdapter.GetDataByEventId( EventId );

        foreach ( DataRow row in this.taskTable.Rows )
        {
          row.Delete( );
        }

        for ( int iWp = 0; iWp < MaxNumberOfTaskWaypoints; iWp++ )
        {
          AddTaskPoint( iWp );
        }

        this.taskAdapter.Update( this.taskTable );

        TaskSaveInfo = "Done, task saved.<br />";
      }
      catch ( Exception exc )
      {
        TaskSaveError = "TASK NOT SAVED: " + exc.Message + "<br />";
      }
    }

    private void AddTaskPoint( int iWp )
    {
      try
      {
        DropDownList ddlWp = ( DropDownList ) this.taskPanel.FindControl( "ddlWp" + iWp.ToString( ) );
        TextBox radiusTextBox = ( TextBox ) this.taskPanel.FindControl( "tbRadius" + iWp.ToString( ) );

        if ( string.IsNullOrEmpty( ddlWp.SelectedValue ) )
          return;

        int waypointId = int.Parse( ddlWp.SelectedValue );

        int radius = 0;
        if ( radiusTextBox.Text == "" || !int.TryParse( radiusTextBox.Text, out radius ) || radius < 0 )
        {
          throw new ApplicationException(
            string.Format(
              "Radius is invalid, it should be non-negative integer value.", iWp ) );
        }

        this.taskTable.AddTaskRow( waypointId, radius, iWp );
      }
      catch ( Exception exc )
      {
        throw new ApplicationException(
          string.Format( "Problem with TP #{0}: {1}", iWp, exc.Message ),
          exc );
      }
    }

    private void ShowWaypoint( int iWp, DataRow[] sortedByWptOrder )
    {
      DropDownList waypointDdl = ( DropDownList ) this.taskPanel.FindControl( "ddlWp" + iWp.ToString( ) );

      string tbName = "tbRadius" + iWp.ToString( );
      TextBox radiusTextBox = ( TextBox ) this.taskPanel.FindControl( tbName );
      if ( sortedByWptOrder.Length <= iWp )
      {
        radiusTextBox.Text = this.defaultRadius[tbName];
        return;
      }

      TrackerDataSet.TaskRow taskRow = ( TrackerDataSet.TaskRow ) sortedByWptOrder[iWp];
      waypointDdl.SelectedValue = taskRow.WaypointId.ToString( );
      radiusTextBox.Text = taskRow.Radius.ToString( );
    }

    protected void showHideGroupsButton_Click( object sender, EventArgs e )
    {
      if ( this.groupsPanelMultiView.GetActiveView( ) == this.defaultGroupPanelView )
      {
        this.groupsPanelMultiView.SetActiveView( this.expandedGroupPanelView );
      }
      else
      {
        this.groupsPanelMultiView.SetActiveView( this.defaultGroupPanelView );
      }
    }

    protected void defaultButton_Click( object sender, EventArgs e )
    {
      if ( this.defaultEventControlsMultiView.GetActiveView( ) == this.defaultEventReadView )
      {
        this.defaultEventControlsMultiView.SetActiveView( this.defaultEventEditView );
        this.defaultCheckBox.Checked = IsEventDefault;

        SetAssignedGroupsReadView( );
      }
      else
      {
        if ( sender == this.defaultSaveButton )
        {
          // We can't just set this.eventRow.IsDefault and call adapter.Update, because there is more 
          // complicated DB logic in setting this value, which is done in SetEventDefaultFlag. Sp
          // call that stored proc here:
          bool? isDefaultValue = this.defaultCheckBox.Checked ? true : false;
          TrackerDataSetTableAdapters.ProcsAdapter procAdapters = new FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter( );

          if ( this.defaultCheckBox.Checked )
            procAdapters.SetEventAsDefault( EventId );
          else
            procAdapters.SetEventAsNonDefault( EventId );

          // We need to update this.IsEventDefault that is used in the markup
          IsEventDefault = isDefaultValue.Value;
        }

        // If "groups assignment" was in edit mode, cancel any changes and return it to the read mode:
        this.defaultEventControlsMultiView.SetActiveView( this.defaultEventReadView );
      }
    }

    protected void assignGroupsButton_Click( object sender, EventArgs e )
    {
      if ( groupsPanelMultiView.GetActiveView( ) == this.defaultGroupPanelView )
      {
        this.groupsPanelMultiView.SetActiveView( this.expandedGroupPanelView );
      }

      if ( this.assignedTaskMultiView.GetActiveView( ) != this.assignedTaskEditView )
      {
        this.assignedTaskMultiView.SetActiveView( this.assignedTaskEditView );

        // If "default Event controls" was in edit mode, cancel any changes and return it to the read mode:
        this.defaultEventControlsMultiView.SetActiveView( this.defaultEventReadView );
      }
      else
      {
        bool isChanged = false;
        if ( sender == this.assignGroupsSaveButton )
        {
          TrackerDataSetTableAdapters.GroupTableAdapter groupTableAdapter = new TrackerDataSetTableAdapters.GroupTableAdapter( );
          TrackerDataSet.GroupDataTable userGroups = groupTableAdapter.GetDataByUserId( Global.UserId );

          int tempAssignedGroupsCount = 0;
          foreach ( ListItem li in this.assignedGroupsCheckBoxList.Items )
          {
            int groupId = int.Parse( li.Value );
            TrackerDataSet.GroupRow groupRow = userGroups.FindById( groupId );

            // Note that the content of assignedGroupsCheckBoxList came from the ViewState, and the group 
            // can be actually deleted by that time (or replaced in ViewState by another user's group). So check 
            // if it's valid group to prevent NullReferenceExcpetion:
            if ( groupRow == null )
              continue;

            if ( li.Selected )
            {
              tempAssignedGroupsCount++;
              if ( groupRow.IsEventIdNull( ) ||
                   groupRow.EventId != EventId )
                groupRow.EventId = EventId;
            }
            else
            {
              if ( !groupRow.IsEventIdNull( ) &&
                   groupRow.EventId == EventId )
              {
                groupRow.SetEventIdNull( );
              }
            }
          }

          int updatedRecords = groupTableAdapter.Update( userGroups );
          this.assignedGroupsCount = tempAssignedGroupsCount;
          isChanged = updatedRecords > 0;
          if ( isChanged )
          {
            TrackerDataSetTableAdapters.ProcsAdapter procsAdapter = new FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter( );
            procsAdapter.SetEventAsDefault( EventId );

            // We just set event as default in the DB - now we need to update this.IsEventDefault that is used in the markup
            IsEventDefault = true;
          }
        }

        SetAssignedGroupsReadView( );
        SetNoGroupWarningVisibility( );
        if ( isChanged )
        {
          this.assignedGroupsGridView.DataBind( );
        }
      }
    }

    protected void assignedGroupsCheckBoxList_DataBound( object sender, EventArgs e )
    {
      TrackerDataSetTableAdapters.GroupTableAdapter groupTableAdapter = new TrackerDataSetTableAdapters.GroupTableAdapter( );
      TrackerDataSet.GroupDataTable assignedGroups = groupTableAdapter.GetAssignedGroups( Global.UserId, EventId );

      foreach ( ListItem li in this.assignedGroupsCheckBoxList.Items )
      {
        int groupId = int.Parse( li.Value );
        li.Selected = assignedGroups.FindById( groupId ) != null;
      }
    }

    protected void assignedGroupsGridView_DataBound( object sender, EventArgs e )
    {
      assignedGroupsGridView.AllowSorting = this.assignedGroupsGridView.Rows.Count > 1;
    }

    protected string OldPointsCleanUpAge
    {
      get
      {
        if ( this.eventRow.IsStartTsNull( ) )
          return "(none)";

        return FlyTrace.LocationLib.Tools.GetAgeStr( this.eventRow.StartTs, false );
      }
    }

    protected string OldPointsCleanUpUtcTs
    {
      get
      {
        if ( this.eventRow.IsStartTsNull( ) )
          return "(any time)";

        return this.eventRow.StartTs.ToString( Resources.Resources.AgeFormat ) + " UTC";
      }
    }

    protected string OldPointsCleanUpLocalTs
    {
      get
      {
        return this.eventRow.StartTs.ToLocalTime( ).ToString( Resources.Resources.AgeFormat ) + " local time";
      }
    }

    protected void clearOldPointsButton_Click( object sender, EventArgs e )
    {
      int hrsBack = 0;

      {
        string suffix = "_hr";
        Control ctrl = ( Control ) sender;

        if ( ctrl.ID.EndsWith( suffix ) )
        {
          string noSuffix = ctrl.ID.Remove( ctrl.ID.Length - suffix.Length );
          int iUnderscore = noSuffix.LastIndexOf( '_' );
          if ( iUnderscore < 0 )
            throw new ApplicationException( "Unexpected sender control name " + ctrl.ID );
          {
            string hrsString = noSuffix.Substring( iUnderscore + 1 );
            hrsBack = int.Parse( hrsString );
          }
        }
      }

      DateTime utcThreshold = DateTime.UtcNow.AddHours( -hrsBack );

      TrackerDataSetTableAdapters.EventTableAdapter adapter = new TrackerDataSetTableAdapters.EventTableAdapter( );
      adapter.UpdateEventStartTs( EventId, utcThreshold );

      Service.ServiceFacade.ResetGroupsDefCache( );

      // this.eventRow accessed in Page_PreRender, so set it to the correct value:
      this.eventRow.StartTs = utcThreshold;
      this.eventRow.AcceptChanges( );
    }

    protected void restoreOldPointsPanel_Click( object sender, EventArgs e )
    {
      TrackerDataSetTableAdapters.EventTableAdapter adapter = new TrackerDataSetTableAdapters.EventTableAdapter( );
      adapter.UpdateEventStartTs( EventId, null );

      Service.ServiceFacade.ResetGroupsDefCache( );

      // this.eventRow accessed in Page_PreRender, so set it to NULL here too:
      this.eventRow.SetStartTsNull( );
      this.eventRow.AcceptChanges( );
    }

    protected void advancedCleanUpShowHideLinkButton_Click( object sender, EventArgs e )
    {
      if ( this.advancedCleanUpPanel.Visible )
      {
        this.advancedCleanUpShowHideLinkButton.Text = Resources.Resources.ShowAdvPointsCleanUp;
        this.advancedCleanUpPanel.Visible = false;
      }
      else
      {
        this.advancedCleanUpShowHideLinkButton.Text = Resources.Resources.HideAdvPointsCleanUp;
        this.advancedCleanUpPanel.Visible = true;
      }
    }

    protected void Page_PreRender( object sender, EventArgs e )
    {
      // The code below can't be in Page_Load because uploadBtn_Click is fired after that, and 
      // we need all the just inserted wpts when we fill comboboxes and decide whether to show taskPanel or not.
      // At the same time, we can't do that in uploadBtn_Click because we need the same stuff every time page loads.
      // So do it here, after all other events has fired.

      if ( string.IsNullOrEmpty( TaskSaveError ) )
      {
        this.loadWaypointsPanel.Visible = noWaypointsInfoPanel.Visible = AllEventWptsCount == 0;
        this.taskPanel.Visible = !this.loadWaypointsPanel.Visible;

        DropDownList[] waypointsDdls = new DropDownList[] {
            ddlWp0, ddlWp1,  ddlWp2,  ddlWp3,  ddlWp4,  ddlWp5,  ddlWp6,  ddlWp7,  ddlWp8 };

        DataRow[] sortedWaypoints = this.allEventWaypoints.Select( "", "Name" );

        foreach ( DropDownList ddl in waypointsDdls )
        {
          ddl.Items.Clear( );
          ddl.Items.Add( "" );

          foreach ( TrackerDataSet.WaypointRow row in sortedWaypoints )
          {
            ListItem li = new ListItem( row.Name, row.Id.ToString( ) );
            ddl.Items.Add( li );
          }
        }

        if ( this.taskTable == null )
        {
          this.taskTable = this.taskAdapter.GetDataByEventId( EventId );
        }

        DataRow[] sortedByWptOrder = this.taskTable.Select( "", "WptOrder" );
        for ( int iWp = 0; iWp < MaxNumberOfTaskWaypoints; iWp++ )
        {
          ShowWaypoint( iWp, sortedByWptOrder );
        }
      }

      this.clearOldPointsInfoPanel.Visible =
        this.restoreOldPointsPanel.Visible =
        !this.eventRow.IsStartTsNull( );
    }
  }
}