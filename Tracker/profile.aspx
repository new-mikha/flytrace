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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="profile.aspx.cs" Inherits="FlyTrace.profile" StylesheetTheme="Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<script type="text/javascript" src="Scripts/jquery-1.11.3.min.js"></script>
<script type="text/javascript" src="Scripts/maintainScrollPosition.js"></script>
<script type="text/javascript">
    setScrollHiddenInputId('scrollHiddenField');
</script>
<head runat="server">
    <title>User Settings - FlyTrace</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:HiddenField ID="scrollHiddenField" runat="server" />
        <table style="text-align: left">
            <tr>
                <td>
                    <table style="width: 100%;" class="UserInfo">
                        <tr>
                            <td>
                                <a href="default.aspx">FlyTrace</a>&nbsp;&gt;&nbsp;User Profile
                            </td>
                            <td style="text-align: right;">
                                <b>
                                    <asp:LoginName ID="LoginName1" runat="Server"></asp:LoginName>
                                </b>&nbsp;-&nbsp;<a href="profile.aspx">Settings</a>&nbsp;-&nbsp;<asp:LinkButton ID="SignOutLinkButton" runat="server" OnClick="SignOutLinkButton_Click">Sign out</asp:LinkButton>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    <asp:ChangePassword Width="100%" ID="changePasswordControl" runat="server" CssClass="GroupTable" ContinueDestinationPageUrl="~/default.aspx">
                        <ChangePasswordTemplate>
                            <table cellpadding="1" cellspacing="0" style="width: 100%; border-collapse: collapse;">
                                <tr>
                                    <td class="VisualGroupTitle" colspan="2">
                                        Password
                                    </td>
                                </tr>
                                <tr>
                                    <td align="right">
                                        <asp:Label ID="CurrentPasswordLabel" runat="server" AssociatedControlID="CurrentPassword">Password:</asp:Label>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="CurrentPassword" runat="server" TextMode="Password"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="CurrentPasswordRequired" runat="server" ControlToValidate="CurrentPassword" ErrorMessage="Password is required." ToolTip="Password is required." ValidationGroup="changePasswordControl">*</asp:RequiredFieldValidator>
                                    </td>
                                </tr>
                                <tr>
                                    <td align="right">
                                        <asp:Label ID="NewPasswordLabel" runat="server" AssociatedControlID="NewPassword">New Password:</asp:Label>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="NewPassword" runat="server" TextMode="Password"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="NewPasswordRequired" runat="server" ControlToValidate="NewPassword" ErrorMessage="New Password is required." ToolTip="New Password is required." ValidationGroup="changePasswordControl">*</asp:RequiredFieldValidator>
                                    </td>
                                </tr>
                                <tr>
                                    <td align="right">
                                        <asp:Label ID="ConfirmNewPasswordLabel" runat="server" AssociatedControlID="ConfirmNewPassword">Confirm New Password:</asp:Label>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="ConfirmNewPassword" runat="server" TextMode="Password"></asp:TextBox>
                                        <asp:RequiredFieldValidator ID="ConfirmNewPasswordRequired" runat="server" ControlToValidate="ConfirmNewPassword" ErrorMessage="Confirm New Password is required." ToolTip="Confirm New Password is required." ValidationGroup="changePasswordControl">*</asp:RequiredFieldValidator>
                                    </td>
                                </tr>
                                <tr>
                                    <td align="center" colspan="2">
                                        <asp:CompareValidator ID="NewPasswordCompare" runat="server" ControlToCompare="NewPassword" ControlToValidate="ConfirmNewPassword" Display="Dynamic" ErrorMessage="The Confirm New Password must match the New Password entry." ValidationGroup="changePasswordControl"></asp:CompareValidator>
                                    </td>
                                </tr>
                                <tr>
                                    <td align="center" colspan="2" style="color: Red;">
                                        <asp:Literal ID="FailureText" runat="server" EnableViewState="False"></asp:Literal>
                                    </td>
                                </tr>
                                <tr>
                                    <td align="right" colspan="2">
                                        <asp:Button ID="ChangePasswordPushButton" runat="server" CommandName="ChangePassword" Text="Change Password" ValidationGroup="changePasswordControl" />
                                    </td>
                                </tr>
                            </table>
                        </ChangePasswordTemplate>
                        <SuccessTemplate>
                            <table cellpadding="1" cellspacing="0" style="width: 100%; border-collapse: collapse;">
                                <tr>
                                    <td class="VisualGroupTitle">
                                        Password for <i>
                                            <asp:LoginName runat="Server" />
                                        </i>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        Your password has been changed!
                                    </td>
                                </tr>
                                <tr>
                                    <td align="right">
                                        <asp:Button ID="GoBackButton" runat="server" CausesValidation="False" Text="Change Again" OnClick="GoBackButton_Click" />
                                        &nbsp;&nbsp;
                                        <asp:Button ID="ContinuePushButton" runat="server" CausesValidation="False" CommandName="Continue" Text="Continue" />
                                    </td>
                                </tr>
                            </table>
                        </SuccessTemplate>
                    </asp:ChangePassword>
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    <table width="100%" class="GroupTable">
                        <tr>
                            <td class="VisualGroupTitle">
                                Email
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Wizard Width="100%" ID="emailWizard" runat="server" DisplaySideBar="False" OnFinishButtonClick="anyWizard_FinishButtonClick" OnNextButtonClick="emailWizard_NextButtonClick" ActiveStepIndex="0">
                                    <FinishNavigationTemplate>
                                        <asp:Button ID="FinishPreviousButton" runat="server" CausesValidation="False" CommandName="MovePrevious" Text="Change again" />
                                        <asp:Button ID="FinishButton" runat="server" CommandName="MoveComplete" Text="Continue" />
                                    </FinishNavigationTemplate>
                                    <StartNavigationTemplate>
                                        <asp:Button ID="StartNextButton" runat="server" CommandName="MoveNext" Text="Change e-mail" CausesValidation="true" ValidationGroup="changeEmail" />
                                    </StartNavigationTemplate>
                                    <WizardSteps>
                                        <asp:WizardStep runat="server">
                                            <table style="width: 100%">
                                                <tr>
                                                    <td colspan="2">
                                                        If you wish to change your email, please
                                                        <br />
                                                        provide your current password too:
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td align="right" style="width: 25%">
                                                        <asp:Label runat="server" AssociatedControlID="PasswordForEmail">Password:</asp:Label>
                                                    </td>
                                                    <td>
                                                        <asp:TextBox Width="90%" ID="PasswordForEmail" runat="server" TextMode="Password"></asp:TextBox>
                                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="PasswordForEmail" ErrorMessage="Password is required." ToolTip="Password is required." ValidationGroup="changeEmail">*</asp:RequiredFieldValidator>
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td align="right">
                                                        <asp:Label ID="Label1" runat="server" AssociatedControlID="Email">E-mail:</asp:Label>
                                                    </td>
                                                    <td>
                                                        <asp:TextBox Width="90%" ID="Email" runat="server"></asp:TextBox>
                                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="Email" ErrorMessage="Email is required." ToolTip="Email is required." ValidationGroup="changeEmail">*</asp:RequiredFieldValidator>
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td colspan="2" align="right">
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td align="center" colspan="2" style="color: Red;">
                                                        <asp:Literal ID="EmailChangeFailureText" runat="server" EnableViewState="False"></asp:Literal>
                                                    </td>
                                                </tr>
                                            </table>
                                        </asp:WizardStep>
                                        <asp:WizardStep runat="server">
                                            Your email has been changed to<br />
                                            <b>
                                                <% Response.Write( Membership.GetUser( ).Email );%></b>
                                        </asp:WizardStep>
                                    </WizardSteps>
                                </asp:Wizard>
                                <br />
                                To check that you could receive our emails:<br />
                                <asp:Button ID="sendTestEmailButton" runat="server" Text="Send a test one to your address" OnClick="sendTestEmailButton_Click" />
                                <asp:Literal ID="testEmailSendResult" runat="server" EnableViewState="False"></asp:Literal>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    <table width="100%" class="GroupTable">
                        <tr>
                            <td class="VisualGroupTitle">
                                Events Mode
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <p>
                                    Event is e.g. a competition or fly-in. A Pilot Group could<br />
                                    have an event assigned, in this case the group's map will<br />
                                    display the Today Task for the event.
                                </p>
                                <asp:Wizard ID="settingWizard" runat="server" DisplaySideBar="False" StartNextButtonText="Change Events Mode" FinishCompleteButtonText="Continue" OnFinishButtonClick="anyWizard_FinishButtonClick" Width="100%" FinishPreviousButtonText="Change Events Mode Again" OnNextButtonClick="settingWizard_NextButtonClick">
                                    <StartNavigationTemplate>
                                        <asp:Button ID="StartNextButton" runat="server" CommandName="MoveNext" Text="Change Events Mode" />
                                    </StartNavigationTemplate>
                                    <WizardSteps>
                                        <asp:WizardStep runat="server">
                                            <asp:RadioButton GroupName="eventsMode" Checked="true" runat="server" ID="singleEventMode" Text="Single Task Mode" />
                                            <br />
                                            <span style="font-size: small">&nbsp;&nbsp;&nbsp;&nbsp;In <b>Single Task Mode</b> there is only one default event,<br />
                                                &nbsp;&nbsp;&nbsp;&nbsp;i.e. in this mode you have only one Today Task for<br />
                                                &nbsp;&nbsp;&nbsp;&nbsp;all your groups.<br />
                                                &nbsp;&nbsp;&nbsp;&nbsp;<span style="color: Red">Simple mode is OK for most cases.</span> </span>
                                            <br />
                                            <br />
                                            <asp:RadioButton GroupName="eventsMode" runat="server" ID="multipleEventsMode" Text="Multiple Events Mode" />
                                            <br />
                                            <span style="font-size: small">&nbsp;&nbsp;&nbsp;&nbsp;In <b>Multiple Events Mode</b> you can have<br />
                                                &nbsp;&nbsp;&nbsp;&nbsp;many events. E.g. you could use that if<br />
                                                &nbsp;&nbsp;&nbsp;&nbsp;you have a joint competition with more<br />
                                                &nbsp;&nbsp;&nbsp;&nbsp;than one task running together.<br />
                                            </span>
                                            <br />
                                        </asp:WizardStep>
                                        <asp:WizardStep runat="server" Title="Step 2">
                                            Your settings have been saved.
                                        </asp:WizardStep>
                                    </WizardSteps>
                                </asp:Wizard>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    <table width="100%" class="GroupTable">
                        <tr>
                            <td class="VisualGroupTitle">
                                Showing owner-defined messages by default
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <p>
                                    This setting controls if a new group Shows or Hides<br />
                                    owner-defined messages by default. <a href="help/usermessageshelp.aspx">
                                        <asp:Image ID="Image5" runat="server" ImageUrl="~/App_Themes/Default/siteHelp.png" /></a>
                                </p>
                                <asp:Wizard ID="ownerMessagesDefaultModeWizard" runat="server" DisplaySideBar="False" StartNextButtonText="Change default behaviour" FinishCompleteButtonText="Continue" OnFinishButtonClick="anyWizard_FinishButtonClick" Width="100%" FinishPreviousButtonText="Change default behaviour again" OnNextButtonClick="ownerMessagesDefaultModeWizard_NextButtonClick">
                                    <StartNavigationTemplate>
                                        <asp:Button ID="StartNextButton" runat="server" CommandName="MoveNext" Text="Change Default Behaviour" />
                                    </StartNavigationTemplate>
                                    <WizardSteps>
                                        <asp:WizardStep ID="WizardStep1" runat="server">
                                            <asp:RadioButton GroupName="ownerMessagesMode" Checked="true" runat="server" ID="showOwnerMessagesRadioButton" Text="Show owner-defined messages by default" />
                                            <br />
                                            <span style="font-size: small">&nbsp;&nbsp;&nbsp;&nbsp;In <b>Show messages by default</b> mode a newly created group will<br />
                                                have "Owner-defined messages" option set to "SHOW" by default.</span>
                                            <br />
                                            <br />
                                            <asp:RadioButton GroupName="ownerMessagesMode" Checked="true" runat="server" ID="hideOwnerMessagesRadioButton" Text="Hide owner-defined messages by default" />
                                            <br />
                                            <span style="font-size: small">&nbsp;&nbsp;&nbsp;&nbsp;In <b>Hide messages by default</b> mode a newly created group will<br />
                                                have "Owner-defined messages" option set to "HIDE" by default.</span>
                                            <br />
                                        </asp:WizardStep>
                                        <asp:WizardStep ID="WizardStep2" runat="server" Title="Step 2">
                                            Your settings have been saved.
                                        </asp:WizardStep>
                                    </WizardSteps>
                                </asp:Wizard>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
