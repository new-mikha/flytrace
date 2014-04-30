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

<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UserEventsGrid.ascx.cs" Inherits="FlyTrace.UserEventsGrid" %>
<div class="GroupTable2">
    <div class="VisualGroupTitle2">
        (optional) My Waypoints&nbsp;&amp;&nbsp;Tasks
    </div>
    <div>
        You can set up events here to show tasks on your public maps <a href="help/eventshelp.htm" target="_blank">
            <asp:Image ID="Image1" runat="server" ImageUrl="~/App_Themes/Default/siteHelp.png" /></a>
        <br />
        <br />
        <asp:GridView EnableViewState="False" Width="100%" ID="eventsGridView" runat="server" AutoGenerateColumns="False" DataSourceID="userEventsDataSource" DataKeyNames="Id" GridLines="None" AllowSorting="True" OnDataBound="eventsGridView_DataBound" OnRowDataBound="eventsGridView_RowDataBound">
            <EmptyDataTemplate>
                <asp:MultiView ID="noEventMultiView" runat="server" EnableViewState="false" ActiveViewIndex="1">
                    <asp:View ID="groupAreEmptyView" runat="server">
                        But first create a Pilot Group above.
                    </asp:View>
                    <asp:View ID="groupAreNotEmptyView" runat="server">
                        <asp:LinkButton ID="createEventLinkButton" runat="server" OnClick="createEventLinkButton_Click">Create first event...</asp:LinkButton>
                    </asp:View>
                </asp:MultiView>
            </EmptyDataTemplate>
            <Columns>
                <asp:TemplateField HeaderText="Event Name" SortExpression="Name">
                    <ItemTemplate>
                        <asp:Label Font-Bold='<%# Bind("IsDefault")%>' ID="Label1" runat="server" Text='<%# Eval("Name") + ((bool)Eval("IsDefault") ? " *" : "")%>'></asp:Label>
                    </ItemTemplate>
                    <HeaderStyle HorizontalAlign="Left" />
                </asp:TemplateField>
                <asp:TemplateField>
                    <ItemTemplate>
                        |
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center" DataField="WaypointsCount" HeaderText="Wpts" SortExpression="WaypointsCount">
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
                        <a href='manageEvent.aspx?event=<%# Eval("Id")%>'>Edit</a> &nbsp;&nbsp;&nbsp;&nbsp;<asp:LinkButton ID="LinkButton3" runat="server" CausesValidation="False" CommandName="Delete" Text="Delete" OnClientClick="<%$ Resources: Resources, EventDeleteConfirmationFunc %>"></asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <AlternatingRowStyle CssClass="AlternatingRowStyle" />
            <RowStyle CssClass="RowStyle" />
        </asp:GridView>
        <asp:SqlDataSource ID="userEventsDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>" SelectCommand="SELECT E.*, 
                                (SELECT COUNT(*) FROM Waypoint W WHERE W.EventId=E.Id) AS WaypointsCount 
                               FROM [EventView] E 
                                    LEFT OUTER JOIN UserProfile P ON 
                                        E.Id = P.DefaultEventId AND 
                                        E.UserId = P.UserId
                               WHERE (E.UserId = @UserId)" InsertCommand="EXEC CreateEvent @UserId, @NewEventId OUTPUT" UpdateCommand="UPDATE [Event] SET Name = @Name WHERE Id=@Id" DeleteCommand="EXEC DeleteEvent @Id" OnInserted="userEventsDataSource_Inserted">
            <SelectParameters>
                <asp:SessionParameter Name="UserId" SessionField="UserId" />
            </SelectParameters>
            <DeleteParameters>
                <asp:Parameter Name="Id" />
            </DeleteParameters>
            <UpdateParameters>
                <asp:Parameter Name="Name" />
                <asp:Parameter Name="Id" />
            </UpdateParameters>
            <InsertParameters>
                <asp:SessionParameter Name="UserId" SessionField="UserId" />
                <asp:Parameter Name="NewEventId" Direction="Output" Type="Int32" />
            </InsertParameters>
        </asp:SqlDataSource>
        <asp:Panel ID="createEventPanel" runat="server" Visible="false" EnableViewState="false">
            <asp:LinkButton ID="createEventLinkButton" runat="server" OnClick="createEventLinkButton_Click">Create new event...</asp:LinkButton>
        </asp:Panel>
        <asp:Panel ID="defaultAnnotationPanel" Visible="false" runat="server" EnableViewState="false">
            <span style="font-size: small">____
                <br />
                *&nbsp;New pilot groups will use this event by default </span>
        </asp:Panel>
        <asp:Panel ID="switchToSingleEventModePanel" Visible="false" runat="server" EnableViewState="false">
            <div style="font-size: smaller">
                <br />
                Multiple events mode looks too complicated?<br />
                Switch to the
                <asp:LinkButton ID="switchToSimpleButton" runat="server" OnClick="switchToSimpleButton_Click">Single Task mode</asp:LinkButton>
                then.
            </div>
        </asp:Panel>
    </div>
</div>