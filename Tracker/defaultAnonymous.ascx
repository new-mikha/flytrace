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
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="defaultAnonymous.ascx.cs"
    Inherits="FlyTrace.defaultAnonymous" %>
<%--<div class="UserInfo2">
    <div style="margin-left: 0.5em;">
        <div style="font-size: larger; font-weight: bold;">
            With Flytrace, watch groups of <a target="_blank" href='http://www.findmespot.com/en/index.php?cid=102'>
                SPOT Satellite GPS Messengers</a> on a single map page.<br />
        </div>
        <div style="font-size: larger;">
            It's similar to the original SPOT Shared Pages, but with many trackers on a single,
            mobile-friendly map.
        </div>
    </div>
</div>
--%><div style="margin-left: 0.5em">
        <div style="font-size: larger; font-weight: bold;margin-top:1em">
            With Flytrace, watch groups of <a target="_blank" href='http://www.findmespot.com/en/index.php?cid=102'>
                SPOT</a> Satellite GPS Messengers on a single map page.<br />
        </div>
        <div style="margin-top:0.5em">
            It's similar to the original SPOT Shared Pages, but with many trackers on a single,
            mobile-friendly map.
        </div>
    <p style="margin-bottom:0px">
        Here you can (all is free)
    </p>
    <ul style="margin-top:0px">
        <li>Watch group pages created by others. You can do it without registration, e.g. see some of the others' groups in the list below.</li>
        <li>Create your own groups and add as many trackers as you like once you register
            & login - and then share your link anywhere you want.</li>
    </ul>
    <p>
        Map pages designed to work fast on mobile devices with poor internet connection.
        <br />
        <br />
        Originally it was made for flying sports like hang-, para- and just gliding (that's
        why you can see the word "<b><u><code>Pilot</code></u></b>" on almost every page<br />
        on this site) but it can be used with any other activity where several trackers
        need to be displayed on a single page.
    </p>
    <table style="border: 1px solid #B0B0B0;">
        <tr>
            <td>
                See a sample map screenshot with some explanatory comments<span id="collapsedHelpTitleEnding">...&nbsp;&nbsp;&nbsp;[&nbsp;<a
                    href='javascript:toggleHelp()'>Expand</a>&nbsp;]</span><span id="expandedHelpTitleEnding"
                        style='display: none'>:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[&nbsp;<a href='javascript:toggleHelp()'>Hide</a>&nbsp;]</span>
                <div id="helpPanel" style="display: none">
                    <p>
                        <img alt="" src="help/getting-started.png" />
                    </p>
                    <a href="help/commonhelp.aspx">Find out more...</a>
                </div>
            </td>
        </tr>
    </table>
    <p>
        <a href="login.aspx?ReturnUrl=<%= Server.UrlEncode( this.Request.RawUrl ) %>">Log in</a>
        if you have an account, or <a href="register.aspx?ReturnUrl=<%= Server.UrlEncode( this.Request.RawUrl ) %>">
            register</a> if you don&#39;t have one.</p>
</div>
