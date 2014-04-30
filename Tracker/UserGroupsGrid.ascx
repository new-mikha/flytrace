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

<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UserGroupsGrid.ascx.cs" Inherits="FlyTrace.UserGroupsGrid" %>
<asp:SqlDataSource ID="allUserGroupsDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>" SelectCommand="<%$ Resources: Resources, SelectUserGroupsSql %>" InsertCommand="EXEC CreateTrackersGroup @UserId, @NewGroupId OUTPUT" UpdateCommand="UPDATE [Group] SET Name = @Name WHERE Id=@Id" DeleteCommand="DELETE FROM [Group] WHERE (Id = @Id)" OnInserted="allUserGroupsDataSource_Inserted" OnDeleting="allUserGroupsDataSource_Deleting">
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
        <asp:Parameter Name="NewGroupId" Direction="Output" Type="Int32" />
    </InsertParameters>
</asp:SqlDataSource>
<div class="GroupTable2">
    <div class="VisualGroupTitle2">
        My Pilot Groups
    </div>
    <div>
        <asp:GridView EnableViewState="False" Width="100%" ID="groupsGridView" runat="server" AutoGenerateColumns="False" DataSourceID="allUserGroupsDataSource" DataKeyNames="Id" GridLines="None" AllowSorting="True" OnDataBound="groupsGridView_DataBound" OnRowDataBound="groupsGridView_RowDataBound">
            <EmptyDataTemplate>
                You don't have Pilot Groups yet.
                <asp:LinkButton runat="server" OnClick="createGroupLinkButton_Click" Text="Create the first one..." />
            </EmptyDataTemplate>
            <Columns>
                <asp:TemplateField HeaderText="Name" SortExpression="Name">
                    <EditItemTemplate>
                        <asp:TextBox ID="TextBox1" runat="server" Text='<%# Bind("Name") %>'></asp:TextBox>
                    </EditItemTemplate>
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
                <asp:BoundField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center" DataField="TrackersCount" HeaderText="Pilots" SortExpression="TrackersCount"></asp:BoundField>
                <asp:TemplateField>
                    <ItemTemplate>
                        |
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center" HeaderText="Updated" SortExpression="NewestCoordTs" Visible="true">
                    <ItemTemplate>
                        <asp:Panel runat="server" ID="agePanel" Visible="false" EnableViewState="false">
                            <asp:Label runat="server" ID="ageLabel" EnableViewState="false" Font-Bold="true" />
                            <br />
                        </asp:Panel>
                        <asp:Label runat="server" ID="updateTsLabel" EnableViewState="false" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField Visible="true">
                    <ItemTemplate>
                        |
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderStyle-HorizontalAlign="Center" HeaderText='Task' Visible='<%$ FtCode: ShouldShowTasksColumn %>' ItemStyle-HorizontalAlign="Center" SortExpression="EventName">
                    <ItemTemplate>
                        <asp:Panel Style="display: inline;" ID="Panel1" runat="server" Visible='<%# ((bool)Eval("IsSimpleEventsModel"))%>'>
                            <%-- Simple events mode panel --%>
                            <asp:Label runat="server" ForeColor="Gray" ID="Label3" Visible='<%# Eval("EventId") is System.DBNull  %>'>No</asp:Label>
                            <asp:Label runat="server" ID="Label4" Text='Yes' Visible='<%# Eval("EventId") != System.DBNull.Value %>' ForeColor='<%# (int)Eval("TaskWptCount") < 2 ? System.Drawing.Color.Gray : System.Drawing.Color.Empty %>' />
                        </asp:Panel>
                        <asp:Panel Style="display: inline;" runat="server" Visible='<%# !((bool)Eval("IsSimpleEventsModel"))%>'>
                            <%-- Multiple events mode panel --%>
                            <asp:Label runat="server" ForeColor="Gray" ID="noEventLbl" Visible='<%# Eval("EventId") is System.DBNull  %>'>none</asp:Label>
                            <asp:Label runat="server" ID="Label2" Text='<%# "yes: " + Eval("EventName") %>' ForeColor='<%# (int)Eval("TaskWptCount") < 2 ? System.Drawing.Color.Gray : System.Drawing.Color.Empty %>' Visible='<%# Eval("EventId") != System.DBNull.Value %>' />
                        </asp:Panel>
                        <asp:Label runat="server" ID="Label5" Visible='<%# Eval("EventId") != System.DBNull.Value && (int)Eval("TaskWptCount") < 2 %>'>^</asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField Visible='<%$ FtCode: ShouldShowTasksColumn %>'>
                    <ItemTemplate>
                        |
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center" DataField="ViewsNum" HeaderText="Views" SortExpression="ViewsNum">
                    <HeaderStyle HorizontalAlign="Left"></HeaderStyle>
                    <ItemStyle HorizontalAlign="Center"></ItemStyle>
                </asp:BoundField>
                <asp:TemplateField>
                    <ItemTemplate>
                        |
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center" DataField="PageUpdatesNum" HeaderText="Updates" SortExpression="PageUpdatesNum" Visible='<%$ FtCode: FlyTrace.Global.IsAdmin %>'>
                    <HeaderStyle HorizontalAlign="Left"></HeaderStyle>
                    <ItemStyle HorizontalAlign="Center"></ItemStyle>
                </asp:BoundField>
                <asp:TemplateField Visible='<%$ FtCode: FlyTrace.Global.IsAdmin %>'>
                    <ItemTemplate>
                        |
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField ShowHeader="False">
                    <ItemTemplate>
                        <a href='manageGroup.aspx?group=<%# Eval("Id")%>'>Edit</a> &nbsp;&nbsp;&nbsp;&nbsp;<asp:LinkButton ID="LinkButton3" runat="server" CausesValidation="False" CommandName="Delete" Text="Delete" OnClientClick="return confirmDeletingFromGrid(this,'Are you certain you want to delete THE WHOLE GROUP? This action cannot be undone.')"></asp:LinkButton>
                        &nbsp;&nbsp;&nbsp;&nbsp;<a href='map.aspx?group=<%# Eval("Id")%>' target="_blank">Public map link</a>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <AlternatingRowStyle CssClass="AlternatingRowStyle" />
            <RowStyle CssClass="RowStyle" />
        </asp:GridView>
        <div style="height: 10px">
            &nbsp;</div>
        <asp:Panel runat="server" ID="createGroupLinkPanel">
            <asp:LinkButton ID="createGroupLinkButton" runat="server" OnClick="createGroupLinkButton_Click" Text="Create new group..."></asp:LinkButton>
            <br />
        </asp:Panel>
        <asp:Panel ID="emptyTaskNotePanel" Visible="false" runat="server" EnableViewState="false">
            <span style="font-size: small">____
                <br />
                ^ Task is empty, so there is no actual task shown on the group's map. Use the section below to set up a task.</span>
        </asp:Panel>
    </div>
</div>
