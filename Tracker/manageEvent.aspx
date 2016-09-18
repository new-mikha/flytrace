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
<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/react/15.3.1/react.js"></script>
<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/react/15.3.1/react-dom.js"></script>
<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/babel-core/5.6.16/browser.js"></script>
<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/jquery/2.2.2/jquery.min.js"></script>
<script type="text/javascript" src="Scripts/maintainScrollPosition.js"></script>
<script type="text/javascript" src="Scripts/manageEvent.js"></script>
<script type="text/javascript">
    setScrollHiddenInputId('<%= scrollHiddenField.ClientID %>');
</script>
<head runat="server">
    <title>
        <% 
            if ( FlyTrace.Global.IsSimpleEventsModel )
            {
                Response.Write( "Edit Today Task - FlyTrace" );
            }
            else
            {
                Response.Write( string.Format( "Edit Today Task for {0} - FlyTrace", EventName ) );
            }
        %>
    </title>
    <script language="javascript" type="text/javascript">
        // <!CDATA[

        function taskChanged()
        {
        }

        function clearWp(wpNum)
        {
            document.getElementById('ddlWp' + wpNum.toString()).options[0].selected = true;

            var defRadius = 400;
            if (wpNum == 0)
            {
                defRadius = 10000;
            }
            document.getElementById('tbRadius' + wpNum.toString()).value = defRadius.toString();
        }

        function btnClear_onclick()
        {
            try
            {
                document.getElementById("ddlWp1").options[0].selected = true;
                document.getElementById("ddlWp2").options[0].selected = true;
                document.getElementById("ddlWp3").options[0].selected = true;
                document.getElementById("ddlWp4").options[0].selected = true;
                document.getElementById("ddlWp5").options[0].selected = true;
                document.getElementById("ddlWp6").options[0].selected = true;
                document.getElementById("ddlWp7").options[0].selected = true;
                document.getElementById("ddlWp8").options[0].selected = true;

                document.getElementById("tbRadius0").value = "10000";
                document.getElementById("tbRadius1").value = "400";
                document.getElementById("tbRadius2").value = "400";
                document.getElementById("tbRadius3").value = "400";
                document.getElementById("tbRadius4").value = "400";
                document.getElementById("tbRadius5").value = "400";
                document.getElementById("tbRadius6").value = "400";
                document.getElementById("tbRadius7").value = "400";
                document.getElementById("tbRadius8").value = "400";

                taskChanged();
            } catch (e)
            {
                alert(e.message);
            }
        }

        // ]]>
    </script>
</head>
<body>
    <form id="frmEvent" runat="server">
    <div align="left" style="width: 26em">
        <asp:HiddenField ID="scrollHiddenField" runat="server" />
        <asp:ScriptManager runat="server" />
        <script type="text/javascript" language="javascript">
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(onEndRequest);

            function onEndRequest(sender, args)
            {
                if (args.get_error() != undefined)
                {
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
                                                        Text="Delete the whole event" OnClientClick="<%$ Resources: Resources, EventDeleteConfirmationFunc %>">
                                                    </asp:Button>
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
                                <td style="background-color: #FFFF66" class="VisualGroupTitle">
                                    Load waypoints first
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
                                <td>
                                    This should be Google Earth (*.KML) or SeeYou (*.CUP) file:<br />
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
                            <td class="VisualGroupTitle">
                                Today Task
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
                                <asp:Panel runat="server" ID="taskPanel" OnInit="taskPanel_Init">
                                    <br />
                                    <b><a href='manageWaypoints.aspx?event=<%=EventId%>'>Manage available waypoints (add
                                        new waypoints, edit etc)...</a></b>
                                    <br />
                                    <br />
                                    <input id="btnClear" type="button" value="Clear Task" onclick="return btnClear_onclick()" />
                                    
                                    <div id="react_waypoints"></div>
                                    <table id="sdfsdf" style="width: 100%">
                                        <tr>
                                            <td>
                                                #
                                            </td>
                                            <td>
                                                Turn Point
                                            </td>
                                            <td>
                                                Radius (in meters)
                                            </td>
                                        </tr>
                                        <tr valign="top">
                                            <td>
                                                Start
                                            </td>
                                            <td>
                                                <asp:DropDownList ID="ddlWp0" runat="server" onchange="taskChanged()" EnableViewState="true">
                                                </asp:DropDownList>
                                            </td>
                                            <td>
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator10" runat="server"
                                                    ValidationExpression="^[0-9]+$" ControlToValidate="tbRadius0" ErrorMessage="*"
                                                    Display="Dynamic" />
                                                <asp:TextBox ID="tbRadius0" Text="10000" runat="server" onchange="taskChanged()"
                                                    EnableViewState="true"> </asp:TextBox>
                                                <input type="button" value="Clr" onclick="clearWp(0)" />
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" ValidationExpression="^[0-9]+$"
                                                    ControlToValidate="tbRadius0" ErrorMessage="<br />*Should be non-negative numeric value"
                                                    Display="Dynamic" />
                                            </td>
                                        </tr>
                                        <tr valign="top">
                                            <td>
                                                1
                                            </td>
                                            <td>
                                                <asp:DropDownList ID="ddlWp1" runat="server" onchange="taskChanged()" EnableViewState="true">
                                                </asp:DropDownList>
                                            </td>
                                            <td>
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator11" runat="server"
                                                    ValidationExpression="^[0-9]+$" ControlToValidate="tbRadius1" ErrorMessage="*"
                                                    Display="Dynamic" />
                                                <asp:TextBox ID="tbRadius1" runat="server" Text="400" onchange="taskChanged()" EnableViewState="true"> </asp:TextBox>
                                                <input type="button" value="Clr" onclick="clearWp(1)" />
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator2" runat="server" ValidationExpression="^[0-9]+$"
                                                    ControlToValidate="tbRadius1" ErrorMessage="<br />Should be non-negative numeric value"
                                                    Display="Dynamic" />
                                            </td>
                                        </tr>
                                        <tr valign="top">
                                            <td>
                                                2
                                            </td>
                                            <td>
                                                <asp:DropDownList ID="ddlWp2" runat="server" onchange="taskChanged()" EnableViewState="true">
                                                </asp:DropDownList>
                                            </td>
                                            <td>
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator12" runat="server"
                                                    ValidationExpression="^[0-9]+$" ControlToValidate="tbRadius2" ErrorMessage="*"
                                                    Display="Dynamic" />
                                                <asp:TextBox ID="tbRadius2" runat="server" Text="400" onchange="taskChanged()" EnableViewState="true"> </asp:TextBox>
                                                <input type="button" value="Clr" onclick="clearWp(2)" />
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator3" runat="server" ValidationExpression="^[0-9]+$"
                                                    ControlToValidate="tbRadius2" ErrorMessage="<br />Should be non-negative numeric value"
                                                    Display="Dynamic" />
                                            </td>
                                        </tr>
                                        <tr valign="top">
                                            <td>
                                                3
                                            </td>
                                            <td>
                                                <asp:DropDownList ID="ddlWp3" runat="server" onchange="taskChanged()" EnableViewState="true">
                                                </asp:DropDownList>
                                            </td>
                                            <td>
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator13" runat="server"
                                                    ValidationExpression="^[0-9]+$" ControlToValidate="tbRadius3" ErrorMessage="*"
                                                    Display="Dynamic" />
                                                <asp:TextBox ID="tbRadius3" runat="server" Text="400" onchange="taskChanged()" EnableViewState="true"> </asp:TextBox>
                                                <input type="button" value="Clr" onclick="clearWp(3)" />
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator4" runat="server" ValidationExpression="^[0-9]+$"
                                                    ControlToValidate="tbRadius3" ErrorMessage="<br />Should be non-negative numeric value"
                                                    Display="Dynamic" />
                                            </td>
                                        </tr>
                                        <tr valign="top">
                                            <td>
                                                4
                                            </td>
                                            <td>
                                                <asp:DropDownList ID="ddlWp4" runat="server" onchange="taskChanged()" EnableViewState="true">
                                                </asp:DropDownList>
                                            </td>
                                            <td>
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator15" runat="server"
                                                    ValidationExpression="^[0-9]+$" ControlToValidate="tbRadius4" ErrorMessage="*"
                                                    Display="Dynamic" />
                                                <asp:TextBox ID="tbRadius4" runat="server" Text="400" onchange="taskChanged()" EnableViewState="true"> </asp:TextBox>
                                                <input type="button" value="Clr" onclick="clearWp(4)" />
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator5" runat="server" ValidationExpression="^[0-9]+$"
                                                    ControlToValidate="tbRadius4" ErrorMessage="<br />Should be non-negative numeric value"
                                                    Display="Dynamic" />
                                            </td>
                                        </tr>
                                        <tr valign="top">
                                            <td>
                                                5
                                            </td>
                                            <td>
                                                <asp:DropDownList ID="ddlWp5" runat="server" onchange="taskChanged()" EnableViewState="true">
                                                </asp:DropDownList>
                                            </td>
                                            <td>
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator16" runat="server"
                                                    ValidationExpression="^[0-9]+$" ControlToValidate="tbRadius5" ErrorMessage="*"
                                                    Display="Dynamic" />
                                                <asp:TextBox ID="tbRadius5" runat="server" Text="400" onchange="taskChanged()" EnableViewState="true"> </asp:TextBox>
                                                <input type="button" value="Clr" onclick="clearWp(5)" />
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator6" runat="server" ValidationExpression="^[0-9]+$"
                                                    ControlToValidate="tbRadius5" ErrorMessage="<br />Should be non-negative numeric value"
                                                    Display="Dynamic" />
                                            </td>
                                        </tr>
                                        <tr valign="top">
                                            <td>
                                                6
                                            </td>
                                            <td>
                                                <asp:DropDownList ID="ddlWp6" runat="server" onchange="taskChanged()" EnableViewState="true">
                                                </asp:DropDownList>
                                            </td>
                                            <td>
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator17" runat="server"
                                                    ValidationExpression="^[0-9]+$" ControlToValidate="tbRadius6" ErrorMessage="*"
                                                    Display="Dynamic" />
                                                <asp:TextBox ID="tbRadius6" runat="server" Text="400" onchange="taskChanged()" EnableViewState="true"> </asp:TextBox>
                                                <input type="button" value="Clr" onclick="clearWp(6)" />
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator7" runat="server" ValidationExpression="^[0-9]+$"
                                                    ControlToValidate="tbRadius6" ErrorMessage="<br />Should be non-negative numeric value"
                                                    Display="Dynamic" />
                                            </td>
                                        </tr>
                                        <tr valign="top">
                                            <td>
                                                7
                                            </td>
                                            <td>
                                                <asp:DropDownList ID="ddlWp7" runat="server" onchange="taskChanged()" EnableViewState="true">
                                                </asp:DropDownList>
                                            </td>
                                            <td>
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator18" runat="server"
                                                    ValidationExpression="^[0-9]+$" ControlToValidate="tbRadius7" ErrorMessage="*"
                                                    Display="Dynamic" />
                                                <asp:TextBox ID="tbRadius7" runat="server" Text="400" onchange="taskChanged()" EnableViewState="true"> </asp:TextBox>
                                                <input type="button" value="Clr" onclick="clearWp(7)" />
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator8" runat="server" ValidationExpression="^[0-9]+$"
                                                    ControlToValidate="tbRadius7" ErrorMessage="<br />Should be non-negative numeric value"
                                                    Display="Dynamic" />
                                            </td>
                                        </tr>
                                        <tr valign="top">
                                            <td>
                                                8
                                            </td>
                                            <td>
                                                <asp:DropDownList ID="ddlWp8" runat="server" onchange="taskChanged()" EnableViewState="true">
                                                </asp:DropDownList>
                                            </td>
                                            <td>
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator19" runat="server"
                                                    ValidationExpression="^[0-9]+$" ControlToValidate="tbRadius8" ErrorMessage="*"
                                                    Display="Dynamic" />
                                                <asp:TextBox ID="tbRadius8" runat="server" Text="400" onchange="taskChanged()" EnableViewState="true"> </asp:TextBox>
                                                <input type="button" value="Clr" onclick="clearWp(8)" />
                                                <asp:RegularExpressionValidator ID="RegularExpressionValidator9" runat="server" ValidationExpression="^[0-9]+$"
                                                    ControlToValidate="tbRadius8" ErrorMessage="<br />Should be non-negative numeric value"
                                                    Display="Dynamic" />
                                            </td>
                                        </tr>
                                    </table>
                                    <span style="color: #FF3300">
                                        <%=TaskSaveError%></span> <span class="InfoMessage">
                                            <%=TaskSaveInfo%></span>
                                    <asp:Button ID="saveBtn" runat="server" OnClick="saveBtn_Click" Text="Save" />
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
                            <td class="VisualGroupTitle">
                                Old points removal (optional)
                            </td>
                        </tr>
                        <tr>
                            <td style="text-align: justify;">
                                <asp:UpdatePanel runat="server" UpdateMode="Always">
                                    <ContentTemplate>
                                        <asp:Timer runat="server" Interval="30000">
                                        </asp:Timer>
                                        <asp:Panel runat="server" ID="clearOldPointsInfoPanel" Style="margin-top: 0.6em;
                                            margin-bottom: 0.6em;">
                                            <span class="InfoMessage"><b>Assigned maps are hiding the old points now.</b><br />
                                                <asp:Label runat="server" ID="cleanUpAgeLabel">
                                                Threshold for "old" is the moment <b>
                                                    <%=OldPointsCleanUpAge %></b>, i.e.</asp:Label><br />
                                                only those points are displayed that are appeared<br />
                                                after<b>
                                                    <%=OldPointsCleanUpLocalTs %></b>,<br />
                                                which is <b>
                                                    <%=OldPointsCleanUpUtcTs %></b>. </span>
                                        </asp:Panel>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                                <asp:Button runat="server" ID="clearOldPointsButton" Text="Hide points older than now"
                                    OnClick="clearOldPointsButton_Click" OnClientClick="return confirm('Are you sure you want to hide all points older than NOW?')" />
                                <br />
                                <span style="font-size: small">Clicking on this button hides all trackers from
                                    <asp:Literal runat="server" Visible="<%$ FtCode: FlyTrace.Global.IsSimpleEventsModel %>">
                                            all maps showing the task
                                    </asp:Literal>
                                    <asp:Literal runat="server" Visible="<%$ FtCode: !FlyTrace.Global.IsSimpleEventsModel %>">
                                            the event's assigned maps 
                                    </asp:Literal>
                                    until new positions are received. That can be useful when only new positions make
                                    sense for the moment. E.g. it can be clicked at the beginning of the competition
                                    day - without such clean-up a map can contain points and tracks from previous days
                                    for a while until the trackers are switched on which could be annoying.</span>
                                <p>
                                    <asp:LinkButton runat="server" ID="advancedCleanUpShowHideLinkButton" Text="<%$ Resources: Resources, ShowAdvPointsCleanUp %>"
                                        OnClick="advancedCleanUpShowHideLinkButton_Click"></asp:LinkButton>
                                    <asp:Panel runat="server" ID="advancedCleanUpPanel" Visible="false">
                                        <span style="font-size: small">Below are buttons for the same clean-up as described
                                            right above, but those buttons set the threshold not just to now, but to now minus
                                            some hours: </span>
                                        <br />
                                        <asp:Button runat="server" ID="clearOldPointsButton_1_hr" Text="Hide points older than NOW minus 1 hour"
                                            OnClick="clearOldPointsButton_Click" OnClientClick="return confirm('Are you sure you want to hide all points older than NOW minus 1 hour?')" />
                                        <br />
                                        <asp:Button runat="server" ID="clearOldPointsButton_2_hr" Text="Hide points older than NOW minus 2 hours"
                                            OnClick="clearOldPointsButton_Click" OnClientClick="return confirm('Are you sure you want to hide all points older than NOW minus 2 hours?')" />
                                        <br />
                                        <asp:Button runat="server" ID="clearOldPointsButton_3_hr" Text="Hide points older than NOW minus 3 hours"
                                            OnClick="clearOldPointsButton_Click" OnClientClick="return confirm('Are you sure you want to hide all points older than NOW minus 3 hours?')" />
                                        <br />
                                        <asp:Button runat="server" ID="clearOldPointsButton_5_hr" Text="Hide points older than NOW minus 5 hours"
                                            OnClick="clearOldPointsButton_Click" OnClientClick="return confirm('Are you sure you want to hide all points older than NOW minus 5 hours?')" />
                                        <br />
                                        <asp:Button runat="server" ID="clearOldPointsButton_24_hr" Text="Hide points older than NOW minus 24 hours"
                                            OnClick="clearOldPointsButton_Click" OnClientClick="return confirm('Are you sure you want to hide all points older than NOW minus 1 day?')" />
                                        <br />
                                    </asp:Panel>
                                </p>
                                <asp:Panel ID="restoreOldPointsPanel" runat="server">
                                    <br />
                                    <asp:Button runat="server" ID="restoreOldPointsButton" Text="Restore old points"
                                        OnClick="restoreOldPointsPanel_Click" OnClientClick="return confirm('Are you sure you want to remove the threshold for old points and show all of them back?')" />
                                    <br />
                                    <span style="font-size: small">Restores all trackers on the map no matter how old their
                                        positions are - if a&nbsp;tracker is on its original SPOT Shared Page then it will be
                                        shown on the event's assigned maps after pressing this button. In other words, it
                                        cancels earlier click on the "<%=this.clearOldPointsButton.Text.ToUpper()%>" button
                                        above. </span>
                                </asp:Panel>
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
                                    <td class="WarningGroupTitle">
                                        Task is not displayed on any map yet
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        There are no pilots groups yet that are assigned to the task. You can assign pilot
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
                                        <td class="VisualGroupTitle">
                                            Pilot groups that use this task
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
</html>
