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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="userInformation.aspx.cs"
    Inherits="FlyTrace.userInformation" Theme="Default" EnableViewState="false" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>
        <%= Request["user"] %>
        User Info - FlyTrace</title>
    <script type="text/javascript">
        function confirmMakeAdmin()
        {
            return confirm('Are you sure you would like TO MAKE this user ADMIN?');
        }

        function confirmMakeNotAdmin()
        {
            return confirm('Are you sure you would like TO REVOKE admin rights from the user?');
        }

        function confirmUnlock()
        {
            return confirm('Unlock? Sure?');
        }

        function confirmLock()
        {
            return confirm('Lock him out? Sure?');
        }


    </script>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div style="width: 100%;" class="UserInfo">
            <a href="../default.aspx">FlyTrace</a>&nbsp;&gt;&nbsp;<a href="manageUsers.aspx">Manage
                Users</a>&nbsp;&gt;&nbsp;<%= Request["user"] %>
            User Info&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Logged
            as <b>
                <asp:LoginName ID="LoginName1" runat="Server"></asp:LoginName>
            </b>&nbsp;-&nbsp;<a href="../profile.aspx">Settings</a>&nbsp;-&nbsp;<asp:LinkButton
                ID="SignOutLinkButton" runat="server" OnClick="SignOutLinkButton_Click">Sign out</asp:LinkButton>
        </div>
        <span style="color: Red">
            <asp:Literal ID="errorLiteral" runat="server" EnableViewState="false"></asp:Literal></span>
        <asp:MultiView ID="mainMultiView" runat="server">
            <asp:View ID="normalView" runat="server">
                <h3>
                    '<%= Request["user"] %>' User Info</h3>
                <asp:Literal ID="roleLiteral" runat="server">User is NOT Administrator</asp:Literal>
                <br />
                <asp:Button ID="makeAdminButton" runat="server" Text="Make the user Admin" OnClick="adminButton_Click"
                    OnClientClick="return confirmMakeAdmin()" />
                <asp:Button ID="makeNotAdminButton" runat="server" Text="Make the user NOT Admin"
                    OnClientClick="return confirmMakeNotAdmin()" onclick="adminButton_Click" />
                <br />
                <asp:Button ID="unlockButton" runat="server" Text="Unlock the user" EnableViewState="false"
                    OnClientClick="return confirmUnlock()" OnClick="unlockButton_Click" />
            </asp:View>
            <asp:View ID="wrongUserView" runat="server">
                <h3>
                    Unable to find user <i>'<%= Request["user"] %>'</i></h3>
            </asp:View>
        </asp:MultiView>
    </div>
    </form>
</body>
</html>
