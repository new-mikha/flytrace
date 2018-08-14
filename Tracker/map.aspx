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
<%@  AutoEventWireup="true" EnableViewState="true" Language="C#" CodeBehind="map.aspx.cs"
    Inherits="FlyTrace.MapForm" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>
        <%=GroupName %>
        - Multi Tracker View</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=0" />

    <!-- Google Analytics -->
    <script type="text/javascript">
        (function (i, s, o, g, r, a, m) {
            i['GoogleAnalyticsObject'] = r; i[r] = i[r] || function () {
                (i[r].q = i[r].q || []).push(arguments)
            }, i[r].l = 1 * new Date(); a = s.createElement(o),
            m = s.getElementsByTagName(o)[0]; a.async = 1; a.src = g; m.parentNode.insertBefore(a, m)
        })(window, document, 'script', '//www.google-analytics.com/analytics.js', 'ga');

        ga('create', 'UA-44990201-1', 'auto');
        ga('send', 'pageview');

        function logoClicked() {
            ga('send', 'event', 'AdvRiderMagLogo', 'click');
        }
    </script>
    <!-- End Google Analytics -->

    <script type="text/javascript">
        var _ie8_or_less = false;
    </script>
    <%--Comment below is not a real one but rather an "IE conditional comment", 
        DO NOT CHANGE OR REMOVE IT, THE CODE INSIDE ACTUALLY WORKS IN IE 8 OR LESS --%>
    <!--[if lte IE 8]>
    <script type="text/javascript">
        _ie8_or_less = true;
    </script>
    <![endif]-->
    <link href="App_Themes/Default/site2.css?v=2" type="text/css" rel="stylesheet" />
    <style type="text/css">
        #listPanel tr:nth-child(even) {
            background: #CCC;
        }

        #listPanel thead {
            font-weight: bold;
        }

        #listPanel tr:nth-child(odd) {
            background: #FFF;
        }

        .infoUsrMsg {
            width: 12em;
            margin-bottom: 0.5em;
        }
    </style>
    <% 
        {
            string logoPath = System.IO.Path.Combine( HttpRuntime.AppDomainAppPath, "add/logo1.png" );

            Response.Write(
                string.Format(
                    "\n\n<script type=\"text/javascript\">\nvar _logoSource='{0}';\n</script>\n",
                    ( OwnerId == AdvRiderUserId && System.IO.File.Exists( logoPath ) )
                    ? "add/logo1.png"
                    : "" ) );
        }

        {
            string smallLogoPath = System.IO.Path.Combine( HttpRuntime.AppDomainAppPath, "add/logo1-small.png" );

            Response.Write(
                string.Format(
                    "\n\n<script type=\"text/javascript\">\nvar _smallLogoSource='{0}';\n</script>\n",
                    ( OwnerId == AdvRiderUserId && System.IO.File.Exists( smallLogoPath ) )
                    ? "add/logo1-small.png"
                    : "" ) );
        }
                
    %>
    <% 
        bool shouldLog;
        if ( !bool.TryParse( this.Request.Params["log"], out shouldLog ) )
        {
            shouldLog = false;
        }

        Response.Write( "\n<script type=\"text/javascript\">" );
        Response.Write( string.Format( "var _groupId = {0}\n", GroupId ) );
        Response.Write( string.Format( "var _shouldLog = {0}", shouldLog ? "true" : "false" ) ); // without that it's written like "True"/"False" and probably could even be loc.problems.
        Response.Write( "</script>\n" );

        if ( MapCenterLat.HasValue && MapCenterLon.HasValue )
        {
            Response.Write( "\n<script type=\"text/javascript\">" );
            Response.Write( string.Format( "var _mapCenterLat = {0}\n", MapCenterLat.Value ) );
            Response.Write( string.Format( "var _mapCenterLon = {0}\n", MapCenterLon.Value ) );
            Response.Write( "</script>\n" );
        }
    %>
    <script type="text/javascript" src="//maps.googleapis.com/maps/api/js?key=AIzaSyBQjcGGONlL8Ppv0qFJTBiJeqZSMrwaH8g"></script>
    <script type="text/javascript">
        var _reloadLatId = '<%= reloadLat.ClientID %>';
        var _reloadLonId = '<%= reloadLon.ClientID %>';
        var _reloadZoomId = '<%= reloadZoom.ClientID %>';
    </script>
    <script type="text/javascript" src="Scripts/jquery-1.11.3.min.js"></script>
    <script type="text/javascript" src="Scripts/labelMapControl.js?ver=4">
    </script>
    <script type="text/javascript" src="Scripts/buttonMapControl.js?ver=3">
    </script>
    <script type="text/javascript" src="Scripts/date.format.js">
    </script>
    <script type="text/javascript" src="Scripts/main.js?ver=77">
    </script>
</head>
<body onload="initialize()">
    <form id="defaultForm" runat="server" style="height: 100%">
        <div>
            <asp:HiddenField ID="reloadLat" runat="server" />
            <asp:HiddenField ID="reloadLon" runat="server" />
            <asp:HiddenField ID="reloadZoom" runat="server" />
            <div id="header">
                <div id="headerContent">
                    <div id="sosList">
                    </div>
                    <div id="helpList">
                    </div>
                </div>
                <div id="horSplitter">
                    <table>
                        <tr>
                            <td id="statusLabel">Initializing...
                            </td>
                            <td id="errorLabel"></td>
                        </tr>
                    </table>
                </div>
            </div>
            <div id="content">
                <div id="mapPanel" style="height: 100%; width: 100%">
                </div>
                <div id="listPanel">
                    <div style="padding-left: 0px">
                        <a href="default.aspx">FlyTrace</a>&nbsp;&nbsp;
                    <input type="button" value="Update Now" onclick="onLookup();" />
                        <input type="button" value="Show Map" onclick="togglePanels();" />
                    </div>
                    <p>
                        Coordinates format:
                    <select id="coordFormatSelect" onchange="javascript:changeCoordsFormat(this.value)"
                        id="coordFormatTopDropDownList">
                        <option value="deg">DD.DDDDDD&#176;</option>
                        <option selected="selected" value="degmin">DD&#176;MM.MMM'</option>
                        <option value="degminsec">DD&#176;MM'SS.S&quot;</option>
                    </select>
                        <br />
                        The same map with the format above pre-selected: <a id='preFormatLink' href=''>link</a>
                    </p>
                    <table id='listTable' border="0">
                        <thead>
                            <tr>
                                <td>Tracker
                                </td>
                                <td>Status
                                </td>
                                <td>Coordinates
                                </td>
                                <td>Time
                                </td>
                                <td>Status Age
                                </td>
                            </tr>
                        </thead>
                        <tbody>
                            <tr style="display: none">
                                <td>
                                    <div class="TrackerNameCtl">
                                    </div>
                                </td>
                                <td>
                                    <div class="TrackerStatusCtl">
                                    </div>
                                </td>
                                <td>
                                    <div class="TrackerCoordsCtl">
                                    </div>
                                </td>
                                <td>
                                    <div class="TrackerTimestampCtl">
                                    </div>
                                </td>
                                <td>
                                    <div class="TrackerAgeCtl">
                                    </div>
                                </td>
                                <td>
                                    <div class="TrackerErrorCtl">
                                    </div>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                    <div style="display: none;" id="hiddenHint">
                        * Tracker is hidden until it sends a new point.
                    </div>
                    <input type="button" value='Show All Tracks' onclick='showAllTracks()' />
                    <input type="button" value='Hide All Tracks' onclick='hideAllTracks()' />
                    <div style="display: none;" id="logDiv">
                        <br />
                        <input type="button" value='Send log to site admin' onclick='sendLog()' />
                    </div>
                </div>
            </div>
        </div>
        <asp:ScriptManager ID="scriptManager1" runat="server">
            <Services>
                <asp:ServiceReference Path="~/Service/TrackerService.asmx" />
            </Services>
        </asp:ScriptManager>
    </form>
</body>
</html>
