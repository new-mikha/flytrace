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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="commonhelp.aspx.cs" Inherits="FlyTrace.help.commonhelp"
    StylesheetTheme="Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
        .style1
        {
            font-size: smaller;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div align="left" style="padding-left: 5px; padding-bottom: 10px">
        <asp:HiddenField ID="scrollHiddenField" runat="server" />
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
                    <br />
                    <h3>
                        This is a screenshot of a sample map page:</h3>
                    <img alt="" src="getting-started.png" />
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    <h3>
                        Clicking on the marker brings up an info window with some details & links:</h3>
                    <img alt="" src="infoWindow.png" />
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    <h3>
                        Markers could have different statuses depending on a button pressed on the SPOT
                        device:</h3>
                    <table border="1" cellspacing="0">
                        <tr style="font-size: large; font-weight: bold">
                            <td>
                                SPOT button pressed
                            </td>
                            <td>
                                Marker on the page
                            </td>
                            <td>
                                Status shown in info window
                            </td>
                        </tr>
                        <tr>
                            <td>
                                'Track'
                            </td>
                            <td>
                                <img alt="" src='marker.png' />
                                (actual letter depends on a marker name)
                            </td>
                            <td>
                                <img alt="" src="../App_Themes/Default/info.png" />
                                ON TRACK
                            </td>
                        </tr>
                        <tr>
                            <td>
                                'OK'
                            </td>
                            <td>
                                <img alt="" src="../App_Themes/Default/finish.png" />
                            </td>
                            <td>
                                <img alt="" src="../App_Themes/Default/finish.png" style="float: left" />
                                LANDED (Ok) - for hang- and para-gliding<br />
                                usually means 'NEED RETRIEVE'
                            </td>
                        </tr>
                        <tr>
                            <td>
                                'Custom'
                            </td>
                            <td>
                                <img alt="" src="../App_Themes/Default/finish-custom.png" />
                            </td>
                            <td>
                                <img alt="" src="../App_Themes/Default/finish-custom.png" style="float: left" />
                                LANDED (Custom) - for hang- and para-gliding<br />
                                usually means 'DO NOT NEED RETRIEVE'
                            </td>
                        </tr>
                        <tr>
                            <td>
                                'Help' * or 'SOS' *
                            </td>
                            <td>
                                <img alt="" src="../App_Themes/Default/redarrow.png" style="float: left" /><br />
                            </td>
                            <td>
                                <img alt="" src="../App_Themes/Default/help.png" style="float: left" />
                                <i>'SOS', 'Help' or whatever tracker<br />
                                    sends as&quot;not good&quot; message</i>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
        --<br />
        * <span class="style1">SOS and Help statuses are not tested well. For example, there
            is no certainty that SOS message could be received by this site at all. </span>
    </div>
    </form>
</body>
</html>
