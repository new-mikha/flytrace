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
    <meta name="viewport" content="width=device-width; initial-scale=1.0; maximum-scale=1.0; user-scalable=0" />
    <script type="text/javascript">
        var _gaq = _gaq || [];
        _gaq.push(['_setAccount', 'UA-44990201-1']);
        _gaq.push(['_setDomainName', 'flytrace.com']);
        _gaq.push(['_setAllowLinker', true]);
        _gaq.push(['_trackPageview']);

        (function () {
            var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
            ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
            var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
        })();
    </script>
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
    <link href="App_Themes/Default/site2.css" type="text/css" rel="stylesheet" />
    <style type="text/css">
        #listPanel tr:nth-child(even)
        {
            background: #CCC;
        }
        #listPanel tr:nth-child(odd)
        {
            background: #FFF;
        }
        .infoUsrMsg
        {
            width: 12em;
            margin-bottom: 0.5em;
        }
    </style>
    <% 
        bool showLogo = false;

        if ( OwnerId == AdvRiderUserId )
        {
            bool.TryParse( this.Request.Params["logo_test"], out showLogo );
        }

        Response.Write(
            string.Format(
                "\n\n<script type=\"text/javascript\">\nvar _logoSource='{0}';\n</script>\n",
                showLogo
                ? "add/logo1.png"
                : "" ) );

    %>
    <% 
        bool sensor;
        if ( !bool.TryParse( this.Request.Params["sensor"], out sensor ) )
        {
            sensor = true;
        }

        bool shouldLog;
        if ( !bool.TryParse( this.Request.Params["log"], out shouldLog ) )
        {
            shouldLog = false;
        }

        Response.Write(
            string.Format(
                "<script type=\"text/javascript\" src=\"http://maps.googleapis.com/maps/api/js?key=AIzaSyBQjcGGONlL8Ppv0qFJTBiJeqZSMrwaH8g&sensor={0}\">\n</script>",
                sensor ? "true" : "false" ) );

        Response.Write(
            string.Format(
                "\n\n<script type=\"text/javascript\">\nvar _useLocationSensor={0};\n</script>\n",
                sensor ? "true" : "false" ) );

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
    <script type="text/javascript">
        var _reloadLatId = '<%= reloadLat.ClientID %>';
        var _reloadLonId = '<%= reloadLon.ClientID %>';
        var _reloadZoomId = '<%= reloadZoom.ClientID %>';
    </script>
    <script type="text/javascript" src="Scripts/jquery-1.7.1.min.js"></script>
    <script type="text/javascript" src="Scripts/labelMapControl.js?ver=3">
    </script>
    <script type="text/javascript" src="Scripts/buttonMapControl.js?ver=3">
    </script>
    <script type="text/javascript" src="Scripts/date.format.js">
    </script>
    <script type="text/javascript" src="Scripts/main.js?ver=58">
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
                        <td id="statusLabel">
                            Initializing...
                        </td>
                        <td id="errorLabel">
                        </td>
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
                <br />
                Coordinates format:
                <select id="coordFormatSelect" onchange="javascript:changeCoordsFormat(this.value)"
                    id="coordFormatTopDropDownList">
                    <option value="Deg">DD.DDDDDD&#176;</option>
                    <option selected="selected" value="DegMin">DD&#176;MM.MMM'</option>
                    <option value="DegMinSec">DD&#176;MM'SS.S&quot;</option>
                </select>
                <table id='listTable' border="0">
                    <tr>
                        <td>
                            Tracker
                        </td>
                        <td>
                            Status
                        </td>
                        <td>
                            Coordinates
                        </td>
                        <td>
                            Time
                        </td>
                        <td>
                            Status Age
                        </td>
                    </tr>
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
                </table>
                <div style="display: none;" id="hiddenHint">
                    * Tracker is hidden from the map until it sends a new point.
                </div>
                <input type="button" value='Show All Tracks' onclick='showAllTracks()' />
                <input type="button" value='Hide All Tracks' onclick='hideAllTracks()' />
                <div id='logDiv'>
                </div>
            </div>
        </div>
    </div>
    <asp:ScriptManager ID="scriptManager1" runat="server">
        <Services>
            <asp:ServiceReference Path="/Tracker/Service/TrackerService.asmx" />
        </Services>
    </asp:ScriptManager>
    </form>
</body>
</html>
