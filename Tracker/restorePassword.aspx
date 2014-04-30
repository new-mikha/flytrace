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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="restorePassword.aspx.cs"
    Inherits="FlyTrace.restorePassword" StylesheetTheme="Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Restoring account details - FlyTrace</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div style="width: 100%;" class="UserInfo">
            <a href="default.aspx">FlyTrace</a>&nbsp;&gt;&nbsp;Restoring account details
        </div>
        <table>
            <tr>
                <td>
                    <asp:Wizard Width="100%" ID="wizard" runat="server" DisplaySideBar="False" OnActiveStepChanged="wizard_ActiveStepChanged"
                        OnFinishButtonClick="wizard_FinishButtonClick">
                        <WizardSteps>
                            <asp:WizardStep ID="problemTypeWizardStep" runat="server" Title="Can't log in?">
                                <h3>
                                    Can't log in?</h3>
                                <asp:RadioButtonList ID="problemTypeRadioButtonList" runat="server">
                                    <asp:ListItem Selected="True" Value="password">I forgot my password</asp:ListItem>
                                    <asp:ListItem Value="username">I forgot my user name</asp:ListItem>
                                </asp:RadioButtonList>
                            </asp:WizardStep>
                            <asp:WizardStep AllowReturn="false" ID="problemDetailsWizardStep" runat="server"
                                Title="Your details">
                                <asp:MultiView ID="problemDetailsMultiView" runat="server">
                                    <asp:View ID="passwordRestoreView" runat="server">
                                        <h3>
                                            Restoring the password</h3>
                                        To restore your password, enter the<br />
                                        username you use to sign in to FlyTrace:<br />
                                        <asp:TextBox ID="userNameTextBox" runat="server" Style="width: 100%" placeholder="User name"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="userNameRequired" runat="server" ControlToValidate="userNameTextBox"
                                            ErrorMessage="User Name is required." ToolTip="User Name is required." ValidationGroup="PasswordRecovery"
                                            Display="Dynamic">*</asp:RequiredFieldValidator>
                                    </asp:View>
                                    <asp:View ID="usernameRestoreView" runat="server">
                                        <h3>
                                            Restoring the user name</h3>
                                        Please enter the email address you<br />
                                        provided when creating your account:<br />
                                        <asp:TextBox ID="emailTextBox" runat="server" Style="width: 100%" placeholder="email address"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="emailRequired" runat="server" ControlToValidate="emailTextBox"
                                            ErrorMessage="E-Mail is required." ToolTip="E-Mail is required." ValidationGroup="PasswordRecovery"
                                            Display="Dynamic">*</asp:RequiredFieldValidator>
                                    </asp:View>
                                </asp:MultiView>
                                <p style="color: Red">
                                    <asp:Literal ID="failureTextLiteral" runat="server" EnableViewState="False"></asp:Literal>
                                </p>
                            </asp:WizardStep>
                            <asp:WizardStep StepType="Complete" ID="completeWizardStep">
                                <h3>
                                    Success!</h3>
                                <asp:Literal ID="completeTextLiteral" runat="server" EnableViewState="False">Done.</asp:Literal>
                                <br />
                                <asp:Button runat="server" ID="goHomeButton" Text="Return to the FlyTrace home" OnClick="goHomeButton_Click" />
                            </asp:WizardStep>
                        </WizardSteps>
                    </asp:Wizard>
                    <p style="font-size: smaller">
                        &nbsp;&nbsp;&nbsp;&nbsp;In case of any other problem please<br />
                        &nbsp;&nbsp;&nbsp;&nbsp;contact a site administrator:<br />
                        &nbsp;&nbsp;&nbsp;&nbsp;<a href='mailto: <%= AdminEmail %> '><%= AdminEmail %></a>
                    </p>
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
