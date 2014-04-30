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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="manageUsers.aspx.cs" Inherits="FlyTrace.manageUsers"
    Theme="Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Manage Users - FlyTrace</title>
    <script type="text/javascript">
        function selectDeselectRow(checkbox)
        {
            var parent = checkbox.parentNode;
            while (parent != null)
            {
                if (parent.tagName == 'TR') break;
                parent = parent.parentNode;
            }

            if (parent != null)
            {
                if (checkbox.checked)
                {
                    _numberOfSelectedUsers++;
                    parent.className = "SelectedRow";
                } else
                {
                    _numberOfSelectedUsers--;
                    parent.className = "";
                }
            }

            var button = document.getElementById("deleteSelectedButton");
            if (_numberOfSelectedUsers > 0)
            {
                button.disabled = "";
            } else
            {
                button.disabled = "disabled";
            }
        }

        function confirmDeletingUsers()
        {
            if (confirm('Are you certain you want to delete selected users?'))
            {
                if (confirm('You are about to DELETE USERS AND THEIR STUFF COMPLETELY.'))
                {
                    return confirm('This is a final confirmation. Double check that you really want to delete the selected users and then press OK to DELETE.')
                }
            }
            return false;
        }

        function confirmDeletingSingleUser(ctrl)
        {
            var parent = ctrl.parentNode;
            var origClassName;
            while (parent != null)
            {
                if (parent.tagName == 'TR')
                {
                    origClassName = parent.className;

                    // the row might be already selected, so use a style that differs from multi-select style used in selectDeselectRow
                    parent.className = "WarningRow";

                    break;
                }
                parent = parent.parentNode;
            }

            var result = false;
            if (confirm('Are you certain you want to delete the user?'))
            {
                result = confirm('You are about to delete the user AND ALL HIS STUFF!!!');
            }

            if (!result && (parent != null))
            {
                parent.className = origClassName;
            }

            return result;
        }

        function initialize()
        {
            var button = document.getElementById("deleteSelectedButton");
            if (_numberOfSelectedUsers == 0)
            {
                button.disabled = "disabled";
            }
        }
    </script>
</head>
<%
    Response.Write( "<script type='text/javascript'>\n" );
    Response.Write( string.Format( "\tvar _numberOfSelectedUsers = {0};\n", this.NumOfSelectedRows ) );
    Response.Write( "</script>\n" );
%>
<body onload="initialize()">
    <form id="form1" runat="server">
    <div>
        <table>
            <tr>
                <td>
                    <table style="width: 100%;" class="UserInfo">
                        <tr>
                            <td>
                                <a href="../default.aspx">FlyTrace</a>&nbsp;&gt;&nbsp;Manage Users
                            </td>
                            <td style="text-align: right">
                                <b>
                                    <asp:LoginName ID="LoginName1" runat="Server"></asp:LoginName>
                                </b>&nbsp;-&nbsp;<a href="../profile.aspx">Settings</a>&nbsp;-&nbsp;<asp:LinkButton
                                    ID="SignOutLinkButton" runat="server" OnClick="SignOutLinkButton_Click">Sign out</asp:LinkButton>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td>
                    <p>
                        <asp:Repeater ID="filteringUI" runat="server" OnItemCommand="filteringUI_ItemCommand"
                            OnItemDataBound="filteringUI_ItemDataBound">
                            <ItemTemplate>
                                <asp:LinkButton Enabled="false" runat="server" ID="lnkFilter" Text='<%# Container.DataItem %>'
                                    CommandName='<%# Container.DataItem %>'></asp:LinkButton>
                            </ItemTemplate>
                            <SeparatorTemplate>
                                |</SeparatorTemplate>
                        </asp:Repeater>
                    </p>
                </td>
            </tr>
            <tr>
                <td>
                    Custom Name Filter:&nbsp;<asp:TextBox runat="server" ID="customFilterTextBox"></asp:TextBox>&nbsp;
                    <asp:Button ID="applyCustomFilterButton" runat="server" Text="Apply Custom Filter"
                        OnClick="applyCustomFilterButton_Click" />
                    <span style="color: Red">
                        <asp:Literal ID="errorLiteral" runat="server" EnableViewState="false"></asp:Literal>
                    </span>
                    <p>
                        Number of filtered users (filter string: '<asp:Label ID="currentFilterLabel" runat="server"
                            Font-Bold="True"></asp:Label>', where '%' means 'any symbols')
                        <asp:Label ID="usersCountLabel" runat="server" Font-Bold="True"></asp:Label>
                    </p>
                    <p>
                        <asp:GridView Width="100%" ID="userAccounts" runat="server" AutoGenerateColumns="False"
                            AllowSorting="True" DataKeyNames="UserName" EnableModelValidation="True" OnRowDeleting="userAccounts_RowDeleting"
                            OnRowDataBound="userAccounts_RowDataBound" OnSorting="userAccounts_Sorting">
                            <AlternatingRowStyle CssClass="AlternatingRowStyle" />
                            <RowStyle CssClass="RowStyle" />
                            <Columns>
                                <asp:TemplateField>
                                    <HeaderTemplate>
                                        <%--<input id="headerSelectionCheckBox" type="checkbox" />--%>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox runat="server" ID="selectionCheckBox" onclick="selectDeselectRow(this)" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="UserName" HeaderText="UserName ^" SortExpression="UserName" />
                                <asp:BoundField DataField="Email" HeaderText="Email" SortExpression="Email" />
                                <asp:BoundField DataField="CreationDate" HeaderText="Created" SortExpression="CreationDate" />
                                <asp:BoundField DataField="LastActivityDate" HeaderText="Prev.Activity" SortExpression="LastActivityDate" />
                                <asp:CheckBoxField ItemStyle-HorizontalAlign="Center" DataField="IsLockedOut" HeaderText="Locked"
                                    SortExpression="IsLockedOut" />
                                <asp:CheckBoxField ItemStyle-HorizontalAlign="Center" DataField="IsOnline" HeaderText="Is Online"
                                    SortExpression="IsOnline" />
                                <%-- <asp:BoundField DataField="LastLoginDate" HeaderText="Prev.Login" />
                <asp:CheckBoxField DataField="IsApproved" HeaderText="Approved?" />
                <asp:CheckBoxField DataField="IsLockedOut" HeaderText="Locked Out?" /> --%>
                                <asp:TemplateField HeaderText="Groups" ItemStyle-HorizontalAlign="Center" SortExpression="Groups">
                                    <ItemTemplate>
                                        <asp:Label ID="numberOfGroupsLabel" runat="server"></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Unique Trackers" ItemStyle-HorizontalAlign="Center"
                                    SortExpression="Trackers">
                                    <ItemTemplate>
                                        <asp:Label ID="numberOfUniqTrackers" runat="server"></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:HyperLinkField DataNavigateUrlFields="UserName" Target="_blank" DataNavigateUrlFormatString="userInformation.aspx?user={0}"
                                    Text="Manage" />
                                <asp:TemplateField ShowHeader="False">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="LinkButton1" runat="server" CausesValidation="False" CommandName="Delete"
                                            Text="Delete" OnClientClick="return confirmDeletingSingleUser(this)"></asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                            <SelectedRowStyle BackColor="#00CCFF" />
                        </asp:GridView>
                    </p>
                    <asp:Button ID="deleteSelectedButton" runat="server" Text="Delete Selected Users"
                        OnClick="deleteSelectedButton_Click" OnClientClick="return confirmDeletingUsers()" />
                    <p>
                        Page size:<asp:DropDownList ID="dropDownListPageSize" runat="server" AutoPostBack="True"
                            OnSelectedIndexChanged="dropDownListPageSize_SelectedIndexChanged">
                            <asp:ListItem Text="10" Value="10" />
                            <asp:ListItem Text="20" Value="20" Selected="True" />
                            <asp:ListItem Text="50" Value="50" />
                            <asp:ListItem Text="100" Value="100" />
                            <asp:ListItem Text="Single Page" Value="0" />
                        </asp:DropDownList>
                    </p>
                    <p>
                        <asp:LinkButton ID="lnkFirst" runat="server" OnClick="lnkFirst_Click">&lt;&lt;&nbsp;First</asp:LinkButton>&nbsp;|&nbsp;
                        <asp:LinkButton ID="lnkPrev" runat="server" OnClick="lnkPrev_Click">&lt;&nbsp;Prev</asp:LinkButton>&nbsp;|
                        Page:
                        <asp:DropDownList ID="dropDownListPageNumber" runat="server" AutoPostBack="True"
                            OnSelectedIndexChanged="dropDownListPageNumber_SelectedIndexChanged">
                        </asp:DropDownList>
                        &nbsp;/&nbsp;<asp:Literal runat="server" ID="literalTotalRecords"></asp:Literal>
                        &nbsp;|&nbsp;
                        <asp:LinkButton ID="lnkNext" runat="server" OnClick="lnkNext_Click">Next&nbsp;&gt;</asp:LinkButton>&nbsp;|&nbsp;
                        <asp:LinkButton ID="lnkLast" runat="server" OnClick="lnkLast_Click">Last&nbsp;&gt;&gt;</asp:LinkButton>
                    </p>
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
