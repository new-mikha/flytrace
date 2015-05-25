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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="manageWaypoints.aspx.cs" StylesheetTheme="Default" Inherits="FlyTrace.ManageWaypointsForm" %>

<%@ Register TagPrefix="flyTrace" TagName="Deg" Src="~/CoordControls/Deg.ascx" %>
<%@ Register TagPrefix="flyTrace" TagName="DegMin" Src="~/CoordControls/DegMin.ascx" %>
<%@ Register TagPrefix="flyTrace" TagName="DegMinSec" Src="~/CoordControls/DegMinSec.ascx" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<script type="text/javascript" src="Scripts/jquery-1.11.3.min.js"></script>
<script type="text/javascript" src="Scripts/maintainScrollPosition.js"></script>
<script type="text/javascript">
    setScrollHiddenInputId('<%= scrollHiddenField.ClientID %>');
</script>
<head runat="server">
    <title>Waypoints available for the task</title>
    <script type="text/javascript">
        function confirmDeletingWaypoint(ctrl)
        {
            var parent = ctrl.parentNode;
            var origClassName;
            var origBckgColor;
            while (parent != null)
            {
                if (parent.tagName == 'TR')
                {
                    origClassName = parent.className;
                    origBckgColor = parent.style.backgroundColor;

                    // the row might be already selected, so use a style that differs from multi-select style 
                    // used in selectDeselectRow, as well as from "just added" color 
                    parent.className = "WarningRow";
                    parent.style.backgroundColor = "";

                    break;
                }
                parent = parent.parentNode;
            }

            var result = confirm('Are you certain you want to delete the waypoint from the event?');

            if (!result && (parent != null))
            {
                parent.className = origClassName;
                parent.style.backgroundColor = origBckgColor;
            }

            return result;
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <div align="left">
        <asp:HiddenField ID="scrollHiddenField" runat="server" />
        <table style="text-align: left">
            <%--Top level row: "Go back home"--%><tr>
                <td>
                    <table style="width: 100%;" class="UserInfo">
                        <tr>
                            <td>
                                <a href="default.aspx">FlyTrace</a>&nbsp;&gt;
                                <asp:MultiView runat="server" ActiveViewIndex="<%$ FtCode: FlyTrace.Global.IsSimpleEventsModel ? 0: 1%>">
                                    <asp:View runat="server">
                                        Available Waypoints
                                    </asp:View>
                                    <asp:View runat="server">
                                        <a href="manageEvent.aspx?event=<%=EventId%>">&#39;<%=EventName%>&#39;&nbsp;today task</a>&nbsp;&gt;&nbsp;Available Waypoints
                                    </asp:View>
                                </asp:MultiView>
                            </td>
                            <td style="text-align: right; vertical-align: top">
                                <b>
                                    <asp:LoginName ID="LoginName1" runat="Server"></asp:LoginName>
                                </b>&nbsp;-&nbsp;<a href="profile.aspx">Settings</a>&nbsp;-&nbsp;<asp:LinkButton ID="SignOutLinkButton" runat="server" OnClick="SignOutLinkButton_Click">Sign out</asp:LinkButton>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <asp:Panel ID="manageEventLinkTopPanel" runat="server" EnableViewState="false">
                <tr>
                    <td>
                        <br />
                        <a href="manageEvent.aspx?event=<%=EventId%>">View/edit Today Task...</a>
                    </td>
                </tr>
            </asp:Panel>
            <%--Top-level row: "Load wpts" />--%><tr>
                <td>
                    <br />
                    <table width="100%" class="GroupTable">
                        <tr>
                            <td class="VisualGroupTitle">
                                <asp:MultiView runat="server" ActiveViewIndex="<%$ FtCode: FlyTrace.Global.IsSimpleEventsModel ? 0: 1%>">
                                    <asp:View runat="server">
                                        Load Competiton Waypoints
                                    </asp:View>
                                    <asp:View runat="server">
                                        Load Waypoints to &#39;<%=EventName%>&#39;
                                    </asp:View>
                                </asp:MultiView>
                            </td>
                        </tr>
                        <tr style="display: <%= string.IsNullOrEmpty(LoadWaypointsMsg) ? "none" : ""%>;">
                            <td>
                                <div class="InfoMessage">
                                    <asp:Label ID="Label2" runat="server"><%= LoadWaypointsMsg %></asp:Label>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                This should be a Google Earth (*.KML) or SeeYou (*.CUP) file.<br />
                                Same as uploading waypoints to vario before a comp:<br />
                                <asp:FileUpload ID="fileUpload" runat="server" onchange="document.getElementById('uploadLbl').style.display = ''; __doPostBack('fileUpload','');" />
                                <p id="uploadLbl" class="InfoMessage" style="display: none">
                                    <asp:Image ID="Image1" runat="server" ImageUrl="~/App_Themes/Default/hourglass.gif" />
                                    Uploading &amp; processing the file, please wait...
                                </p>
                                <asp:RadioButtonList ID="rbExistingWptUploadRule" runat="server">
                                    <asp:ListItem Value="skip" Selected="True">Skip existing waypoints (matched by name)</asp:ListItem>
                                    <asp:ListItem Value="replace">Replace existing waypoints (matched by name) with new data</asp:ListItem>
                                </asp:RadioButtonList>
                                <asp:Button ID="deleteBtn" runat="server" OnClick="deleteBtn_Click" Text="Delete all waypoints" OnClientClick="return confirm('Are you certain you want to delete ALL LOADED WAYPOINTS?');" />
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    Coordinates format:&nbsp;<asp:DropDownList AutoPostBack="true" ID="coordFormatTopDropDownList" runat="server" OnSelectedIndexChanged="coordFormatDropDownList_SelectedIndexChanged">
                        <asp:ListItem Value="Deg">DD.DDDDDD&deg;</asp:ListItem>
                        <asp:ListItem Value="DegMin">DD&deg;MM.MMM&#39;</asp:ListItem>
                        <asp:ListItem Value="DegMinSec">DD&deg;MM&#39;SS.S&quot;</asp:ListItem>
                    </asp:DropDownList>
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    <table width="100%" class="GroupTable">
                        <tr>
                            <td class="VisualGroupTitle">
                                <asp:MultiView runat="server" ActiveViewIndex="<%$ FtCode: FlyTrace.Global.IsSimpleEventsModel ? 0: 1%>">
                                    <asp:View runat="server">
                                        Waypoints available for <a href="manageEvent.aspx?event=<%=EventId%>">Today Task</a>
                                    </asp:View>
                                    <asp:View runat="server">
                                        Waypoints available for tasks in &#39;<%=EventName%>&#39;
                                    </asp:View>
                                </asp:MultiView>
                            </td>
                        </tr>
                        <tr style="display: <%= string.IsNullOrEmpty(UpdateWaypointsMsg) ? "none" : ""%>;">
                            <td>
                                <div class="InfoMessage">
                                    <asp:Label ID="Label3" runat="server"><%= UpdateWaypointsMsg %></asp:Label>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:SqlDataSource ID="waypointsDataSource" runat="server" ConnectionString="<%$ ConnectionStrings:TrackerConnectionString %>" SelectCommand="SELECT [Id], [EventId], [Name], [Lat], [Lon], [Alt], [Description] FROM [Waypoint] WHERE ([EventId] = @EventId)" InsertCommand="INSERT INTO [Waypoint]([EventId], [Name], [Lat], [Lon], [Alt], [Description]) VALUES (@EventId, @Name, @Lat, @Lon, @Alt, @Description)" UpdateCommand="UPDATE [Waypoint] SET Name = @Name, [Lat]=@Lat, [Lon]=@Lon, [Alt]=@Alt, [Description]=@Description WHERE ([Id] = @Id)" DeleteCommand="DELETE FROM [Waypoint] WHERE ([Id] = @Id)" OnInserted="waypointsDataSource_Inserted" OnUpdated="waypointsDataSource_Updated" OnDeleted="waypointsDataSource_Deleted">
                                    <SelectParameters>
                                        <flytrace_tools:EvalParameter Name="EventId" Expression="EventId" />
                                    </SelectParameters>
                                    <DeleteParameters>
                                        <asp:Parameter Name="Id" />
                                    </DeleteParameters>
                                    <UpdateParameters>
                                        <asp:Parameter Name="Name" />
                                        <asp:Parameter Name="Lat" />
                                        <asp:Parameter Name="Lon" />
                                        <asp:Parameter Name="Alt" />
                                        <asp:Parameter Name="Description" />
                                        <asp:Parameter Name="Id" />
                                    </UpdateParameters>
                                    <InsertParameters>
                                        <flytrace_tools:EvalParameter Name="EventId" Expression="EventId" />
                                        <asp:Parameter Name="Name" />
                                        <asp:Parameter Name="Lat" />
                                        <asp:Parameter Name="Lon" />
                                        <asp:Parameter Name="Alt" />
                                        <asp:Parameter Name="Description" />
                                    </InsertParameters>
                                </asp:SqlDataSource>
                                <asp:GridView EnableViewState="False" Width="100%" DataKeyNames="Id" ID="waypointsGridView" runat="server" AutoGenerateColumns="False" DataSourceID="waypointsDataSource" GridLines="None" AllowSorting="True" OnRowUpdated="waypointsGridView_RowUpdated" OnRowDataBound="waypointsGridView_RowDataBound" OnDataBound="waypointsGridView_DataBound" OnRowDeleted="waypointsGridView_RowDeleted" OnRowDeleting="waypointsGridView_RowDeleting" OnRowUpdating="waypointsGridView_RowUpdating">
                                    <EmptyDataTemplate>
                                        There are no waypoints at the moment.<br />
                                        Use Upload command above, or Add Waypoint command below.
                                    </EmptyDataTemplate>
                                    <Columns>
                                        <asp:TemplateField HeaderText="#" ItemStyle-VerticalAlign="Top">
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
                                        <asp:TemplateField HeaderText="Wpt Name" SortExpression="Name" ItemStyle-VerticalAlign="Top">
                                            <ItemTemplate>
                                                <a href="http://maps.google.com.au/maps?q=<%# Eval("Lat")%>,<%# Eval("Lon")%>&z=10" target="_blank">
                                                    <asp:Label ID="Label3" runat="server" Text='<%# Bind("Name") %>'></asp:Label></a>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="nameTextBox" runat="server" Text='<%# Bind("Name") %>'></asp:TextBox>
                                                <asp:RequiredFieldValidator ControlToValidate="nameTextBox" ID="RequiredFieldValidator1" runat="server" ErrorMessage="&lt;br /&gt;Waypoint name cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
                                            </EditItemTemplate>
                                            <HeaderStyle HorizontalAlign="Left" />
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                &nbsp;&nbsp;
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Lat" SortExpression="Lat" ItemStyle-VerticalAlign="Top">
                                            <ItemTemplate>
                                                <asp:Label EnableViewState="false" ID="latLabel" runat="server" Text='<%# Bind("Lat") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:MultiView ID="latEditMultiView" runat="server" ActiveViewIndex="<%$ FtCode: EditViewIndex %>" EnableViewState="false">
                                                    <asp:View runat="server">
                                                        <flyTrace:Deg ID="latDeg" runat="server" CoordType="Latitude" ValidationGroup="rowEdit" />
                                                    </asp:View>
                                                    <asp:View runat="server">
                                                        <flyTrace:DegMin ID="latDegMin" runat="server" CoordType="Latitude" ValidationGroup="rowEdit" />
                                                    </asp:View>
                                                    <asp:View runat="server">
                                                        <flyTrace:DegMinSec ID="latDegMinSec" runat="server" CoordType="Latitude" ValidationGroup="rowEdit" />
                                                    </asp:View>
                                                </asp:MultiView>
                                            </EditItemTemplate>
                                            <HeaderStyle HorizontalAlign="Left" />
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                &nbsp;&nbsp;</ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Lon" SortExpression="Lon" ItemStyle-VerticalAlign="Top">
                                            <ItemTemplate>
                                                <asp:Label EnableViewState="false" ID="lonLabel" runat="server" Text='<%# Bind("Lon") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:MultiView ID="lonEditMultiView" runat="server" ActiveViewIndex="<%$ FtCode: EditViewIndex %>" EnableViewState="false">
                                                    <asp:View runat="server">
                                                        <flyTrace:Deg ID="lonDeg" runat="server" CoordType="Longitude" ValidationGroup="rowEdit" />
                                                    </asp:View>
                                                    <asp:View runat="server">
                                                        <flyTrace:DegMin ID="lonDegMin" runat="server" CoordType="Longitude" ValidationGroup="rowEdit" />
                                                    </asp:View>
                                                    <asp:View runat="server">
                                                        <flyTrace:DegMinSec ID="lonDegMinSec" runat="server" CoordType="Longitude" ValidationGroup="rowEdit" />
                                                    </asp:View>
                                                </asp:MultiView>
                                            </EditItemTemplate>
                                            <HeaderStyle HorizontalAlign="Left" />
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                &nbsp;&nbsp;</ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Alt" SortExpression="Alt" ItemStyle-VerticalAlign="Top">
                                            <ItemTemplate>
                                                <asp:Label ID="Label6" runat="server" Text='<%# Bind("Alt") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="altTextBox" runat="server" Text='<%# Bind("Alt") %>'></asp:TextBox>
                                                <asp:RequiredFieldValidator ControlToValidate="altTextBox" ID="RequiredFieldValidator4" runat="server" ErrorMessage="&lt;br /&gt;Altitude cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
                                            </EditItemTemplate>
                                            <HeaderStyle HorizontalAlign="Left" />
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                &nbsp;&nbsp;</ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Description" SortExpression="Description" ItemStyle-VerticalAlign="Top">
                                            <ItemTemplate>
                                                <asp:Label ID="Label7" runat="server" Text='<%# Bind("Description") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="descrTextBox" runat="server" Text='<%# Bind("Description") %>'></asp:TextBox>
                                            </EditItemTemplate>
                                            <HeaderStyle HorizontalAlign="Left" />
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                &nbsp;&nbsp;</ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField ShowHeader="False">
                                            <EditItemTemplate>
                                                <asp:LinkButton ID="LinkButton3" runat="server" CausesValidation="True" CommandName="Update" Text="Update"></asp:LinkButton>
                                            </EditItemTemplate>
                                            <ItemTemplate>
                                                <asp:LinkButton ID="LinkButton4" runat="server" CausesValidation="False" CommandName="Edit" Text="Edit"></asp:LinkButton>
                                            </ItemTemplate>
                                            <ItemStyle VerticalAlign="Top" />
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                &nbsp;&nbsp;</ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField ShowHeader="False">
                                            <EditItemTemplate>
                                                <asp:LinkButton ID="LinkButton5" runat="server" CausesValidation="False" CommandName="Cancel" Text="Cancel"></asp:LinkButton>
                                            </EditItemTemplate>
                                            <ItemTemplate>
                                                <asp:LinkButton ID="LinkButton6" runat="server" CausesValidation="False" CommandName="Delete" Text="Delete" OnClientClick="return confirmDeletingWaypoint(this);"></asp:LinkButton>
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
            <tr>
                <td>
                    Coordinates format:&nbsp;<asp:DropDownList AutoPostBack="true" ID="coordFormatBottomDropDownList" runat="server" OnSelectedIndexChanged="coordFormatDropDownList_SelectedIndexChanged">
                        <asp:ListItem Value="Deg">DD.DDDDDD&deg;</asp:ListItem>
                        <asp:ListItem Value="DegMin">DD&deg;MM.MMM&#39;</asp:ListItem>
                        <asp:ListItem Value="DegMinSec">DD&deg;MM&#39;SS.S&quot;</asp:ListItem>
                    </asp:DropDownList>
                    <br />
                    <br />
                </td>
            </tr>
            <tr>
                <td>
                    <table width="100%" class="GroupTable">
                        <tr>
                            <td class="VisualGroupTitle">
                                <asp:MultiView ID="MultiView1" runat="server" ActiveViewIndex="<%$ FtCode: FlyTrace.Global.IsSimpleEventsModel ? 0: 1%>">
                                    <asp:View ID="View1" runat="server">
                                        Add Waypoint Manually
                                    </asp:View>
                                    <asp:View ID="View2" runat="server">
                                        Add waypoint to &#39;<%=EventName%>&#39;
                                    </asp:View>
                                </asp:MultiView>
                            </td>
                        </tr>
                        <tr style="display: <%= string.IsNullOrEmpty(InsertWaypointMsg) ? "none" : ""%>;">
                            <td>
                                <div class="InfoMessage">
                                    <asp:Label ID="Label4" runat="server"><%= InsertWaypointMsg %></asp:Label></div>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:FormView ID="formView" runat="server" DataKeyNames="Id" DataSourceID="waypointsDataSource" DefaultMode="Insert" OnItemInserted="formView_ItemInserted" OnItemInserting="formView_ItemInserting" OnDataBound="formView_OnDataBound">
                                    <InsertItemTemplate>
                                        <table style="padding-right: 2px">
                                            <tr>
                                                <td style="vertical-align: top;">
                                                    Waypoint Name:
                                                </td>
                                                <td>
                                                    <asp:TextBox Width="100%" ID="NameTextBox" runat="server" Text='<%# Bind("Name") %>' />
                                                    <asp:RequiredFieldValidator ValidationGroup="formView" ControlToValidate="NameTextBox" ID="RequiredFieldValidator5" runat="server" ErrorMessage="&lt;br /&gt;Waypoint name cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
                                                </td>
                                            </tr>
                                            <asp:MultiView runat="server" ID="addWptFormatMultiView">
                                                <asp:View runat="server" ID="addWptDeg">
                                                    <tr>
                                                        <td style="vertical-align: top;">
                                                            Latitude:<br />
                                                            <span style="font-size: smaller">(&plusmn;DD.DDDDDD&deg;)</span>
                                                        </td>
                                                        <td style="vertical-align: top;">
                                                            <flyTrace:Deg ID="latDeg" runat="server" CoordType="Latitude" ValidationGroup="formView" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td style="vertical-align: top;">
                                                            Longitude:<br />
                                                            <span style="font-size: smaller">(&plusmn;DD.DDDDDD&deg;)</span>
                                                        </td>
                                                        <td style="vertical-align: top;">
                                                            <flyTrace:Deg ID="lonDeg" runat="server" CoordType="Longitude" ValidationGroup="formView" />
                                                        </td>
                                                    </tr>
                                                </asp:View>
                                                <asp:View runat="server" ID="addWptDegMin">
                                                    <tr>
                                                        <td style="vertical-align: top;">
                                                            Latitude:<br />
                                                            <span style="font-size: smaller">(DD&deg;MM.MMM&#39;)</span>
                                                        </td>
                                                        <td style="vertical-align: top;">
                                                            <flyTrace:DegMin ID="latDegMin" runat="server" CoordType="Latitude" ValidationGroup="formView" DefHemisphere="<%$ FtCode: FlyTrace.Global.DefHemisphereNS %>" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td style="vertical-align: top;">
                                                            Longitude:<br />
                                                            <span style="font-size: smaller">(DDD&deg;MM.MMM&#39;)</span>
                                                        </td>
                                                        <td style="vertical-align: top;">
                                                            <flyTrace:DegMin ID="lonDegMin" runat="server" CoordType="Longitude" ValidationGroup="formView" DefHemisphere="<%$ FtCode: FlyTrace.Global.DefHemisphereEW %>" />
                                                        </td>
                                                    </tr>
                                                </asp:View>
                                                <asp:View runat="server" ID="addWptDegMinSec">
                                                    <tr>
                                                        <td style="vertical-align: top;">
                                                            Latitude:<br />
                                                            <span style="font-size: smaller">(DD&deg;&nbsp;MM&#39;&nbsp;SS.S&quot;)</span>
                                                        </td>
                                                        <td style="vertical-align: top;">
                                                            <flyTrace:DegMinSec ID="latDegMinSec" runat="server" CoordType="Latitude" ValidationGroup="formView" DefHemisphere="<%$ FtCode: FlyTrace.Global.DefHemisphereNS %>" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td style="vertical-align: top;">
                                                            Longitude:<br />
                                                            <span style="font-size: smaller">(DDD&deg;&nbsp;MM&#39;&nbsp;SS.S&quot;)</span>
                                                        </td>
                                                        <td style="vertical-align: top;">
                                                            <flyTrace:DegMinSec ID="lonDegMinSec" runat="server" CoordType="Longitude" ValidationGroup="formView" DefHemisphere="<%$ FtCode: FlyTrace.Global.DefHemisphereEW %>" />
                                                        </td>
                                                    </tr>
                                                </asp:View>
                                            </asp:MultiView>
                                            <tr>
                                                <td style="vertical-align: top;">
                                                    Altitude:<br />
                                                    <span style="font-size: smaller">(meters, not used yet)</span>
                                                </td>
                                                <td style="vertical-align: top;">
                                                    <asp:TextBox Width="100%" ID="AltTextBox" runat="server" Text='<%# Bind("Alt") %>' />
                                                    <asp:RequiredFieldValidator ValidationGroup="formView" ControlToValidate="AltTextBox" ID="RequiredFieldValidator8" runat="server" ErrorMessage="Altitude cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="vertical-align: top;">
                                                    Description<br />
                                                    <span style="font-size: smaller">(optional)</span>
                                                </td>
                                                <td style="vertical-align: top;">
                                                    <asp:TextBox Width="100%" ID="DescrTextBox" runat="server" Text='<%# Bind("Description") %>' />
                                                </td>
                                            </tr>
                                        </table>
                                        <asp:Button ID="InsertButton" runat="server" CausesValidation="True" ValidationGroup="formView" CommandName="Insert" Text="Add Waypoint" />
                                    </InsertItemTemplate>
                                </asp:FormView>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    <asp:Panel ID="manageEventLinlBottomPanel" runat="server" EnableViewState="false">
                        <a href="manageEvent.aspx?event=<%=EventId%>">View/edit Today Task...</a>
                    </asp:Panel>
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
