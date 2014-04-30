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

<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UserTodayTask.ascx.cs" Inherits="FlyTrace.UserTodayTask" %>
<div class="GroupTable2">
    <div class="VisualGroupTitle2">
        Today Task (optional)
    </div>
    <div>
        Here you can manage competiton waypoints, and set up a task then to display on your public maps <a href="help/eventshelp.htm" target="_blank">
            <asp:Image ID="Image1" runat="server" ImageUrl="~/App_Themes/Default/siteHelp.png" /></a><br />
        <br />
        <asp:MultiView ID="waypointsMultiView" runat="server" ActiveViewIndex="0">
            <asp:View ID="noGroupAtAll" runat="server">
                But first create a Pilot Group above.
            </asp:View>
            <asp:View ID="waypointsNotLoadedView" runat="server">
                <span>Competition waypoints not loaded yet.</span>
                <br />
                <asp:LinkButton ID="LinkButton1" runat="server" Text="Load waypoints now (optional)..." OnClick="editWptsButton_Click" />
            </asp:View>
            <asp:View ID="waypointsLoadedView" runat="server">
                <b>
                    <%= FlyTrace.Global.DefEventLoadedWptsCount %></b> competition waypoint(s) loaded.<br />
                <asp:LinkButton runat="server" Text="View/edit all Waypoints..." OnClick="editWptsButton_Click" />
            </asp:View>
        </asp:MultiView>
        <asp:MultiView ID="taskMultiView" runat="server" ActiveViewIndex="0">
            <%--                <asp:View ID="taskSetupDisabledView" runat="server">
                    There is no task at the moment. Before a task can be set, waypoints<br />
                    should be loaded using the link above.
                </asp:View>--%>
            <asp:View ID="taskNotSetView" runat="server">
                <br />
                <br />
                There is no task at the moment.<br />
                <asp:LinkButton runat="server" Text="Set up a task..." OnClick="editTaskButton_Click" />
            </asp:View>
            <asp:View ID="taskSetupView" runat="server">
                <br />
                <br />
                Task set with <b>
                    <%= FlyTrace.Global.DefEventTaskWptsCount %></b> waypoint(s)
                <asp:Label runat="server" ID="oneWptWarningLabel">, so it's an empty task. <br /> Add more waypoints to the task to show it on your map(s)</asp:Label>.<br />
                <asp:LinkButton ID="editTaskButton" runat="server" Text="View/edit Today Task" OnClick="editTaskButton_Click" />
                <br />
                <br />
                <div style="font-size: smaller">
                    Having more than one competition at a time?<br />
                    Switch to the
                    <asp:LinkButton ID="switchToAdvButton" runat="server" OnClick="switchToAdvButton_Click">Multiple Events mode</asp:LinkButton>
                    then.
                </div>
            </asp:View>
        </asp:MultiView>
    </div>
    <div>
        <asp:Panel runat="server" Visible="<%$ FtCode: false && Request.IsLocal %>">
            <br />
            <br />
            <p style="font-size: x-small">
                <asp:LinkButton runat="server" OnClick="switchToAdvButton_Click">Multiple Events mode</asp:LinkButton>
            </p>
        </asp:Panel>
    </div>
</div>