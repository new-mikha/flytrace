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

<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="DegMin.ascx.cs" Inherits="FlyTrace.CoordControls.DegMin"
    EnableViewState="false" %>
<table>
    <tr>
        <td>
            <asp:DropDownList ID="nsDropDownList" runat="server">
                <asp:ListItem Value="N">N</asp:ListItem>
                <asp:ListItem Value="S">S</asp:ListItem>
            </asp:DropDownList>
            <asp:DropDownList ID="weDropDownList" runat="server">
                <asp:ListItem Value="W">W</asp:ListItem>
                <asp:ListItem Value="E">E</asp:ListItem>
            </asp:DropDownList>
        </td>
        <td>
            <asp:TextBox ID="degTextBox" runat="server" MaxLength="2" Width="30px" />
        </td>
        <td>
            &deg;
        </td>
        <td>
            <asp:TextBox ID="minTextBox" runat="server" MaxLength="2" Width="25px" />
        </td>
        <td>
            .
        </td>
        <td>
            <asp:TextBox ID="minFractionTextBox" runat="server" MaxLength="3" Width="35px" />
        </td>
        <td>
            &#39;
        </td>
    </tr>
</table>
<%-- Common degree validator--%>
<asp:RequiredFieldValidator ControlToValidate="degTextBox" runat="server" ErrorMessage="<br />Degrees cannot be empty"
    Display="Dynamic"></asp:RequiredFieldValidator>
<%--Latitude degree validators--%>
<asp:RangeValidator ID="latDegRangeValidator" ControlToValidate="degTextBox" Display="Dynamic"
    Type="Integer" runat="server" MinimumValue="0" MaximumValue="89" ErrorMessage="<br />Latitude degrees should be an integer from 0 to 89"></asp:RangeValidator>
<%--Longitude degree validators--%>
<asp:RangeValidator ID="lonDegRangeValidator" ControlToValidate="degTextBox" Display="Dynamic"
    Type="Integer" runat="server" MinimumValue="0" MaximumValue="179" ErrorMessage="<br />Longitude degrees should be an integer from 0 to 179"></asp:RangeValidator>
<%--Other common validators - minute --%>
<asp:RequiredFieldValidator ControlToValidate="minTextBox" runat="server" ErrorMessage="<br />Minutes cannot be empty"
    Display="Dynamic"></asp:RequiredFieldValidator>
<asp:RangeValidator ControlToValidate="minTextBox" Display="Dynamic" runat="server"
    Type="Integer" MinimumValue="0" MaximumValue="59" ErrorMessage="<br />Minutes should be an integer value from 0 to 59"></asp:RangeValidator>
<asp:RangeValidator ControlToValidate="minFractionTextBox" Display="Dynamic" runat="server"
    Type="Integer" MinimumValue="0" MaximumValue="999" ErrorMessage="<br />Minutes fraction should be an integer value from 0 to 999"></asp:RangeValidator>