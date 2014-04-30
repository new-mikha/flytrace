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

<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Deg.ascx.cs" Inherits="FlyTrace.CoordControls.Deg" %>
<table>
    <tr>
        <td>
            <asp:TextBox ID="degTextBox" Width="30px" runat="server" />
        </td>
        <td>
            .
        </td>
        <td>
            <asp:TextBox Width="60px" MaxLength="5" ID="degFractionsTextBox" runat="server" />
        </td>
        <td>
            &deg;
        </td>
    </tr>
</table>
<asp:RequiredFieldValidator ID="latDegRequiredFieldValidator" ControlToValidate="degTextBox"
    runat="server" ErrorMessage="<br />Latitude cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
<asp:RequiredFieldValidator ID="lonDegRequiredFieldValidator" ControlToValidate="degTextBox"
    runat="server" ErrorMessage="<br />Longitude cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
<asp:RangeValidator ID="latDegRangeValidator" runat="server" ControlToValidate="degTextBox"
    Display="Dynamic" Type="Integer" MinimumValue="-89" MaximumValue="89" ErrorMessage="<br />Degrees should be an integer from -89 to 89.<br />Use second box to enter fractions."></asp:RangeValidator>
<asp:RangeValidator ID="lonDegRangeValidator" Type="Integer" runat="server" ControlToValidate="degTextBox"
    Display="Dynamic" MinimumValue="-179" MaximumValue="179" ErrorMessage="<br />Degrees should be an integer from -179 to 179.<br />Use second box to enter fractions."></asp:RangeValidator>
<%-- Fraction validators (they are the same for both lat an lon)--%>
<asp:RequiredFieldValidator ControlToValidate="degFractionsTextBox" runat="server"
    ErrorMessage="<br />Fractions cannot be empty" Display="Dynamic"></asp:RequiredFieldValidator>
<asp:RangeValidator runat="server" ControlToValidate="degFractionsTextBox" Display="Dynamic"
    Type="Integer" MinimumValue="0" MaximumValue="999999" ErrorMessage="<br />Fractions should be a value from 0 to 999999.<br />Fraction box should NOT contain the decimal symbol."></asp:RangeValidator>
