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

<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LogConfig.ascx.cs" Inherits="FlyTrace.Service.Administration.LogConfig" %>
<table>
    <tr>
        <td style="text-align: left">
            Is log4net configured:
        </td>
        <td>
            <asp:Label ID="isConfiguredLabel" runat="server" Font-Bold="True" ForeColor="Green"
                Text="Yes" />
            <asp:Label ID="isNotConfiguredLabel" runat="server" Font-Bold="True" ForeColor="Red"
                Text="No" />
        </td>
    </tr>
    <tr>
        <td style="text-align: left">
            Config File Name:
        </td>
        <td>
            <asp:Label ID="configFileNameLabel" runat="server" Font-Bold="True" />
        </td>
    </tr>
    <tr>
        <td style="text-align: left">
            Should Watch Config File:
        </td>
        <td>
            <asp:Label ID="shouldWatchLabel" runat="server" Font-Bold="True" />
        </td>
    </tr>
</table>
<p>
    <asp:Label ID="infoLabel" runat="server"></asp:Label>
</p>
<p>
    <asp:MultiView ID="logConfigMultiView" runat="server">
        <asp:View ID="logConfigNormalView" runat="server">
            <asp:TextBox ID="logConfigTextBox" runat="server" TextMode="MultiLine" Width="99%"
                Wrap="False" Height="282px"></asp:TextBox>
            <p>
                <asp:Button ID="updateConfigButton" runat="server" OnClick="updateConfigButton_Click"
                    Text="Update Config" />
                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                <asp:Button ID="restoreDefaultConfigButton" runat="server" 
                    OnClick="updateConfigButton_Click" Text="Restore Default Config" />
            </p>
        </asp:View>
        <asp:View ID="logConfigErrorView" runat="server">
            <asp:Label ID="logConfigErrorLabel" runat="server" ForeColor="Red" />
            <br />
            <asp:Button ID="rereadExistingConfigButton" runat="server" OnClick="updateConfigButton_Click"
                Text="Re-read existing config" />
        </asp:View>
    </asp:MultiView>
</p>
