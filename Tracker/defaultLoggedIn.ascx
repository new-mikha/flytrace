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
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="defaultLoggedIn.ascx.cs"
    Inherits="FlyTrace.defaultLoggedIn" %>
<%@ Register TagPrefix="flyTrace" TagName="UserGroupsGrid" Src="~/UserGroupsGrid.ascx" %>
<%@ Register TagPrefix="flyTrace" TagName="UserEventsGrid" Src="~/UserEventsGrid.ascx" %>
<%@ Register TagPrefix="flyTrace" TagName="UserTodayTask" Src="~/UserTodayTask.ascx" %>
<div align="left">
    <asp:Panel runat="server" Visible='<%$ FtCode: FlyTrace.Global.IsAdmin %>'>
        <br />
        <div class="GroupTable2">
            <div class="VisualGroupTitle2">
                Admin Stuff
            </div>
            <div style="height: 0.5em;">
                &nbsp;
            </div>
            <div>
                <div style="border: 1px dotted gray; display: inline-block; margin-left: 1em;">
                    <div>
                        Attempts order:
                        <asp:Label ID="serviceStatShortStatus" Font-Bold="true" runat="server" ForeColor="#00CC00">all good</asp:Label>
                    </div>
                    <div style="border-top: 1px dotted gray;">
                        <asp:Table EnableViewState="false" ID="serviceStatDisplayTable" runat="server" CellSpacing="0"
                            CellPadding="3">
                        </asp:Table>
                    </div>
                </div>
                <div style="display: inline-block; margin-left: 1em; vertical-align: top">
                    <a href="administration/manageUsers.aspx">Manage Users</a><br />
                    <a href="administration/uiLogConfig.aspx">UI Log Config</a><br />
                    <a href="administration/serviceLogConfig.aspx">Service Log Config</a><br />
                    <a href="administration/currentTrackers.aspx">Current Trackers</a><br />
                    <p>
                        Admins:
                        <%= GetAdminsList() %>
                    </p>
                </div>
            </div>
            <div style="margin-left: 1em;">
                <asp:Table EnableViewState="false" ID="adminStatMessagesTable" runat="server" CellSpacing="0"
                    CellPadding="3">
                </asp:Table>
            </div>
        </div>
    </asp:Panel>
    <div>
        <br />
        <flyTrace:UserGroupsGrid runat="server" ID="userGroupsGrid" />
    </div>
    <div>
        <br />
        <asp:MultiView runat="server" ID="eventsMultiView" ActiveViewIndex="<%$ FtCode: FlyTrace.Global.IsSimpleEventsModel ? 0: 1%>">
            <asp:View ID="todayTaskView" runat="server">
                <flyTrace:UserTodayTask runat="server" ID="userTodayTask" OnEventsModelChanged="events_EventsModelChanged" />
            </asp:View>
            <asp:View ID="eventsGridView" runat="server">
                <flyTrace:UserEventsGrid runat="server" ID="userEventsGrid" OnEventsModelChanged="events_EventsModelChanged" />
            </asp:View>
        </asp:MultiView>
    </div>
    <br />
</div>
