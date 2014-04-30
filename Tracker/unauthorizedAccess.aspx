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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="unauthorizedAccess.aspx.cs"
    Inherits="FlyTrace.unauthorizedAccess" Theme="Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <table style="text-align: left">
            <tr>
                <td>
                    <table style="width: 100%;" class="UserInfo">
                        <tr>
                            <td>
                                <a href="default.aspx">FlyTrace</a>
                            </td>
                            <td style="text-align: right;">
                                <asp:LoginView ID="loginView" runat="server">
                                    <LoggedInTemplate>
                                        <b>
                                            <asp:LoginName ID="LoginName2" runat="Server"></asp:LoginName>
                                        </b>&nbsp;-&nbsp;<a href="profile.aspx">Settings</a>&nbsp;-&nbsp;<asp:LinkButton
                                            ID="LinkButton1" runat="server" OnClick="SignOutLinkButton_Click">Sign out</asp:LinkButton>
                                    </LoggedInTemplate>
                                    <AnonymousTemplate>
                                        Not logged in - <a href="login.aspx">Log in</a>
                                    </AnonymousTemplate>
                                </asp:LoginView>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td>
                    <h2>
                        Unauthorized Access</h2>
                    <p>
                        You have attempted to access a page that you are not authorized to view.
                    </p>
                    <p>
                        If you have any questions, please contact the site administrator: <a href='mailto:<%=AdminEmail%>'>
                            <%=AdminEmail%></a>
                    </p>
                    <p>
                        Go to <a href="default.aspx">FlyTrace home</a>.
                    </p>
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
