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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="manageGroup.aspx.cs" Inherits="FlyTrace.ManageGroupForm" StylesheetTheme="Default" ViewStateEncryptionMode="Always" %>

<%@ PreviousPageType VirtualPath="~/default.aspx" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<script type="text/javascript" src="Scripts/jquery-1.11.3.min.js"></script>
<script type="text/javascript" src="Scripts/maintainScrollPosition.js"></script>
<script type="text/javascript" src="Scripts/tools.js"></script>
<script type="text/javascript">
    setScrollHiddenInputId('<%= scrollHiddenField.ClientID %>');
</script>
<script type="text/javascript">
        function validateTrackerForeignId(source, args) {
            try {
                var regEx = new RegExp(<%= "'" + SpotIdJScriptPattern + "'" %> , 'i');
                args.IsValid = regEx.test(args.Value);
            }
            catch (e) {
                alert(e.message);
            }
        }
</script>
<head runat="server">
    <title>Edit
        <%=GroupName%>
        - FlyTrace </title>
</head>
<body>
    <form id="manageGroupForm" runat="server">
    <div align="left">
        <asp:HiddenField ID="scrollHiddenField" runat="server" />
        <table style="text-align: left">
            <tr>
                <td>
                    <table style="width: 100%;" class="UserInfo">
                        <tr>
                            <td>
                                <a href="default.aspx">FlyTrace</a>&nbsp;&gt;&nbsp;<%=GroupName%>
                            </td>
                            <td style="text-align: right;">
                                <b>
                                    <asp:LoginName ID="LoginName1" runat="Server"></asp:LoginName>
                                </b>&nbsp;-&nbsp;<a href="profile.aspx">Settings</a>&nbsp;-&nbsp;<asp:LinkButton ID="SignOutLinkButton" runat="server" OnClick="SignOutLinkButton_Click">Sign out</asp:LinkButton>
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
                            <td class="VisualGroupTitle">
                                <i>
                                    <%=GroupName%></i>&nbsp;details
                            </td>
                        </tr>
                        <tr style="display: <%= string.IsNullOrEmpty(UpdateGroupMsg) ? "none" : ""%>;">
                            <td>
                                <div class="InfoMessage">
                                    <asp:Label runat="server"><%= UpdateGroupMsg %></asp:Label>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td style="padding-left: 0.2em">
                                <asp:MultiView runat="server" ID="headerMultiView" ActiveViewIndex="0">
                                    <asp:View runat="server" ID="headerDisplayView">
                                        <div style="margin-bottom: 0.8em">
                                            - Group name: <b>
                                                <%=GroupName%></b>
                                        </div>
                                        <div style="margin-bottom: 0.8em">
                                            <asp:Panel ID="publicGroupPanel" runat="server" Visible="true">
                                                - This group is <b>ON THE HOMEPAGE</b>: anyone can see its Public Map link on the homepage
                                                <br />
                                                (when the group contains 2 or more pilots)
                                            </asp:Panel>
                                            <asp:Panel ID="unlistedGroupPanel" runat="server" Visible="false">
                                                - This group is <b>UNLISTED</b>: share the map link only with the people you want to watch it.<br />
                                                Note: site admins can see the group anyway.
                                            </asp:Panel>
                                        </div>
                                        <div style="margin-bottom: 0.8em">
                                            <asp:Panel ID="hideUserMessagesAsNewPanel" runat="server">
                                                - <b>DO NOT SHOW</b> owner-defined messages* for OK, CUSTOM and HELP markers.
                                                <br />
                                                * <span style="color: Red; font-size: smaller"><b>New!</b> Change this setting to show owner-defined messages.</span> 
                                                <a href="help/usermessageshelp.aspx">
                                                    <asp:Image ID="Image5" runat="server" ImageUrl="~/App_Themes/Default/siteHelp.png" /></a>
                                            </asp:Panel>
                                            <asp:Panel ID="hideUserMessagesPanel" runat="server">
                                                - <b>DO NOT SHOW</b> owner-defined messages for OK, CUSTOM and HELP  markers.
                                                <a href="help/usermessageshelp.aspx">
                                                    <asp:Image ID="Image4" runat="server" ImageUrl="~/App_Themes/Default/siteHelp.png" /></a>
                                            </asp:Panel>
                                            <asp:Panel ID="showUserMessagesPanel" runat="server">
                                                - <b>SHOW</b> owner-defined messages for OK, CUSTOM and HELP markers.
                                                <a href="help/usermessageshelp.aspx">
                                                    <asp:Image ID="Image6" runat="server" ImageUrl="~/App_Themes/Default/siteHelp.png" /></a>
                                            </asp:Panel>
                                        </div>
                                        <asp:Button ID="changeGroupDetailsButton" runat="server" OnClick="changeGroupDetailsButton_Click" Text="Change details above" />
                                        &nbsp;&nbsp;&nbsp;
                                        <asp:Button ID="deleteGroup" runat="server" OnClick="deleteGroup_Click" OnClientClick="<%$ Resources: Resources, GroupDeleteConfirmationFunc %>" Style="margin-right: 2em" Text="Delete the whole group" />
                                        <a href="map.aspx?group=<%= GroupId%>" target="_blank">Public map link</a>
                                    </asp:View>
                                    <asp:View runat="server" ID="headerEditView">
                                        - Group name:
                                        <asp:TextBox runat="server" ID="groupNameTextBox"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator5" runat="server" ErrorMessage="Required field" ControlToValidate="groupNameTextBox"></asp:RequiredFieldValidator>
                                        <hr />
                                        Select how you want others to discover your group:
                                        <asp:RadioButtonList ID="publicOptionRadioButtonList" runat="server">
                                            <asp:ListItem Value="public"><b>ON THE HOMEPAGE</b>: anyone can see its Public Map link on the homepage.
                                            </asp:ListItem>
                                            <asp:ListItem Value="unlisted"><b>UNLISTED</b>: share the map link only with the people you want to watch it *
                                            </asp:ListItem>
                                        </asp:RadioButtonList>
                                        <span style="font-size: small">* site admins can see the group anyway. </span>
                                        <hr />
                                        Owner-defined messages for OK, CUSTOM and HELP markers:
                                        <asp:Image ID="Image3" runat="server" ImageUrl="~/App_Themes/Default/siteHelp.png" />
                                        <asp:RadioButtonList ID="showMessagesRadioButtonList" runat="server">
                                            <asp:ListItem Value="show"><b>SHOW</b> owner-defined messages for this group.
                                            </asp:ListItem>
                                            <asp:ListItem Value="hide"><b>DO NOT SHOW</b> owner-defined messages for this group.
                                            </asp:ListItem>
                                        </asp:RadioButtonList>
                                        <br />
                                        <asp:Button ID="updateGroupDetailsButton" runat="server" Text="Update" OnClick="updateGroupDetailsButton_Click" />
                                        <asp:Button ID="cancelEditGroupDetailsButton" runat="server" Text="Cancel" OnClick="cancelEditGroupDetailsButton_Click" CausesValidation="False" />
                                    </asp:View>
                                </asp:MultiView>
                            </td>
                        </tr>
                        <tr style="display: none">
                            <td>
                                <asp:GridView EnableViewState="false" ID="groupHeaderGridView" runat="server" AutoGenerateColumns="False" DataKeyNames="Id" DataSourceID="groupHeaderDataSource" GridLines="None" OnRowUpdated="groupHeaderGridView_RowUpdated" OnRowEditing="groupHeaderGridView_RowEditing">
                                    <Columns>
                                        <asp:TemplateField HeaderText="Group name" HeaderStyle-HorizontalAlign="Left" SortExpression="Name">
                                            <ItemTemplate>
                                                <b>
                                                    <asp:Label ID="Label1" runat="server" Text='<%# Bind("Name") %>'></asp:Label></b>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="groupNameTextBox" runat="server" Text='<%# Bind("Name") %>'></asp:TextBox>
                                                <asp:RequiredFieldValidator ControlToValidate="groupNameTextBox" ID="RequiredFieldValidator2" runat="server" ErrorMessage="&lt;br /&gt;Group name cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
                                            </EditItemTemplate>
                                            <HeaderStyle Font-Bold="False" />
                                        </asp:TemplateField>
                                        <asp:TemplateField ShowHeader="False">
                                            <EditItemTemplate>
                                                &nbsp;&nbsp;
                                                <asp:Button ID="LinkButton1" runat="server" CausesValidation="True" CommandName="Update" Text="Update"></asp:Button>
                                                &nbsp;<asp:Button ID="LinkButton2" runat="server" CausesValidation="False" CommandName="Cancel" Text="Cancel"></asp:Button>
                                            </EditItemTemplate>
                                            <ItemTemplate>
                                                &nbsp;&nbsp;
                                                <asp:Button ID="LinkButton2" runat="server" CausesValidation="False" CommandName="Edit" Text="Rename"></asp:Button>
                                                &nbsp;&nbsp;&nbsp;
                                                <asp:Button ID="LinkButton1" runat="server" CausesValidation="False" CommandName="Delete" Text="Delete the whole group" OnClientClick="<%$ Resources: Resources, GroupDeleteConfirmationFunc %>"></asp:Button>
                                                &nbsp;&nbsp;&nbsp; <a href='map.aspx?group=<%= GroupId%>' target="_blank">Public map link</a>
                                            </ItemTemplate>
                                            <ItemStyle VerticalAlign="Top" />
                                        </asp:TemplateField>
                                    </Columns>
                                    <%--<RowStyle CssClass="RowStyle" />--%>
                                </asp:GridView>
                                <asp:SqlDataSource ID="groupHeaderDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>" SelectCommand="SELECT [Id], [Name] FROM [Group] WHERE ([Id] = @Id)" UpdateCommand="UPDATE [Group] SET [Name] = @Name WHERE [Id] = @Id" DeleteCommand="DELETE FROM [Group] WHERE [Id] = @Id" OnUpdating="gridReadonlyCheck_Changing" OnUpdated="groupHeaderDataSource_Updated" OnDeleting="gridReadonlyCheck_Changing" OnDeleted="groupHeaderDataSource_Deleted" OnInserting="gridReadonlyCheck_Changing">
                                    <SelectParameters>
                                        <flytrace_tools:EvalParameter Name="Id" Type="Int32" Expression="GroupId" />
                                    </SelectParameters>
                                    <UpdateParameters>
                                        <asp:Parameter Name="Name" Type="String" />
                                        <asp:Parameter Name="Id" Type="Int32" />
                                    </UpdateParameters>
                                    <DeleteParameters>
                                        <asp:Parameter Name="Id" Type="Int32" />
                                    </DeleteParameters>
                                </asp:SqlDataSource>
                                <hr />
                                <asp:MultiView runat="server" ID="publicOptionMultiView" ActiveViewIndex="0">
                                    <asp:View runat="server" ID="dispayPublicOptionView">
                                        <table>
                                            <tr>
                                                <td>
                                                </td>
                                                <td>
                                                    <asp:Button ID="editPublicOptionButton" runat="server" Text="Change" OnClick="editPublicOptionButton_Click" />
                                                </td>
                                            </tr>
                                        </table>
                                    </asp:View>
                                    <asp:View runat="server" ID="editPublicOptionView">
                                        Select how you want others to discover your group:
                                        <table>
                                            <tr>
                                                <td>
                                                    <asp:RadioButtonList ID="publicOptionRadioButtonList_old" runat="server">
                                                        <asp:ListItem Value="public">ON THE HOMEPAGE: anyone can see its Public Map link on the homepage.
                                                        </asp:ListItem>
                                                        <asp:ListItem Value="unlisted">UNLISTED: share the map link only with the people you want to view it *
                                                        </asp:ListItem>
                                                    </asp:RadioButtonList>
                                                    * site admins can see the group anyway.
                                                </td>
                                                <td>
                                                    <asp:Button ID="savePublicOptionChangeButton" runat="server" Text="Update" OnClick="savePublicOptionChangeButton_Click" />
                                                </td>
                                            </tr>
                                        </table>
                                    </asp:View>
                                </asp:MultiView>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            
            <tr>
                <td>
                    <br/>
                    <b>Altitude display mode: <%= AltitudeDisplayModeString %></b> (change it for all groups in your profile <a href="profile.aspx">Settings</a>)<br/>
                    <span style="font-size: smaller">Altitude is available only for selected models. E.g SPOT Gen3 can send it, but <br/>
                    sometimes the altitude is missing for the points received even from this tracker model.</span>
                </td>
            </tr>

            <tr>
                <td>
                    <br />
                    <table width="100%" class="GroupTable">
                        <tr>
                            <td class="VisualGroupTitle">
                                Trackers in the group
                            </td>
                        </tr>
                        <tr style="display: <%= string.IsNullOrEmpty(UpdateTrackerMsg) ? "none" : ""%>;">
                            <td>
                                <div class="InfoMessage">
                                    <asp:Label runat="server"><%= UpdateTrackerMsg %></asp:Label>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:GridView EnableViewState="false" Width="100%" DataKeyNames="Id" ID="trackersGridView" runat="server" AutoGenerateColumns="False" DataSourceID="trackersDataSource" EmptyDataText="Group doesn't have trackers at the moment, use Add Tracker command below." GridLines="None" AllowSorting="True" OnRowUpdated="trackersGridView_RowUpdated" OnDataBound="trackersGridView_DataBound" OnRowDataBound="trackersGridView_RowDataBound">
                                    <Columns>
                                        <asp:TemplateField HeaderText="#">
                                            <ItemTemplate>
                                                <%# Container.DataItemIndex + 1 %>
                                            </ItemTemplate>
                                            <HeaderStyle HorizontalAlign="Left" />
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                &nbsp;&nbsp;
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Pilot Name" SortExpression="Name">
                                            <ItemTemplate>
                                                <asp:Label ID="Label1" runat="server" Text='<%# Bind("Name") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="pilotNameTextBox" runat="server" Text='<%# Bind("Name") %>'></asp:TextBox>
                                                <asp:RequiredFieldValidator ControlToValidate="pilotNameTextBox" ID="RequiredFieldValidator1" runat="server" ErrorMessage="&lt;br /&gt;Pilot name cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
                                            </EditItemTemplate>
                                            <HeaderStyle HorizontalAlign="Left" />
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                &nbsp;&nbsp;
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Spot ID" SortExpression="TrackerForeignId">
                                            <ItemTemplate>
                                                <span style="font-family: Courier New;">
                                                    <%# string.Format( "<a href='http://share.findmespot.com/shared/faces/viewspots.jsp?glId={0}' target='_blank'>{0}</a>" , Eval( "TrackerForeignId" ) )%>
                                                </span>
                                            </ItemTemplate>
                                            <HeaderStyle HorizontalAlign="Left" />
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                &nbsp;&nbsp;</ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField ShowHeader="False">
                                            <EditItemTemplate>
                                                <asp:LinkButton runat="server" CausesValidation="True" CommandName="Update" Text="Update"></asp:LinkButton>
                                            </EditItemTemplate>
                                            <ItemTemplate>
                                                <asp:LinkButton runat="server" CausesValidation="False" CommandName="Edit" Text="Rename"></asp:LinkButton>
                                            </ItemTemplate>
                                            <ItemStyle VerticalAlign="Top" />
                                        </asp:TemplateField>
                                        <asp:TemplateField ShowHeader="False">
                                            <EditItemTemplate>
                                                <asp:LinkButton runat="server" CausesValidation="False" CommandName="Cancel" Text="Cancel"></asp:LinkButton>
                                            </EditItemTemplate>
                                            <ItemTemplate>
                                                <asp:LinkButton runat="server" CausesValidation="False" CommandName="Delete" Text="Delete" OnClientClick="return confirmDeletingFromGrid(this,'Are you certain you want to delete the tracker from the group?')"></asp:LinkButton>
                                            </ItemTemplate>
                                            <ItemStyle VerticalAlign="Top" />
                                        </asp:TemplateField>
                                    </Columns>
                                    <SelectedRowStyle BackColor="#66FFFF" />
                                    <AlternatingRowStyle CssClass="AlternatingRowStyle" />
                                    <RowStyle CssClass="RowStyle" />
                                </asp:GridView>
                            </td>
                        </tr>
                    </table>
                    <br />
                </td>
            </tr>
            <%--Top-level row: "Add tracker to the group"--%><tr>
                <td>
                    <table width="100%" class="GroupTable">
                        <tr>
                            <td class="VisualGroupTitle">
                                Add tracker to the group
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <span id="insertTrackerLbl" class="InfoMessage" style="display: <%= string.IsNullOrEmpty(InsertTrackerMsg) ? "none" : ""%>;">
                                    <%= InsertTrackerMsg %><br />
                                </span><span id="addProgressLbl" class="InfoMessage" style="display: none">
                                    <asp:Image ID="Image2" runat="server" ImageUrl="~/App_Themes/Default/hourglass.gif" />
                                    Adding &amp; checking the tracker, please wait...<br />
                                </span>
                                <asp:FormView ID="formView" runat="server" DataKeyNames="Id" DataSourceID="trackersDataSource" DefaultMode="Insert" OnItemInserted="formView_ItemInserted" OnItemInserting="formView_ItemInserting" OnDataBound="formView_DataBound">
                                    <InsertItemTemplate>
                                        <table style="padding-right: 2px">
                                            <tr>
                                                <td style="vertical-align: top;">
                                                    Pilot Name:
                                                </td>
                                                <td>
                                                    <asp:TextBox Width="100%" ID="NameTextBox" runat="server" Text='<%# Bind("Name") %>' />
                                                    <asp:RequiredFieldValidator ValidationGroup="formView" ControlToValidate="NameTextBox" ID="RequiredFieldValidator4" runat="server" ErrorMessage="&lt;br /&gt;Pilot name cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="vertical-align: top;">
                                                    SPOT Shared Page
                                                </td>
                                                <td>
                                                    <asp:TextBox Width="100%" ID="TrackerForeignId" runat="server" Text='<%# Bind("TrackerForeignId") %>' />
                                                    <div style="font-size: smaller;">
                                                        SPOT Shared Page link, or just ID part of the link &nbsp;<a href="help/spotidhelp.htm" target="_blank"><asp:Image ID="Image1" runat="server" ImageUrl="~/App_Themes/Default/siteHelp.png" /></a></div>
                                                    <asp:RequiredFieldValidator ValidationGroup="formView" ControlToValidate="TrackerForeignId" ID="RequiredFieldValidator3" runat="server" ErrorMessage="Spot ID cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
                                                    <asp:CustomValidator ID="CustomValidator1" runat="server" ClientValidationFunction="validateTrackerForeignId" ErrorMessage="That doesn't look like a SPOT Shared Page link (click Question mark above)" ControlToValidate="TrackerForeignId" ValidationGroup="formView" OnServerValidate="ValidateTrackerForeignId"></asp:CustomValidator>
                                                </td>
                                            </tr>
                                        </table>
                                        <asp:Button ID="InsertButton" 
                                            runat="server" 
                                            CausesValidation="True" 
                                            ValidationGroup="formView" 
                                            CommandName="Insert" 
                                            Text="Add Tracker" 
                                            OnClientClick="document.getElementById('addProgressLbl').style.display = '';document.getElementById('insertTrackerLbl').style.display = 'none';" />
                                    </InsertItemTemplate>
                                </asp:FormView>
                            </td>
                        </tr>
                    </table>
                    <asp:SqlDataSource ID="trackersDataSource" 
                        runat="server" 
                        ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>" 
                        SelectCommand="SELECT [Id], [Name], [TrackerForeignId] FROM [GroupTracker] WHERE ([GroupId] = @GroupId)" 
                        InsertCommand="EXEC AddTrackerToGroup @GroupId, @Name, @TrackerForeignId" 
                        UpdateCommand="EXEC UpdateTrackerInGroup @Id, @Name" 
                        DeleteCommand="EXEC DeleteTrackerFromGroup @Id" 
                        OnInserting="gridReadonlyCheck_Changing" 
                        OnInserted="trackersDataSource_Inserted" 
                        OnUpdating="gridReadonlyCheck_Changing"
                        OnUpdated="trackersDataSource_Updated" 
                        OnDeleting="gridReadonlyCheck_Changing"
                        >
                        <SelectParameters>
                            <flytrace_tools:EvalParameter Name="GroupId" Expression="GroupId" />
                        </SelectParameters>
                        <DeleteParameters>
                            <asp:Parameter Name="Id" />
                        </DeleteParameters>
                        <UpdateParameters>
                            <asp:Parameter Name="Name" />
                            <asp:Parameter Name="Id" />
                        </UpdateParameters>
                        <InsertParameters>
                            <flytrace_tools:EvalParameter Name="GroupId" Expression="GroupId" />
                            <asp:Parameter Name="Name" />
                            <asp:Parameter Name="TrackerForeignId" />
                        </InsertParameters>
                    </asp:SqlDataSource>
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    <table width="100%" class="GroupTable">
                        <asp:MultiView runat="server" ActiveViewIndex="<%$ FtCode: FlyTrace.Global.IsSimpleEventsModel ? 0: 1%>">
                            <asp:View runat="server">
                                <tr>
                                    <td class="VisualGroupTitle">
                                        OPTIONAL: the group's Task
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:MultiView ID="simpleTaskMultiView" runat="server" ActiveViewIndex="0">
                                            <asp:View ID="simpleTaskShowView" runat="server">
                                                <% 
                                                    if ( AssignedTaskId.HasValue &&
                                                         DefEventId.HasValue &&
                                                         AssignedTaskId.Value == DefEventId.Value )
                                                    {
                                                        FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter procAdapter =
                                                            new FlyTrace.TrackerDataSetTableAdapters.ProcsAdapter( );

                                                        string template;
                                                        if ( DefEventTaskWptCount.Value > 1 )
                                                        {
                                                            template =
                                                                "The group is showing the <a href='manageEvent.aspx?event={0}'>Today Task</a> on the group's map";
                                                        }
                                                        else
                                                        {
                                                            template =
                                                                "The group will show the <a href='manageEvent.aspx?event={0}'>Today Task</a> on the group's map when the task is ready.";
                                                        }

                                                        Response.Write( string.Format( template, AssignedTaskId ) );
                                                    }
                                                    else
                                                    {
                                                        Response.Write( "The group is NOT showing the Today Task on the group's map" );
                                                    }
                                                %>
                                                &nbsp;&nbsp;
                                                <asp:Button runat="server" OnClick="changeSimpleTaskMultiView_Click" Text="Change" />
                                            </asp:View>
                                            <asp:View ID="simpleTaskEditView" runat="server">
                                                <asp:CheckBox ID="showTaskCheckBox" Text="Display Today Task on the group's map" runat="server" />
                                                &nbsp;&nbsp;
                                                <asp:Button ID="updateSimpleTaskButton" runat="server" OnClick="changeSimpleTaskMultiView_Click" Text="Update" />
                                                &nbsp;&nbsp;
                                                <asp:Button ID="simpleTaskCancelEditButton" runat="server" OnClick="changeSimpleTaskMultiView_Click" Text="Cancel" />
                                            </asp:View>
                                        </asp:MultiView>
                                    </td>
                                </tr>
                            </asp:View>
                            <asp:View runat="server">
                                <tr>
                                    <td class="VisualGroupTitle">
                                        OPTIONAL: the group's current Event
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        A task could be added to the <a href='map.aspx?group=<%=GroupId%>'>group's map</a> page.
                                        <p>
                                            Use <b>Today Task</b> from:
                                            <asp:MultiView ID="eventMultiView" runat="server" ActiveViewIndex="0">
                                                <asp:View ID="eventShowView" runat="server">
                                                    <span class="RowStyle">&nbsp;&nbsp;&nbsp;&nbsp;
                                                        <%
                                                            System.Data.DataView assignedEventsDv =
                                                                assignedEventsSqlDataSource.Select( DataSourceSelectArguments.Empty ) as System.Data.DataView;
                                                            if ( assignedEventsDv != null && assignedEventsDv.Count == 1 )
                                                            {
                                                                Response.Write(
                                                                    string.Format(
                                                                        "<a href='manageEvent.aspx?event={0}'>{1}</a>",
                                                                        assignedEventsDv[0]["EventId"],
                                                                        assignedEventsDv[0]["EventName"]
                                                                    )
                                                                );
                                                            }
                                                            else
                                                            {
                                                                Response.Write( "&lt;None&gt;" );
                                                            }                                            
                                                        %>
                                                        &nbsp;&nbsp;&nbsp;&nbsp; </span>&nbsp;
                                                    <asp:Button ID="Button4" runat="server" OnClick="changeTaskMultiView_Click" Text="Use other event"></asp:Button>
                                                    <asp:SqlDataSource ID="assignedEventsSqlDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>" SelectCommand="SELECT E.Id AS EventId, E.Name AS EventName FROM [Event] E JOIN [Group] G ON G.EventId=E.Id WHERE G.Id=@GroupId">
                                                        <SelectParameters>
                                                            <flytrace_tools:EvalParameter Name="GroupId" Type="Int32" Expression="GroupId" />
                                                        </SelectParameters>
                                                    </asp:SqlDataSource>
                                                </asp:View>
                                                <asp:View ID="eventEditView" runat="server">
                                                    <asp:DropDownList ID="assignedTaskDdl" runat="server" DataSourceID="eventsSqlDataSource" AppendDataBoundItems="true" DataTextField="Name" DataValueField="Id" OnDataBound="assignedTaskDdl_DataBound">
                                                        <asp:ListItem Text="&lt;None&gt;" Value="0" />
                                                        <asp:ListItem Text="&lt;Create new event&gt;" Value="-1" />
                                                    </asp:DropDownList>
                                                    &nbsp;&nbsp;
                                                    <asp:Button ID="updateAssignedTaskLinkButton" runat="server" OnClick="changeTaskMultiView_Click" Text="Update" />
                                                    &nbsp;&nbsp;
                                                    <asp:Button ID="Button5" runat="server" OnClick="changeTaskMultiView_Click" Text="Cancel"></asp:Button>
                                                    <asp:SqlDataSource ID="eventsSqlDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>" SelectCommand="SELECT [Id], [Name] FROM [Event] WHERE [UserId] = @UserId ORDER BY [Name]">
                                                        <SelectParameters>
                                                            <asp:SessionParameter Name="UserId" SessionField="UserId" />
                                                        </SelectParameters>
                                                    </asp:SqlDataSource>
                                                </asp:View>
                                            </asp:MultiView>
                                        </p>
                                    </td>
                                </tr>
                            </asp:View>
                        </asp:MultiView>
                    </table>
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
<script type="text/javascript">
    var originalValidationFunction = Page_ClientValidate;
    if (originalValidationFunction && typeof (originalValidationFunction) == "function") {
        Page_ClientValidate = function (validationGroup) {
            originalValidationFunction(validationGroup);

            if (!Page_IsValid) {
                document.getElementById('addProgressLbl').style.display = 'none';
            }
        };
    }
</script>

</html>
