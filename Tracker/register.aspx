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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="register.aspx.cs" Inherits="FlyTrace.register"
    StylesheetTheme="Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="Register" runat="server">
    <div align="left">
        <div style="width: 100%" class="UserInfo">
            <a href="default.aspx">FlyTrace</a>
        </div>
        <br />
        <asp:CreateUserWizard ID="createUserWizard" runat="server" OnContinueButtonClick="createUserWizard_ContinueButtonClick"
            TitleTextStyle-CssClass="VisualGroupTitle" CssClass="GroupTable" 
            OnSendingMail="createUserWizard_SendingMail" 
            oncreateduser="createUserWizard_CreatedUser">
            <MailDefinition BodyFileName="~/EmailTemplates/accountNeedVerification.txt" From="admin@flytrace.com"
                Subject="Account activation">
            </MailDefinition>
            <TitleTextStyle CssClass="VisualGroupTitle" />
            <WizardSteps>
                <asp:CreateUserWizardStep ID="CreateUserWizardStep1" runat="server">
                </asp:CreateUserWizardStep>
                <asp:CompleteWizardStep ID="CompleteWizardStep1" runat="server">
                    <ContentTemplate>
                        <table>
                            <tr>
                                <td align="left">
                                    <b>Complete</b>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:MultiView runat="server" ID="completeMessageMultiView" ActiveViewIndex="<%$ FtCode: this.createUserWizard.DisableCreatedUser ? 1 : 0 %>">
                                        <asp:View runat="server">
                                            Your account has been successfully created.
                                        </asp:View>
                                        <asp:View runat="server">
                                            Your new FlyTrace account is almost ready. We've just sent you an email<br />
                                            to <b>
                                                <%=this.createUserWizard.Email%></b> to verify it. Please check your email and
                                            <br />
                                            click the activation link to complete your account setup.
                                        </asp:View>
                                    </asp:MultiView>
                                </td>
                            </tr>
                            <tr>
                                <td align="right">
                                    <asp:Button ID="ContinueButton" runat="server" CausesValidation="False" CommandName="Continue"
                                        Text="Continue" ValidationGroup="createUserWizard" />
                                </td>
                            </tr>
                        </table>
                    </ContentTemplate>
                </asp:CompleteWizardStep>
            </WizardSteps>
        </asp:CreateUserWizard>
    </div>
    </form>
</body>
</html>
