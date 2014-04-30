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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="uiLogConfig.aspx.cs" Inherits="FlyTrace.Administration.uiLogConfig"
    Theme="Default" ValidateRequest="false" %>

<%@ Register TagPrefix="flyTrace" TagName="LogConfig" Src="~/Service/Administration/LogConfig.ascx" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>UI Log Config - FlyTrace</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <table style="width: 100%;">
            <tr>
                <td>
                    <table style="width: 100%;" class="UserInfo">
                        <tr>
                            <td>
                                <a href="../default.aspx">FlyTrace</a>&nbsp;&gt;&nbsp;Manage User Interface Logs
                                Configuration
                            </td>
                            <td style="text-align: right">
                                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
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
                    <div style="width: 100%; text-align: center; background-color: #FFFF00">
                        <h3>
                            UI Log Config</h3>
                    </div>
                    <flyTrace:LogConfig runat="server" />
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
