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
using System.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.Linq;

using FlyTrace.LocationLib;
using FlyTrace.LocationLib.Data;

// UserGroupsGrid.ascx
// Resources.resx
// Shared/public depending on if it's shared or public
// ALTER TABLE dbo.[Group] ADD IsPublic BIT NOT NULL CONSTRAINT Group_IsPublic DEFAULT 1
// --ALTER TABLE dbo.[Group] DROP CONSTRAINT Group_IsPublic

namespace FlyTrace
{
  public partial class ManageGroupForm : System.Web.UI.Page
  {
    protected string InsertTrackerMsg = "";
    protected string UpdateTrackerMsg = "";
    protected string UpdateGroupMsg = "";

    private const string spotIdPattern =
      @"^\s*((http://)?share.findmespot.com/shared/faces/viewspots.jsp\?glId=)?[^\s\\/\?=&]{20,50}\s*$";

    protected string SpotIdJScriptPattern
    {
      get
      {
        return spotIdPattern.Replace( @"\", @"\\" );
      }
    }

    protected int? AssignedTaskId;

    // need to be a public property because accessed by EvalParameter - it can't 
    // access non-public stuff, and requires a property rather than a field.
    public int GroupId { get; private set; }

    protected string GroupName
    {
      get { return this.groupTable[0].Name; }
    }

    private TrackerDataSetTableAdapters.GroupTableAdapter groupAdapter =
      new FlyTrace.TrackerDataSetTableAdapters.GroupTableAdapter( );
    private TrackerDataSet.GroupDataTable groupTable;

    private bool isReadOnly = false;

    protected void Page_PreInit( object sender, EventArgs e )
    {
      string strGroupId = Request.QueryString["group"];
      int groupId;

      if ( strGroupId != null && int.TryParse( strGroupId, out groupId ) )
      {
        // To make sure that there is no hacking, check that the event 
        // belongs to the current user before setting this.EventId property:
        if ( Global.IsAuthenticated )
        {
          this.groupTable = groupAdapter.GetDataByGroupId( groupId );
          if ( this.groupTable.Count > 0 )
          {
            if ( Global.UserId == groupTable[0].UserId ||
                 Global.IsAdmin ||
                 Global.IsSpotIdReader )
            {
              this.isReadOnly = Global.UserId != groupTable[0].UserId;

              GroupId = this.groupTable[0].Id;
              if ( !this.groupTable[0].IsEventIdNull( ) )
                AssignedTaskId = this.groupTable[0].EventId;
            }
          }
        }
      }

      if ( GroupId == 0 )
      {
        Response.Redirect( "~/default.aspx", true );
      }
      else
      {
        if ( Global.IsSimpleEventsModel )
        { 
          TrackerDataSetTableAdapters.ProcsAdapter procsAdapter =
            new TrackerDataSetTableAdapters.ProcsAdapter( );

          int? loadedWptsCount = null;
          procsAdapter.GetDefaultEventParams( Global.UserId, ref DefEventId, ref loadedWptsCount, ref DefEventTaskWptCount );
        }

        if ( !IsPostBack )
        {
          this.trackersGridView.Sort( "Name", System.Web.UI.WebControls.SortDirection.Ascending );
        }
      }
    }

    protected int? DefEventId;
    protected int? DefEventTaskWptCount;

    protected void SignOutLinkButton_Click( object sender, EventArgs e )
    {
      Response.Clear( );
      FormsAuthentication.SignOut( );
      Response.Redirect( "~/default.aspx", true );
    }

    protected void groupHeaderDataSource_Updated( object sender, SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception != null )
      {
        string msg;
        if ( Global.IsSqlDuplicateError( e.Exception ) )
          msg = string.Format( "Group '{0}' already exists", e.Command.Parameters["@Name"].Value );
        else
          msg = e.Exception.Message;
        e.ExceptionHandled = true;
        UpdateGroupMsg = msg;
      }
    }

    protected void groupHeaderDataSource_Deleted( object sender, SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception == null )
      {
        Response.Redirect( "~/default.aspx", true );
      }
      else
      {
        UpdateGroupMsg = e.Exception.Message;
        e.ExceptionHandled = true;
      }
    }

    protected void groupHeaderGridView_RowUpdated( object sender, GridViewUpdatedEventArgs e )
    {
      if ( !string.IsNullOrEmpty( UpdateGroupMsg ) )
      {
        e.KeepInEditMode = true;
      }
      else
      {
        /* Sequence of events:
         * - Page_PreInit (where is this.eventRow is read to the old values)
         * - this method
         * So we need to update groupTable[0].Name. We can't move loading logic from
         * Page_PreInit to Page_PreRender, because we need to get the ID as eary as possible,
         * e.g. to prevent update with wrong ID.
         */
        this.groupTable[0].Name = e.NewValues["Name"] as string;
        this.groupTable[0].AcceptChanges( ); // DB record has already been updated, so we don't need this record as 'Changed'

      }
    }

    protected void trackersDataSource_Updated( object sender, SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception != null )
      {
        string msg = GetTrackerUpdateErrorMessage( e.Exception );
        e.ExceptionHandled = true;
        UpdateTrackerMsg = msg;
      }
    }

    protected void trackersGridView_RowUpdated( object sender, GridViewUpdatedEventArgs e )
    {
      if ( !string.IsNullOrEmpty( UpdateTrackerMsg ) )
      {
        e.KeepInEditMode = true;
      }
    }

    private const string spotPageAddrMarker = @"share.findmespot.com/shared/faces/viewspots.jsp?glId=";


    public void ValidateTrackerForeignId( object source, ServerValidateEventArgs args )
    {
      try
      {
        Regex regex = new Regex( spotIdPattern, RegexOptions.IgnoreCase );
        args.IsValid = regex.IsMatch( args.Value );
      }
      catch
      {
        args.IsValid = false;
      }
    }

    protected void formView_ItemInserting( object sender, FormViewInsertEventArgs e )
    {
      try
      {
        { // trim passed Name
          object objName = e.Values["Name"];
          if ( objName is string )
          {
            string name = objName.ToString( );
            e.Values["Name"] = name.Trim( );
          }
        }

        { // Now a) trim passed ID and b) try to find SPOT Id in it
          object objTrackerForeignId = e.Values["TrackerForeignId"];
          if ( objTrackerForeignId is string )
          {
            string trackerForeignId = objTrackerForeignId.ToString( );

            int iMarker = trackerForeignId.IndexOf( spotPageAddrMarker, StringComparison.CurrentCultureIgnoreCase );
            if ( iMarker >= 0 )
            {
              trackerForeignId = trackerForeignId.Substring( iMarker + spotPageAddrMarker.Length );
            }

            trackerForeignId = trackerForeignId.Trim( );

            TrackerDataSetTableAdapters.GroupTrackerTableAdapter adapter =
              new FlyTrace.TrackerDataSetTableAdapters.GroupTrackerTableAdapter( );

            // This proc returns value greater than zero if the tracker already exists 
            // in the database, which means it's OK, and 0 otherwise:
            int? dbTrackerCheck = adapter.ForeignTrackerIdCheck( trackerForeignId );

            if ( dbTrackerCheck == 0 )
            { // the tracker doesn't exists in the database, so we need to check it:
              string appAuxLogFolder = Path.Combine( HttpRuntime.AppDomainAppPath, "Serice\\logs" );
              LocationRequest locationRequest =
                new LocationRequest( trackerForeignId, appAuxLogFolder, LocationRequest.DefaultAttemptsOrder );
              TrackerState tracker = locationRequest.ReadLocation( );
              if ( tracker.Error != null &&
                   tracker.Error.Type == ErrorType.BadTrackerId )
              {
                InsertTrackerMsg =
                  string.Format(
                    "We cannot recognize the SPOT Share Page ID you've passed. Our best guess<br />for the Shared Page that you mean " +
                      "is <a href=\"http://share.findmespot.com/shared/faces/viewspots.jsp?glId={0}\"> " +
                      "that one</a>, but it doesn't work.",
                    trackerForeignId
                  );
                //BadSpotId = trackerForeignId;
                //this.formView.FindControl( "badSpotIdInfoPanel" ).Visible = true;
                e.Cancel = true;
              }
            }

            // put it back even if we didn't find spotPageAddrMarker in it, to ensure that it's trimmed:
            e.Values["TrackerForeignId"] = trackerForeignId.Trim( );
          }
        }
      }
      catch ( Exception exc )
      {
        e.Cancel = true;
        InsertTrackerMsg = exc.Message;
      }
    }

    public string BadSpotId;

    protected void formView_DataBound( object sender, EventArgs e )
    {

    }

    protected void trackersDataSource_Inserted( object sender, SqlDataSourceStatusEventArgs e )
    {
      if ( e.Exception != null )
      {
        string msg = GetTrackerUpdateErrorMessage( e.Exception );
        e.ExceptionHandled = true;
        InsertTrackerMsg = msg;
      }
    }

    private static string GetTrackerUpdateErrorMessage( Exception e )
    {
      string msg;
      if ( !Global.IsSqlDuplicateError( e ) )
      {
        msg = e.Message;
      }
      else if ( e.Message.Contains( "IX_GroupTracker_GroupId_Name" ) )
      {
        msg = "This group already has a pilot with such name";
      }
      else if ( e.Message.Contains( "IX_GroupTracker_GroupId_ForeignId" ) )
      {
        msg = "This SPOT ID has already been used in this group";
      }
      else
      {
        msg = e.Message;
      }

      return msg;
    }

    protected void formView_ItemInserted( object sender, FormViewInsertedEventArgs e )
    {
      if ( string.IsNullOrEmpty( InsertTrackerMsg ) )
      {
        this.justInsertedName = e.Values["Name"] as string;
      }
      else
      {
        e.KeepInInsertMode = true;
      }
    }

    private string justInsertedName;

    protected void trackersGridView_RowDataBound( object sender, GridViewRowEventArgs e )
    {
      if ( this.justInsertedName != null )
      {
        DataRowView drv = e.Row.DataItem as DataRowView;
        if ( drv != null &&
             string.Compare( drv["Name"] as string, this.justInsertedName ) == 0 )
        {
          this.selectedRowIndex = e.Row.RowIndex;
        }
      }
    }

    private int selectedRowIndex = -1;

    protected void trackersGridView_DataBound( object sender, EventArgs e )
    {
      // For unknown reasons setting SelectedIndex doesn't work in RowDataBound event handler, so doing that here:
      this.trackersGridView.SelectedIndex = selectedRowIndex;
    }

    protected void changeTaskMultiView_Click( object sender, EventArgs e )
    {
      CheckReadOnly( );
      if ( this.eventMultiView.GetActiveView( ) == this.eventShowView )
      {
        this.eventMultiView.SetActiveView( this.eventEditView );
      }
      else
      {
        if ( sender == this.updateAssignedTaskLinkButton )
        {
          TrackerDataSetTableAdapters.ProcsAdapter procsAdapter = new FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter( );
          int? eventId = int.Parse( this.assignedTaskDdl.SelectedValue );

          if ( eventId.Value == 0 )
          { // 0 means 'None', i.e. NULL
            eventId = null;
          }
          else if ( eventId.Value < 0 ) // less then zero means "Create new event and assign it to the group
          {
            procsAdapter.CreateEvent( Global.UserId, ref eventId );
            if ( !eventId.HasValue )
              throw new ApplicationException( "CreateEvent returned unexpected result: NULL" );
          }

          // Now assign the event to the group (or clear EventId field in DB if eventId is null)
          procsAdapter.AssignEventToGroup( eventId, GroupId );
        }

        this.eventMultiView.SetActiveView( this.eventShowView );
      }
    }

    protected void changeSimpleTaskMultiView_Click( object sender, EventArgs e )
    {
      CheckReadOnly( );
      if ( this.simpleTaskMultiView.GetActiveView( ) == this.simpleTaskShowView )
      {
        this.simpleTaskMultiView.SetActiveView( this.simpleTaskEditView );
        this.showTaskCheckBox.Checked =
          AssignedTaskId.HasValue &&
          DefEventId.HasValue &&
          AssignedTaskId.Value == DefEventId.Value;
      }
      else
      {
        if ( sender == this.updateSimpleTaskButton )
        {
          TrackerDataSetTableAdapters.ProcsAdapter procsAdapter = new FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter( );
          int? defaultEventId = null;

          if ( this.showTaskCheckBox.Checked )
          {
            procsAdapter.EnsureDefaultTask( Global.UserId, ref defaultEventId );
          }

          procsAdapter.AssignEventToGroup( defaultEventId, GroupId );
          AssignedTaskId = defaultEventId;
        }

        this.simpleTaskMultiView.SetActiveView( this.simpleTaskShowView );
      }
    }

    protected void assignedTaskDdl_DataBound( object sender, EventArgs e )
    {
      if ( this.groupTable[0].IsEventIdNull( ) )
      {
        this.assignedTaskDdl.SelectedValue = "0";
      }
      else
      {
        this.assignedTaskDdl.SelectedValue = this.groupTable[0].EventId.ToString( );
      }
    }

    protected void groupHeaderGridView_RowEditing( object sender, GridViewEditEventArgs e )
    {
      // If "Public options" was in edit mode, cancel any changes and return it to the read mode:
      this.publicOptionMultiView.SetActiveView( this.dispayPublicOptionView );
    }

    protected void editPublicOptionButton_Click( object sender, EventArgs e )
    {
      CheckReadOnly( );
      // If header grid was in edit mode, cancel any changes and return it to the read mode:
      this.groupHeaderGridView.EditIndex = -1;
      this.publicOptionMultiView.SetActiveView( this.editPublicOptionView );
    }

    protected void savePublicOptionChangeButton_Click( object sender, EventArgs e )
    {
      TrackerDataSetTableAdapters.GroupTableAdapter adapter = new TrackerDataSetTableAdapters.GroupTableAdapter( );

      string newIsPublicValueStr = this.publicOptionRadioButtonList.SelectedValue;
      bool newIsPublicValue;
      if ( newIsPublicValueStr == "public" )
      {
        newIsPublicValue = true;
      }
      else if ( newIsPublicValueStr == "unlisted" )
      {
        newIsPublicValue = false;
      }
      else
      {
        throw new InvalidOperationException( string.Format( "Unknown public option value '{0}'", newIsPublicValueStr ) );
      }

      this.groupTable[0].IsPublic = newIsPublicValue;
      adapter.Update( this.groupTable );
      this.publicOptionMultiView.SetActiveView( this.dispayPublicOptionView );
    }

    protected void Page_PreRender( object sender, EventArgs e )
    {
      if ( this.groupTable[0].IsPublic )
      {
        this.publicGroupPanel.Visible = true;
        this.unlistedGroupPanel.Visible = false;
      }
      else
      {
        this.publicGroupPanel.Visible = false;
        this.unlistedGroupPanel.Visible = true;
      }

      if ( this.groupTable[0].DisplayUserMessages )
      {
        this.showUserMessagesPanel.Visible = true;
        this.hideUserMessagesAsNewPanel.Visible = false;
        this.hideUserMessagesPanel.Visible = false;
      }
      else if ( Global.UserMessagesSettingIsNew )
      {
        this.showUserMessagesPanel.Visible = false;
        this.hideUserMessagesAsNewPanel.Visible = true;
        this.hideUserMessagesPanel.Visible = false;
      }
      else
      {
        this.showUserMessagesPanel.Visible = false;
        this.hideUserMessagesAsNewPanel.Visible = false;
        this.hideUserMessagesPanel.Visible = true;
      }
    }

    protected void gridReadonlyCheck_Changing( object sender, SqlDataSourceCommandEventArgs e )
    {
      CheckReadOnly( );
    }

    private void CheckReadOnly( )
    {
      if ( this.isReadOnly )
      {
        throw new InvalidOperationException( "You don't have permissions to change this group." );
      }
    }

    protected void changeGroupDetailsButton_Click( object sender, EventArgs e )
    {
      CheckReadOnly( );

      // TODO: cancel any other update

      this.headerMultiView.SetActiveView( this.headerEditView );

      this.groupNameTextBox.Text = GroupName;

      if ( this.groupTable[0].IsPublic )
        this.publicOptionRadioButtonList.SelectedValue = "public";
      else
        this.publicOptionRadioButtonList.SelectedValue = "unlisted";

      { // "setForAll" item migh be here by ViewState
        var listItems =
          this.showMessagesRadioButtonList
          .Items
          .Cast<ListItem>( );

        ListItem setForAllItem =
          listItems.FirstOrDefault(
            li =>
              li.Value == "setForAll"
          );

        if ( Global.ShowUserMessagesByDefault )
        {
          if ( setForAllItem != null )
            this.showMessagesRadioButtonList.Items.Remove( setForAllItem );
        }
        else if ( setForAllItem == null )
        {
          setForAllItem =
            new ListItem(
              "<b>SHOW</b> owner-defined messages, and do the same for <b>all other groups</b>, and make it <b>default setting</b>.",
              "setForAll"
            );

          int iShowItem =
            listItems.ToList( ).FindIndex( li => li.Value == "show" );

          if ( iShowItem < 0 ) iShowItem = listItems.Count( ) - 1;

          this.showMessagesRadioButtonList.Items.Insert( iShowItem + 1, setForAllItem );
        }

        if ( this.groupTable[0].DisplayUserMessages )
        {
          this.showMessagesRadioButtonList.SelectedValue = "show";
        }
        else
        {
          this.showMessagesRadioButtonList.SelectedValue = "hide";
        } // never set "setForAll" item here
      }
    }

    protected void updateGroupDetailsButton_Click( object sender, EventArgs e )
    {
      // group name
      this.groupTable[0].Name = this.groupNameTextBox.Text;

      { // public/unlisted
        string newIsPublicValueStr = this.publicOptionRadioButtonList.SelectedValue;

        if ( newIsPublicValueStr == "public" )
          this.groupTable[0].IsPublic = true;
        else if ( newIsPublicValueStr == "unlisted" )
          this.groupTable[0].IsPublic = false;
        else
          throw new InvalidOperationException( string.Format( "Unknown public option value '{0}'", newIsPublicValueStr ) );
      }

      bool shouldShowForAllGroups = false;
      { // User messages
        string newShowMessagesStr = this.showMessagesRadioButtonList.SelectedValue;

        shouldShowForAllGroups = newShowMessagesStr == "setForAll";

        if ( newShowMessagesStr == "show" || shouldShowForAllGroups )
          this.groupTable[0].DisplayUserMessages = true;
        else if ( newShowMessagesStr == "hide" )
          this.groupTable[0].DisplayUserMessages = false;
        else
          throw new InvalidOperationException( string.Format( "Unknown user messages option value '{0}'", newShowMessagesStr ) );
      }

      try
      {
        this.groupAdapter.Update( this.groupTable );

        this.headerMultiView.SetActiveView( this.headerDisplayView );

        if ( shouldShowForAllGroups )
        {
          TrackerDataSetTableAdapters.ProcsAdapter procAdapter = new TrackerDataSetTableAdapters.ProcsAdapter( );
          procAdapter.ShowUserMessagesInAllGroupsForUser( Global.UserId );

          Global.ShowUserMessagesByDefault = true;
        }

        if ( this.groupTable[0].DisplayUserMessages && Global.UserMessagesSettingIsNew )
        {
          Global.UserMessagesSettingIsNew = false;
        }
      }
      catch ( Exception exc )
      {
        if ( Global.IsSqlDuplicateError( exc ) )
          UpdateGroupMsg = string.Format( "You already have a group named '{0}'", this.groupNameTextBox.Text );
        else
          UpdateGroupMsg = exc.Message;
      }
    }

    protected void cancelEditGroupDetailsButton_Click( object sender, EventArgs e )
    {
      this.headerMultiView.SetActiveView( this.headerDisplayView );
    }

    protected void deleteGroup_Click( object sender, EventArgs e )
    {
      CheckReadOnly( );

      try
      {
        this.groupAdapter.DeleteById( GroupId );

        Response.Redirect( "~/default.aspx", true );
      }
      catch ( Exception exc )
      {
        UpdateGroupMsg = exc.Message;
      }
    }
  }
}