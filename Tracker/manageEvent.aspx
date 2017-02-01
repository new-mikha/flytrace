<%--
  Flytrace, online viewer for GPS trackers.
  Copyright (C) 2011-2014 Mikhail Karmazin
  
  This file is part of Flytrace.
  
  Flytrace is free software: you can redistribute it and/or modify
  it under the terms of the GNU Affero General Public License as
  published by the Free Software Foundation, either version 3 of the
  License, or (at your option) any later version.
  
  Flytrace is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU Affero General Public License for more details.
  
  You should have received a copy of the GNU Affero General Public License
  along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="manageEvent.aspx.cs" Inherits="FlyTrace.ManageEventForm"
    StylesheetTheme="Default" ViewStateEncryptionMode="Always" %>

<%@ PreviousPageType VirtualPath="~/default.aspx" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">

<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/jquery/2.2.2/jquery.min.js"></script>
<script type="text/javascript" src="Scripts/date.format.js"></script>

<%-- 
<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/react/15.3.1/react.js"></script>
<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/react/15.3.1/react-dom.js"></script>
<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/babel-core/5.6.16/browser.js"></script>
--%>

<script type="text/javascript" src="Scripts/maintainScrollPosition.js"></script>
<script type="text/javascript">
    setScrollHiddenInputId('<%= scrollHiddenField.ClientID %>');
</script>
<head runat="server">
    <style>
        * {
            font-family: sans-serif;
            font-size: small;
        }
    </style>
    <title>
        <% 
          if (FlyTrace.Global.IsSimpleEventsModel)
          {
              Response.Write("Edit Today Task - FlyTrace");
          }
          else
          {
              Response.Write(string.Format("Edit Today Task for {0} - FlyTrace", EventName));
          }
        %>
    </title>
    
	
<script type="text/javascript">
	var _ie8_or_less = false;
</script>
	
<%--Comment below is not a real one but rather an "IE conditional comment", 
	DO NOT CHANGE OR REMOVE IT, THE CODE INSIDE ACTUALLY WORKS IN IE 8 OR LESS --%>
<!--[if lte IE 8]>
<script type="text/javascript">
	_ie8_or_less = true;
</script>
<![endif]-->
    
</head>
<body>
    <% Response.Write(string.Format("<script  type='text/javascript'>_startTsMilliseconds={0};</script>", StartTsMillisecodsString)); %>
    <form id="frmEvent" runat="server">
        <div align="left" style="width: 33em">
            <asp:HiddenField ID="scrollHiddenField" runat="server" />
            <asp:ScriptManager runat="server" />
            <script type="text/javascript" language="javascript">
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(onEndRequest);

                function onEndRequest(sender, args) {
                    if (args.get_error() != undefined) {
                        var cleanUpAgeLabel = document.getElementById("cleanUpAgeLabel");
                        if (cleanUpAgeLabel != undefined)
                            cleanUpAgeLabel.innerHTML = "";
                        args.set_errorHandled(true);
                    }
                }
            </script>
            <table style="text-align: left">
                <%--Top level row: "Go back home"--%><tr>
                    <td>
                        <table style="width: 100%;" class="UserInfo">
                            <tr>
                                <td>
                                    <a href="default.aspx">FlyTrace</a>&nbsp;&gt;
                                <asp:MultiView runat="server" ActiveViewIndex="<%$ FtCode: FlyTrace.Global.IsSimpleEventsModel ? 0: 1%>">
                                    <asp:View runat="server">
                                        Today task
                                    </asp:View>
                                    <asp:View runat="server">
                                        &#39;<%=EventName%>&#39; today task
                                    </asp:View>
                                </asp:MultiView>
                                    &nbsp;&nbsp;&nbsp;&nbsp;
                                </td>
                                <td style="text-align: right;">
                                    <b>
                                        <asp:LoginName ID="LoginName1" runat="Server"></asp:LoginName>
                                    </b>&nbsp;-&nbsp;<a href="profile.aspx">Settings</a>&nbsp;-&nbsp;<asp:LinkButton
                                        ID="SignOutLinkButton" runat="server" OnClick="SignOutLinkButton_Click">Sign out</asp:LinkButton>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
                <asp:Panel runat="server" Visible="<%$ FtCode: !FlyTrace.Global.IsSimpleEventsModel %>">
                    <%--Top-level row: "Event details"--%><tr>
                        <td>
                            <br />
                            <table width="100%" class="GroupTable">
                                <tr>
                                    <td class="VisualGroupTitle">
                                        <i>
                                            <%=EventName%></i>&nbsp;details
                                    </td>
                                </tr>
                                <tr style="display: <%= string.IsNullOrEmpty(UpdateEventMsg) ? "none" : ""%>;">
                                    <td>
                                        <div class="InfoMessage">
                                            <asp:Label ID="Label1" runat="server"><%= UpdateEventMsg %></asp:Label>
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:GridView EnableViewState="false" ID="eventHeaderGridView" runat="server" AutoGenerateColumns="False"
                                            DataKeyNames="Id" DataSourceID="eventHeaderDataSource" ShowHeader="true" GridLines="None"
                                            OnRowUpdated="eventHeaderGridView_RowUpdated">
                                            <Columns>
                                                <asp:TemplateField HeaderText="Competition or fly-in name:" HeaderStyle-HorizontalAlign="Left"
                                                    SortExpression="Name">
                                                    <ItemTemplate>
                                                        <b>
                                                            <asp:Label ID="Label2" runat="server" Text='<%# Bind("Name")%>'></asp:Label>&nbsp;
                                                        <%= IsEventDefault ? " ^" : ""%></b>
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="eventNameTextBox" runat="server" Text='<%# Bind("Name") %>'></asp:TextBox>
                                                        <asp:RequiredFieldValidator ControlToValidate="eventNameTextBox" ID="RequiredFieldValidator2"
                                                            runat="server" ErrorMessage="&lt;br /&gt;Event name cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
                                                    </EditItemTemplate>
                                                    <HeaderStyle Font-Bold="False" />
                                                </asp:TemplateField>
                                                <asp:TemplateField ShowHeader="False">
                                                    <EditItemTemplate>
                                                        &nbsp;&nbsp;
                                                    <asp:Button ID="LinkButton1" runat="server" CausesValidation="True" CommandName="Update"
                                                        Text="Update"></asp:Button>
                                                        &nbsp;<asp:Button ID="LinkButton2" runat="server" CausesValidation="False" CommandName="Cancel"
                                                            Text="Cancel"></asp:Button>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        &nbsp;&nbsp;
                                                    <asp:Button ID="LinkButton2" runat="server" CausesValidation="False" CommandName="Edit"
                                                        Text="Rename"></asp:Button>
                                                        &nbsp;&nbsp;&nbsp;
                                                    <asp:Button ID="LinkButton1" runat="server" CausesValidation="False" CommandName="Delete"
                                                        Text="Delete the whole event" OnClientClick="<%$ Resources: Resources, EventDeleteConfirmationFunc %>"></asp:Button>
                                                    </ItemTemplate>
                                                    <ItemStyle VerticalAlign="Top" />
                                                </asp:TemplateField>
                                            </Columns>
                                            <%--<RowStyle CssClass="RowStyle" />--%>
                                        </asp:GridView>
                                        <asp:SqlDataSource ID="eventHeaderDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>"
                                            SelectCommand="SELECT [Id], [Name], [IsDefault] FROM [EventView] WHERE ([Id] = @Id)"
                                            UpdateCommand="UPDATE [Event] SET [Name] = @Name WHERE [Id] = @Id" DeleteCommand="EXEC DeleteEvent @Id"
                                            OnUpdated="eventHeaderDataSource_Updated" OnDeleted="eventHeaderDataSource_Deleted">
                                            <SelectParameters>
                                                <flytrace_tools:EvalParameter Name="Id" Type="Int32" Expression="EventId" />
                                            </SelectParameters>
                                            <UpdateParameters>
                                                <asp:Parameter Name="Name" Type="String" />
                                                <asp:Parameter Name="Id" Type="Int32" />
                                            </UpdateParameters>
                                            <DeleteParameters>
                                                <asp:Parameter Name="Id" Type="Int32" />
                                            </DeleteParameters>
                                        </asp:SqlDataSource>
                                        <span style="font-size: small">
                                            <%= IsEventDefault ? "^ New pilot groups will use this event by default" : ""%>
                                        </span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </asp:Panel>
                <%--<RowStyle CssClass="RowStyle" />--%>
                <asp:Panel runat="server" ID="loadWaypointsPanel">
                    <tr>
                        <td>
                            <br />
                            <table width="100%" class="GroupTable">
                                <tr>
                                    <td style="background-color: #FFFF66" class="VisualGroupTitle">Load waypoints first
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <span style='<%= string.IsNullOrEmpty(LoadWaypointsMsg) ? "color: #FF3300": ""%>;'>Before
                                        setting up a task, waypoints for the event<br />
                                            should be loaded (or added manually)</span>
                                    </td>
                                </tr>
                                <tr style='display: <%= string.IsNullOrEmpty(LoadWaypointsMsg) ? "none" : ""%>;'>
                                    <td>
                                        <span style='color: #FF3300;'>
                                            <%=LoadWaypointsMsg%>
                                        </span>
                                    </td>
                                </tr>
                                <tr>
                                    <td>This should be Google Earth (*.KML) or SeeYou (*.CUP) file:<br />
                                        <asp:FileUpload ID="fileUpload" runat="server" onchange="document.getElementById('uploadLbl').style.display = ''; __doPostBack('fileUpload','');" />
                                        <p id="uploadLbl" class="InfoMessage" style="display: none">
                                            <asp:Image ID="Image1" runat="server" ImageUrl="~/App_Themes/Default/hourglass.gif" />
                                            Uploading &amp; processing the file, please wait...
                                        </p>
                                        <p>
                                            &nbsp;&nbsp;&nbsp;-&nbsp;OR&nbsp;-
                                        </p>
                                        <p>
                                            <a href='manageWaypoints.aspx?event=<%=EventId%>'>Add the waypoints manually one by
                                            one...</a>
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </asp:Panel>
                <!-- Top-level row: "Today Task" -->
                <tr>
                    <td>
                        <br />
                        <table width="100%" class="GroupTable">
                            <tr>
                                <td class="VisualGroupTitle">Today Task
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <b>
                                        <%=AllEventWptsCount%></b> waypoints are available for the task.<br />
                                    <asp:Panel runat="server" Visible="<%$ FtCode: FlyTrace.Global.IsSimpleEventsModel %>">
                                        After you save the task below it will be displayed on your maps.
                                    </asp:Panel>
                                    <asp:Panel runat="server" ID="noWaypointsInfoPanel">
                                        <span style="color: #FF3300;">To set up a task, add waypoints first (see above)</span><br />
                                    </asp:Panel>
                                    <asp:Panel runat="server" ID="taskPanel">
                                        <br />
                                        <a href='manageWaypoints.aspx?event=<%=EventId%>'>Manage available waypoints (add
                                        new waypoints, edit etc)...</a>
                                        <br />
                                        <hr />
                                        <div id="react-waypoints"><i>Loading task, please wait...</i>
										</div>
                                    </asp:Panel>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
                <tr>
                    <td>
                        <br />
                        <table width="100%" class="GroupTable">
                            <tr>
                                <td class="VisualGroupTitle">Old points removal (optional)
                                </td>
                            </tr>
                            <tr>
                                <td style="text-align: justify;">
                                    <div id="react-old-task-clean-up-controls"><i>Loading content, please wait...</i></div>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
                <asp:Panel ID="groupAssignmentStuff" runat="server" Visible="<%$ FtCode: !FlyTrace.Global.IsSimpleEventsModel %>">
                    <asp:Panel runat="server" ID="noGroupWarningPanel" Visible="false">
                        <tr>
                            <td>
                                <br />
                                <table width="450px" class="GroupTable">
                                    <tr>
                                        <td class="WarningGroupTitle">Task is not displayed on any map yet
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>There are no pilots groups yet that are assigned to the task. You can assign pilot
                                        groups one-by-one or press the button below, and assigned groups' maps will display
                                        the task.
                                        <br />
                                            <br />
                                            <asp:Button ID="LinkButton4" runat="server" OnClick="assignGroupsButton_Click" Text="Assign this task to the pilot groups" /><br />
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </asp:Panel>
                    <asp:MultiView runat="server" ID="groupsPanelMultiView" ActiveViewIndex="0">
                        <asp:View ID="defaultGroupPanelView" runat="server">
                            <tr>
                                <td>
                                    <br />
                                    <asp:LinkButton runat="server" ID="showGroupsButton" Text="Show more settings for the event"
                                        OnClick="showHideGroupsButton_Click" />
                                </td>
                            </tr>
                        </asp:View>
                        <asp:View ID="expandedGroupPanelView" runat="server">
                            <tr>
                                <td>
                                    <br />
                                    <table width="100%" class="GroupTable">
                                        <tr>
                                            <td class="VisualGroupTitle">Pilot groups that use this task
                                            <asp:LinkButton runat="server" ID="LinkButton3" Text="(hide)" OnClick="showHideGroupsButton_Click" />
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:MultiView runat="server" ID="defaultEventControlsMultiView" ActiveViewIndex="0">
                                                    <asp:View ID="defaultEventReadView" runat="server">
                                                        Is the event default for new groups: <b>
                                                            <%= IsEventDefault ? "Yes" : "No" %></b>
                                                        <asp:Button runat="server" ID="defaultEditButton" Text="Change" OnClick="defaultButton_Click" />
                                                    </asp:View>
                                                    <asp:View ID="defaultEventEditView" runat="server">
                                                        <asp:CheckBox runat="server" ID="defaultCheckBox" Text="Is default event" />
                                                        <asp:Button ID="defaultSaveButton" runat="server" Text="Update" OnClick="defaultButton_Click" />
                                                        <asp:Button ID="defaultEditCancelButton" runat="server" Text="Cancel" OnClick="defaultButton_Click" />
                                                    </asp:View>
                                                </asp:MultiView>
                                                <br />
                                                <br />
                                                <asp:MultiView runat="server" ID="assignedTaskMultiView">
                                                    <asp:View ID="assignedTaskEmptyView" runat="server">
                                                        No Pilot Group is assigned to the event yet.
                                                    <asp:Button ID="assignGroupsLinkButton" runat="server" OnClick="assignGroupsButton_Click"
                                                        Text="Assign groups" />.
                                                    </asp:View>
                                                    <asp:View ID="assignedTaskReadView" runat="server">
                                                        Following maps display the task when Today Task above is filled &amp; saved:
                                                    <asp:SqlDataSource ID="assignedGroupsDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>"
                                                        SelectCommand="SELECT G.*, 
                                        (SELECT COUNT(*) FROM GroupTracker GT WHERE GT.GroupId = G.Id) AS TrackersCount 
                                       FROM [Group] G 
                                       WHERE (G.UserId = @UserId) AND (G.EventId = @EventId) ORDER BY G.[Name]">
                                                        <SelectParameters>
                                                            <asp:SessionParameter Name="UserId" SessionField="UserId" />
                                                            <flytrace_tools:EvalParameter Name="EventId" Type="Int32" Expression="EventId" />
                                                        </SelectParameters>
                                                    </asp:SqlDataSource>
                                                        <asp:GridView EnableViewState="false" Width="100%" ID="assignedGroupsGridView" runat="server"
                                                            AutoGenerateColumns="False" DataSourceID="assignedGroupsDataSource" DataKeyNames="Id"
                                                            GridLines="None" AllowSorting="True" OnDataBound="assignedGroupsGridView_DataBound">
                                                            <Columns>
                                                                <asp:TemplateField HeaderText="Name" SortExpression="Name">
                                                                    <ItemTemplate>
                                                                        <asp:Label ID="Label1" runat="server" Text='<%# Bind("Name") %>'></asp:Label>
                                                                    </ItemTemplate>
                                                                    <HeaderStyle HorizontalAlign="Left" />
                                                                </asp:TemplateField>
                                                                <asp:TemplateField>
                                                                    <ItemTemplate>
                                                                        |
                                                                    </ItemTemplate>
                                                                </asp:TemplateField>
                                                                <asp:BoundField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center"
                                                                    DataField="TrackersCount" HeaderText="Pilots" SortExpression="TrackersCount">
                                                                    <HeaderStyle HorizontalAlign="Left"></HeaderStyle>
                                                                    <ItemStyle HorizontalAlign="Center"></ItemStyle>
                                                                </asp:BoundField>
                                                                <asp:TemplateField>
                                                                    <ItemTemplate>
                                                                        |
                                                                    </ItemTemplate>
                                                                </asp:TemplateField>
                                                                <asp:TemplateField ShowHeader="False">
                                                                    <ItemTemplate>
                                                                        <a href='manageGroup.aspx?group=<%# Eval("Id")%>'>'<%# Eval("Name") %>' details</a>&nbsp;&nbsp;&nbsp;&nbsp;
                                                                    <a href='map.aspx?group=<%# Eval("Id")%>' target="_blank">Map link</a>
                                                                    </ItemTemplate>
                                                                </asp:TemplateField>
                                                            </Columns>
                                                            <AlternatingRowStyle CssClass="AlternatingRowStyle" />
                                                            <RowStyle CssClass="RowStyle" />
                                                        </asp:GridView>
                                                        <asp:Button ID="assignOtherGroupsBottomButton" OnClick="assignGroupsButton_Click"
                                                            Text="Change" runat="server" />
                                                    </asp:View>
                                                    <asp:View ID="assignedTaskEditView" runat="server">
                                                        <div style="height: 8px;">
                                                        </div>
                                                        Check the Pilot Groups that you want to display the task:
                                                    <asp:SqlDataSource ID="assignedGroupsEditDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>"
                                                        SelectCommand="
                                                            SELECT G.Id,
	                                                        (SELECT COUNT(*) FROM GroupTracker GT WHERE GT.GroupId = G.Id) AS TrackersCount ,
	                                                        CASE WHEN G.EventId IS NULL THEN
		                                                        G.Name + ' (no event yet)'
	                                                        ELSE
		                                                        G.Name
	                                                        END AS GroupNameWithEventFlag
                                                            FROM [Group] G 
                                                            WHERE (G.UserId = @UserId) ORDER BY G.[Name]">
                                                        <SelectParameters>
                                                            <asp:SessionParameter Name="UserId" SessionField="UserId" />
                                                        </SelectParameters>
                                                    </asp:SqlDataSource>
                                                        <asp:CheckBoxList ID="assignedGroupsCheckBoxList" runat="server" DataSourceID="assignedGroupsEditDataSource"
                                                            DataValueField="Id" DataTextField="GroupNameWithEventFlag" OnDataBound="assignedGroupsCheckBoxList_DataBound">
                                                        </asp:CheckBoxList>
                                                        <div style="height: 10px;">
                                                        </div>
                                                        <asp:Button ID="assignGroupsSaveButton" OnClick="assignGroupsButton_Click" Text="Save changes to the groups assignment"
                                                            runat="server" />
                                                        <asp:Button ID="assignGroupsEditCancelButton" OnClick="assignGroupsButton_Click"
                                                            Text="Cancel" runat="server" />
                                                    </asp:View>
                                                </asp:MultiView>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </asp:View>
                    </asp:MultiView>
                </asp:Panel>
            </table>
        </div>
    </form>
</body>
<script type="text/javascript">
	if(_ie8_or_less) {
		document.getElementById('react-waypoints').innerHTML = "<b>Can't load task - Internet Explorer 8 or less is not supported</b>";
		document.getElementById('react-old-task-clean-up-controls').innerHTML = "<b>Can't load contents - Internet Explorer 8 or less is not supported</b>";
	}
</script>
<script type="text/javascript" src="Scripts/manageEvent.js"></script>
</html>
