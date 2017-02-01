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
using System.Web.Security;
using System.Linq;
using System.Web;
using FlyTrace.Tools;

namespace FlyTrace
{
  public partial class ManageEventForm : System.Web.UI.Page
  {
    protected string UpdateEventMsg = "";
    protected string LoadWaypointsMsg = "";

    protected int AllEventWptsCount => _waypointsBundle?.EventWaypoints?.Length ?? 0;

    private TrackerDataSet.EventRow _eventRow;

    protected string EventName => _eventRow.Name;

    // need to be a public property because accessed by EvalParameter - it can't 
    // access non-public stuff, and requires a property rather than a field.
    public int EventId { get; private set; }

    public bool IsEventDefault { get; private set; }

    private WaypointsProvider.WaypointsBundle _waypointsBundle;

    private int _assignedGroupsCount;

    protected void Page_PreInit(object sender, EventArgs e)
    {
      string strEventId = Request.QueryString["event"];
      int eventId;

      if (strEventId != null && int.TryParse(strEventId, out eventId))
      {
        // To make sure that there is no hacking, check that the event 
        // belongs to the current user before setting this.EventId property:
        if (Global.IsAuthenticated)
        {
          TrackerDataSetTableAdapters.EventTableAdapter eventAdapter = new TrackerDataSetTableAdapters.EventTableAdapter();
          TrackerDataSet.EventDataTable eventTable = eventAdapter.GetDataByEventId(eventId);
          if (eventTable.Count > 0)
          {
            if (Global.UserId == eventTable[0].UserId)
            {
              _eventRow = eventTable[0];
              EventId = _eventRow.Id;
              IsEventDefault = _eventRow.IsDefault;
            }
          }
        }
      }

      if (EventId == 0)
      {
        Response.Redirect("~/default.aspx", true);
      }
      else if (Global.IsSimpleEventsModel && !_eventRow.IsDefault)
      {
        Response.Redirect("~/default.aspx", true);
      }
      else
      {
        LoadAllWaypoints();

        TrackerDataSetTableAdapters.GroupTableAdapter groupTableAdapter = new TrackerDataSetTableAdapters.GroupTableAdapter();
        _assignedGroupsCount =
          groupTableAdapter.GetAssignedGroupsCount(Global.UserId, EventId) ?? 0;

        if (!IsPostBack)
        {
          SetAssignedGroupsReadView();
          SetNoGroupWarningVisibility();
        }
      }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
      if (fileUpload.HasFile)
      {
        try
        {
          int updatedRecord = WaypointsLoader.LoadWaypoints(EventId, fileUpload, true);
          if (updatedRecord == 0)
          {
            LoadWaypointsMsg = "No waypoints found in the uploaded file.";
          }
          LoadAllWaypoints(); // we need to update eventsBundle after file load
          SetNoGroupWarningVisibility();
        }
        catch (Exception exc)
        {
          LoadWaypointsMsg = "Error in processing the uploaded file:<br />" + exc.Message;
        }
      }

      Page.Response.Cookies.Add(new HttpCookie("flytrace_event_cache_track_" + EventId, "0"));
    }

    private void LoadAllWaypoints()
    {
      var waypointsProvider = new WaypointsProvider();
      _waypointsBundle = waypointsProvider.GetWaypointsBundle(EventId);
    }

    private void SetNoGroupWarningVisibility()
    {
      noGroupWarningPanel.Visible =
        AllEventWptsCount > 0 &&
        _assignedGroupsCount == 0;
    }

    private void SetAssignedGroupsReadView()
    {
      assignedTaskMultiView.SetActiveView(
        _assignedGroupsCount == 0
        ? assignedTaskEmptyView
        : assignedTaskReadView);
    }

    protected void SignOutLinkButton_Click(object sender, EventArgs e)
    {
      Response.Clear();
      FormsAuthentication.SignOut();
      Response.Redirect("~/default.aspx", true);
    }

    protected void eventHeaderDataSource_Updated(object sender, SqlDataSourceStatusEventArgs e)
    {
      if (e.Exception != null)
      {
        string msg = Global.IsSqlDuplicateError(e.Exception)
          ? $"Event '{e.Command.Parameters["@Name"].Value}' already exists"
          : e.Exception.Message;

        e.ExceptionHandled = true;
        UpdateEventMsg = msg;
      }
    }

    protected void eventHeaderDataSource_Deleted(object sender, SqlDataSourceStatusEventArgs e)
    {
      if (e.Exception == null)
      {
        Response.Redirect("~/default.aspx", true);
      }
      else
      {
        UpdateEventMsg = e.Exception.Message;
        e.ExceptionHandled = true;
      }
    }

    protected void eventHeaderGridView_RowUpdated(object sender, GridViewUpdatedEventArgs e)
    {
      if (!string.IsNullOrEmpty(UpdateEventMsg))
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
        _eventRow.Name = e.NewValues["Name"] as string;
        _eventRow.AcceptChanges(); // DB record has already been updated, so we don't need this record as 'Changed'
      }
    }

    protected void showHideGroupsButton_Click(object sender, EventArgs e)
    {
      groupsPanelMultiView.SetActiveView(
        groupsPanelMultiView.GetActiveView() == defaultGroupPanelView
        ? expandedGroupPanelView
        : defaultGroupPanelView
      );
    }

    protected void defaultButton_Click(object sender, EventArgs e)
    {
      if (defaultEventControlsMultiView.GetActiveView() == defaultEventReadView)
      {
        defaultEventControlsMultiView.SetActiveView(defaultEventEditView);
        defaultCheckBox.Checked = IsEventDefault;

        SetAssignedGroupsReadView();
      }
      else
      {
        if (sender == defaultSaveButton)
        {
          // We can't just set this.eventRow.IsDefault and call adapter.Update, because there is more 
          // complicated DB logic in setting this value, which is done in SetEventDefaultFlag. Sp
          // call that stored proc here:
          bool isDefaultValue = defaultCheckBox.Checked;
          TrackerDataSetTableAdapters.ProcsAdapter procAdapters = new TrackerDataSetTableAdapters.ProcsAdapter();

          if (defaultCheckBox.Checked)
            procAdapters.SetEventAsDefault(EventId);
          else
            procAdapters.SetEventAsNonDefault(EventId);

          // We need to update this.IsEventDefault that is used in the markup
          IsEventDefault = isDefaultValue;
        }

        // If "groups assignment" was in edit mode, cancel any changes and return it to the read mode:
        defaultEventControlsMultiView.SetActiveView(defaultEventReadView);
      }
    }

    protected void assignGroupsButton_Click(object sender, EventArgs e)
    {
      if (groupsPanelMultiView.GetActiveView() == defaultGroupPanelView)
      {
        groupsPanelMultiView.SetActiveView(expandedGroupPanelView);
      }

      if (assignedTaskMultiView.GetActiveView() != assignedTaskEditView)
      {
        assignedTaskMultiView.SetActiveView(assignedTaskEditView);

        // If "default Event controls" was in edit mode, cancel any changes and return it to the read mode:
        defaultEventControlsMultiView.SetActiveView(defaultEventReadView);
      }
      else
      {
        bool isChanged = false;
        if (sender == assignGroupsSaveButton)
        {
          TrackerDataSetTableAdapters.GroupTableAdapter groupTableAdapter = new TrackerDataSetTableAdapters.GroupTableAdapter();
          TrackerDataSet.GroupDataTable userGroups = groupTableAdapter.GetDataByUserId(Global.UserId);

          int tempAssignedGroupsCount = 0;
          foreach (ListItem li in assignedGroupsCheckBoxList.Items)
          {
            int groupId = int.Parse(li.Value);
            TrackerDataSet.GroupRow groupRow = userGroups.FindById(groupId);

            // Note that the content of assignedGroupsCheckBoxList came from the ViewState, and the group 
            // can be actually deleted by that time (or replaced in ViewState by another user's group). So check 
            // if it's valid group to prevent NullReferenceExcpetion:
            if (groupRow == null)
              continue;

            if (li.Selected)
            {
              tempAssignedGroupsCount++;
              if (groupRow.IsEventIdNull() ||
                   groupRow.EventId != EventId)
                groupRow.EventId = EventId;
            }
            else
            {
              if (!groupRow.IsEventIdNull() &&
                   groupRow.EventId == EventId)
              {
                groupRow.SetEventIdNull();
              }
            }
          }

          int updatedRecords = groupTableAdapter.Update(userGroups);
          _assignedGroupsCount = tempAssignedGroupsCount;
          isChanged = updatedRecords > 0;
          if (isChanged)
          {
            TrackerDataSetTableAdapters.ProcsAdapter procsAdapter = new TrackerDataSetTableAdapters.ProcsAdapter();
            procsAdapter.SetEventAsDefault(EventId);

            // We just set event as default in the DB - now we need to update this.IsEventDefault that is used in the markup
            IsEventDefault = true;
          }
        }

        SetAssignedGroupsReadView();
        SetNoGroupWarningVisibility();
        if (isChanged)
        {
          assignedGroupsGridView.DataBind();
        }
      }
    }

    protected void assignedGroupsCheckBoxList_DataBound(object sender, EventArgs e)
    {
      TrackerDataSetTableAdapters.GroupTableAdapter groupTableAdapter = new TrackerDataSetTableAdapters.GroupTableAdapter();
      TrackerDataSet.GroupDataTable assignedGroups = groupTableAdapter.GetAssignedGroups(Global.UserId, EventId);

      foreach (ListItem li in assignedGroupsCheckBoxList.Items)
      {
        int groupId = int.Parse(li.Value);
        li.Selected = assignedGroups.FindById(groupId) != null;
      }
    }

    protected void assignedGroupsGridView_DataBound(object sender, EventArgs e)
    {
      assignedGroupsGridView.AllowSorting = assignedGroupsGridView.Rows.Count > 1;
    }

    protected string StartTsMillisecodsString
    {
      get
      {
        if (_waypointsBundle.StartTsMilliseconds == null)
          return "null";

        return _waypointsBundle.StartTsMilliseconds.Value.ToString();
      }
    }

    protected void Page_PreRender(object sender, EventArgs e)
    {
      string eventWaypointsJsArr =
        string.Join(
          ", ",
          _waypointsBundle
          .EventWaypoints
          .Select(wpt => $"{{id: {wpt.Id}, name: '{wpt.Name}'}}")
        );
      Page.ClientScript.RegisterArrayDeclaration("_eventWaypoints", eventWaypointsJsArr);


      string taskWaypointsJsArr =
        string.Join(
          ", ",
          _waypointsBundle
          .TaskWaypoints
          .Select(wpt => $"{{id: {wpt.Id}, radius: '{wpt.Radius}'}}")
        );
      Page.ClientScript.RegisterArrayDeclaration("_taskWaypoints", taskWaypointsJsArr);

      // The code below can't be in Page_Load because uploadBtn_Click is fired after that, and 
      // we need all the just inserted wpts when we fill comboboxes and decide whether to show taskPanel or not.
      // At the same time, we can't do that in uploadBtn_Click because we need the same stuff every time page loads.
      // So do it here, after all other events has fired.

      loadWaypointsPanel.Visible = noWaypointsInfoPanel.Visible = AllEventWptsCount == 0;
      taskPanel.Visible = !loadWaypointsPanel.Visible;
    }

  }
}