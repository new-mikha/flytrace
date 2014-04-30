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

<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="defaultAnonymous.ascx.cs" Inherits="FlyTrace.defaultAnonymous" %>
<%--<div style="width: 100%" class="UserInfo">
    <asp:Login ID="Login1" runat="server">
        <LayoutTemplate>
            <table cellpadding="1" cellspacing="0" style="border-collapse: collapse; white-space: nowrap;">
                <tr>
                    <td>
                        <table cellpadding="0">
                            <tr>
                                <td>
                                    <asp:TextBox ID="UserName" runat="server" Font-Size="11px" placeholder='Username'></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="UserNameRequired" runat="server" ControlToValidate="UserName" ErrorMessage="User Name is required." ToolTip="User Name is required." ValidationGroup="ctl00$Login1">*</asp:RequiredFieldValidator>
                                </td>
                                <td>
                                    <asp:TextBox ID="Password" runat="server" Font-Size="11px" TextMode="Password" placeholder='Password'></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="Password" ErrorMessage="Password is required." ToolTip="Password is required." ValidationGroup="ctl00$Login1">*</asp:RequiredFieldValidator>
                                </td>
                                <td>
                                    <asp:CheckBox ID="RememberMe" runat="server" Text="Remember Me" />&nbsp;&nbsp;
                                </td>
                                <td>
                                    <asp:Button ID="LoginButton" runat="server" CommandName="Login" Text="Log In" ValidationGroup="ctl00$Login1" />&nbsp;&nbsp;
                                </td>
                                <td>
                                    <asp:HyperLink ID="CreateUserLink" runat="server" NavigateUrl="~/register.aspx" ForeColor="#FFFFFF">Create an account</asp:HyperLink>
                                    &nbsp;&nbsp;
                                </td>
                                <td>
                                    <asp:HyperLink ForeColor="#FFFFFF" ID="PasswordRecoveryLink" runat="server" NavigateUrl="~/restorePassword.aspx">Can't access your account?</asp:HyperLink>
                                    &nbsp;&nbsp;
                                </td>
                                <tr>
                                    <td colspan="6" style="color: #FFAAAA;">
                                        <asp:Literal ID="FailureText" runat="server" EnableViewState="False"></asp:Literal>
                                    </td>
                                </tr>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </LayoutTemplate>
    </asp:Login>
</div>--%>
<p>
    This website displays groups of <a target="_blank" href='http://www.findmespot.com/en/index.php?cid=102'>SPOT Satellite GPS Messengers</a> on a single map page.<br />
    Similar to the original SPOT Shared Pages, but with many trackers on a single, mobile-friendly map.<br />
    <br />
    You can:
</p>
<ul>
    <li>Watch group pages created by others (see the list below) - NO REGISTRATION REQUIRED FOR THAT AND IT'S FREE.</li>
    <li>Create your own pilots group and add any number of trackers on it once you register & login - IT'S FREE.</li>
</ul>
<p>
    The map pages designed to work fast on mobile devices with poor internet connection.
    <br />
    <br />
    Originally it was made for flying sports like hang-, para- and just gliding (that's why you can see the word "<b><u><code>Pilot</code></u></b>" on almost every page<br />
    on this site) but it can be used with any other activity where several trackers need to be displayed on a single page.
</p>
<table style="border: 1px solid #B0B0B0;">
    <tr>
        <td>
            See the sample map screenshot with some comments<span id="collapsedHelpTitleEnding">...&nbsp;&nbsp;&nbsp;[&nbsp;<a href='javascript:toggleHelp()'>Expand</a>&nbsp;]</span><span id="expandedHelpTitleEnding" style='display: none'>:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[&nbsp;<a href='javascript:toggleHelp()'>Hide</a>&nbsp;]</span>
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
    <a href="login.aspx?ReturnUrl=<%= Server.UrlEncode( this.Request.RawUrl ) %>">Log in</a> if you have an account, or <a href="register.aspx?ReturnUrl=<%= Server.UrlEncode( this.Request.RawUrl ) %>">register</a> if you don&#39;t have one.</p>
