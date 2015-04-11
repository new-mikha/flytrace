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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TestControl.aspx.cs" Inherits="FlyTrace.Service.Test.TestControl" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Test Control - FlyTrace</title>
    <style type="text/css">
        .UserInfo
        {
            font-size: 11px;
            background-color: #46565F;
            color: #FFFFFF;
            font-family: Arial, Helvetica, sans-serif;
        }
        .UserInfo A:link, .UserInfo A:visited
        {
            color: #FFFFFF;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server" defaultbutton="SetPositionButton" defaultfocus="PositionNumTextBox">
    <div>
        <table style="text-align: left; width: 100%">
            <tr>
                <td>
                    <table style="width: 100%;" class="UserInfo">
                        <tr>
                            <td>
                                <a href="../../default.aspx">FlyTrace</a>&nbsp;&gt;&nbsp;Test Control
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Button ID="Button1" runat="server" Text="Refresh" />
                    <br />
                    <br />
                    <table>
                        <tr>
                            <td>
                                <asp:Panel ID="PositionControlsPanel" runat="server" Style="padding-left: 1em">
                                    <asp:CheckBox ID="IsAutoUpdateCheckBox" Text="New position on every update" runat="server" AutoPostBack="True" Checked="True" />
                                    <br />
                                    Current position #:
                                    <asp:TextBox Style="margin-left: 1em; margin-right: 1em" ID="PositionNumTextBox" runat="server"></asp:TextBox>
                                    <asp:Button ID="SetPositionButton" runat="server" Text="Set" onclick="SetPositionButton_Click" />
                                    <br />
                                    <br />
                                    <asp:Button ID="IncreaseByOneButton" runat="server" Text="Increase by 1" OnClick="IncreaseByOneButton_Click" />
                                    <asp:Button ID="IncreaseByTenButton" runat="server" Text="Increase by 10" OnClick="IncreaseByTenButton_Click" />
                                    <asp:Button ID="ResetButton" runat="server" Text="Reset to zero" OnClick="ResetButton_Click" />
                                </asp:Panel>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
