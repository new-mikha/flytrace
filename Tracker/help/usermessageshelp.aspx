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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="usermessageshelp.aspx.cs" Inherits="FlyTrace.help.usermessageshelp" StylesheetTheme="Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div align="left" style="padding-left: 5px; padding-bottom: 10px">
        <table style="text-align: left">
            <tr>
                <td>
                    <table style="width: 100%;" class="UserInfo">
                        <tr>
                            <td>
                                <a href="../default.aspx">FlyTrace</a>&nbsp;&gt;&nbsp;Help
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td>
                    <h3>
                        Owner-defined messages
                    </h3>
                    SPOT owners define their own messages for OK, CUSTOM and HELP messages using their accounts on SPOT website.<br />
                    Showing these messages here is disabled by default, but you may turn it on for your groups. In the example below quoted<br />
                    message in <i>Italic</i> font is such owner-defined message:
                    <br />
                    <img alt="" src="userMsgHelp.png" />
                    <br />
                    To enable owner-defined messages for a group, edit your group details (same place where you edit your group name).<br />
                    You can also enable it by default for <b>new groups</b>, this can be done in your <a href="../profile.aspx">profile</a>.
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
