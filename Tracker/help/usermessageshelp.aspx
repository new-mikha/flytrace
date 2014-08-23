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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="usermessageshelp.aspx.cs"
    Inherits="FlyTrace.help.usermessageshelp" StylesheetTheme="Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div align="left" style="padding-left: 5px; width: 50em; padding-bottom: 10px">
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
                <td style="text-align: justify">
                    <h3>
                        Owner-defined messages
                    </h3>
                    SPOT owners can customize the OK, CUSTOM and HELP messages being sent from their devices
                    (to set up these messages, SPOT owners should use their profiles on the SPOT website). As an
                    owner of your groups you can enable showing those messages, like in the example
                    below:
                    <br />
                    <img alt="" src="userMsgHelp.png" />
                    <br />
                    But by default customized messages DO NOT COME THROUGH TO YOUR MAPS along
                    with the points and OK/CUSTOM/HELP statuses. To enable showing it in Info pop-ups
                    like shown above, edit the group details on the same page where you edit the group
                    name. You can also enable it by default for new groups, this can be done in your
                    <a href="../profile.aspx">profile</a>.
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
