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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="currentTrackers.aspx.cs"
    Inherits="FlyTrace.Service.Administration.currentTrackers" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<script type="text/javascript" src="jquery-1.7.1.min.js"></script>
<script type="text/javascript" src="tools.js"></script>
<script type="text/javascript">
    setScrollHiddenInputId('<%= scrollHiddenField.ClientID %>');
</script>
<head runat="server">
    <link href="../../App_Themes/Default/site2.css" type="text/css" rel="stylesheet" />
    <title>Current Trackers State - FlyTrace</title>
</head>
<body>
    <form id="form1" runat="server">
    <asp:HiddenField ID="scrollHiddenField" runat="server" />
    <div>
        <div style="display: inline-block" class="GroupTable2">
            <div class="VisualGroupTitle2">
                Statistics
            </div>
            <div style="margin-top: 0.5em">
                <asp:Panel runat="server" ID="statPanel" EnableViewState="False">
                </asp:Panel>
            </div>
        </div>

        <table style="margin-top: 1em">
            <tr>
                <td>
                    <table style="width: 100%;" class="UserInfo">
                        <tr>
                            <td>
                                <a href="../../default.aspx">FlyTrace</a>&nbsp;&gt;&nbsp;Current Trackers State
                            </td>
                            <td style="text-align: right">
                                <b>
                                    <asp:LoginName ID="LoginName1" runat="Server"></asp:LoginName>
                                </b>&nbsp;-&nbsp;<a href="../../profile.aspx">Settings</a>&nbsp;-&nbsp;<asp:LinkButton
                                    ID="SignOutLinkButton" runat="server" OnClick="SignOutLinkButton_Click">Sign out</asp:LinkButton>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Button ID="refreshButton" runat="server" Text="Refresh" 
                        OnClick="refreshButton_Click" AccessKey="R" />
                    <br />
                    SPOT Id Filter:&nbsp;<asp:TextBox runat="server" ID="idFilterTextBox"></asp:TextBox>&nbsp;
                    <asp:Button ID="applyFilterButton" runat="server" Text="Apply Filter" OnClick="applyFilterButton_Click" />
                    <span style="color: Red">
                        <asp:Literal ID="errorLiteral" runat="server" EnableViewState="false"></asp:Literal>
                    </span>
                    <p>
                        <asp:GridView Width="100%" ID="trackersGridView" runat="server" AutoGenerateColumns="False"
                            AllowSorting="True" DataKeyNames="SpotId" EnableModelValidation="True" OnRowDataBound="trackersGridView_RowDataBound"
                            OnSorting="trackersGridView_Sorting">
                            <AlternatingRowStyle CssClass="AlternatingRowStyle" />
                            <RowStyle CssClass="RowStyle" />
                            <Columns>
                                <asp:TemplateField HeaderText="#">
                                    <ItemTemplate>
                                        <%# Container.DataItemIndex + 1 %>
                                    </ItemTemplate>
                                    <HeaderStyle HorizontalAlign="Left" />
                                </asp:TemplateField>
                                <asp:TemplateField>
                                    <ItemTemplate>
                                        <asp:CheckBox runat="server" ID="selectionCheckBox" AutoPostBack="True" OnCheckedChanged="selectionCheckBox_CheckedChanged" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Spot Id" HeaderStyle-HorizontalAlign="Left" SortExpression="SpotId">
                                    <ItemTemplate>
                                        <%# string.Format( "<a href='http://share.findmespot.com/shared/faces/viewspots.jsp?glId={0}' target='_blank'>{0}</a>" , Eval( "SpotId" ) )%>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Current" ItemStyle-HorizontalAlign="Center" SortExpression="CurrentTs">
                                    <ItemTemplate>
                                        <asp:Label ID="currentCoord" runat="server" Text='<%# Bind("CurrentCoord") %>'></asp:Label>
                                        <br />
                                        <asp:Label ID="Label1" runat="server" Text='<%# Bind("CurrentTsStr") %>'></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Previous" ItemStyle-HorizontalAlign="Center" SortExpression="PrevTs">
                                    <ItemTemplate>
                                        <asp:Label ID="prevCoord" runat="server" Text='<%# Bind("PrevCoord") %>'></asp:Label>
                                        <br />
                                        <asp:Label ID="prevTs" runat="server" Text='<%# Bind("PrevTsStr") %>'></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Error" ItemStyle-HorizontalAlign="Center" SortExpression="Error">
                                    <ItemTemplate>
                                        <asp:Label ID="error" runat="server" Text='<%# Bind("Error") %>'></asp:Label>
                                        <br />
                                        <asp:Label ID="errorTag" runat="server" Text='<%# Bind("ErrorTag") %>'></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Access Time" ItemStyle-HorizontalAlign="Center" SortExpression="AccessTime">
                                    <ItemTemplate>
                                        <asp:Label ID="accessTime" runat="server" Text='<%# Bind("AccessTimeStr") %>'></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="State Time" ItemStyle-HorizontalAlign="Center" SortExpression="CreateTime">
                                    <ItemTemplate>
                                        <asp:Label ID="createTime" runat="server" Text='<%# Bind("CreateTimeStr") %>'></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Refresh Time" ItemStyle-HorizontalAlign="Center" SortExpression="RefreshTime">
                                    <ItemTemplate>
                                        <asp:Label ID="refreshTime" runat="server" Text='<%# Bind("RefreshTimeStr") %>'></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Revision" ItemStyle-HorizontalAlign="Center" SortExpression="Revision">
                                    <ItemTemplate>
                                        <asp:Label ID="revision" runat="server" Text='<%# Bind("Revision") %>'></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Tag" ItemStyle-HorizontalAlign="Center" SortExpression="Tag">
                                    <ItemTemplate>
                                        <asp:Label ID="tag" runat="server" Text='<%# Bind("Tag") %>'></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                            <SelectedRowStyle BackColor="#00CCFF" />
                        </asp:GridView>
                    </p>
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
