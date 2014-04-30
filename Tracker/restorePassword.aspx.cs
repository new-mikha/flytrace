/******************************************************************************
 * Flytrace, online viewer for GPS trackers.
 * Copyright (C) 2011-2014 Mikhail Karmazin
 * 
 * This file is part of Flytrace.
 * 
 * Flytrace is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * Flytrace is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;
using System.Text.RegularExpressions;
using log4net;
using FlyTrace.Properties;
using System.Net.Mail;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;

namespace FlyTrace
{
  public partial class restorePassword : System.Web.UI.Page
  {
    private ILog log = LogManager.GetLogger( "restorePassword" );

    protected readonly string AdminEmail = Settings.Default.AdminEmail;

    protected void Page_Load( object sender, EventArgs e )
    {
    }

    private string GetRestorePasswordMessage( MembershipUser user )
    {
      string password;
      string templateFileName;

      try
      {
        try
        {
          password = user.GetPassword();
          templateFileName = "restorePassword.txt";
        }
        catch ( Exception exc )
        {
          log.WarnFormat(
            "First chance exception when tried to get the password for {0} ({1}): {2}",
            user.UserName,
            user.Email,
            exc.Message
          );
          password = user.ResetPassword();
          templateFileName = "resetPassword.txt";
        }
      }
      catch ( Exception exc )
      {
        log.FatalFormat(
          "Can't {0} user password for {1} ({2}): {3}",
          Membership.EnablePasswordRetrieval ? "get" : "reset",
          user.UserName,
          user.Email,
          exc.Message
        );
        throw;
      }

      string templatePath = Path.Combine( HttpRuntime.AppDomainAppPath, "EmailTemplates" );
      templatePath = Path.Combine( templatePath, templateFileName );
      string messageText =
        File.ReadAllText( templatePath )
        .Replace( "<%UserName%>", user.UserName )
        .Replace( "<%Password%>", password )
        .Replace( "<%AdminEmail%>", Settings.Default.AdminEmail );

      return messageText;
    }

    protected void wizard_ActiveStepChanged( object sender, EventArgs e )
    {
      if ( this.wizard.ActiveStep == this.problemDetailsWizardStep )
      {
        switch ( this.problemTypeRadioButtonList.SelectedValue )
        {
          case "password":
            this.problemDetailsMultiView.SetActiveView( this.passwordRestoreView );
            break;

          case "username":
            this.problemDetailsMultiView.SetActiveView( this.usernameRestoreView );
            break;

          default:
            this.problemDetailsMultiView.SetActiveView( this.passwordRestoreView );
            this.log.FatalFormat( "Unknown value of problemTypeRadioButtonList: {0}", this.problemTypeRadioButtonList.SelectedValue );
            break;
        }
      }
    }

    protected void goHomeButton_Click( object sender, EventArgs e )
    {
      Response.Redirect( "default.aspx" );
    }

    protected void wizard_FinishButtonClick( object sender, WizardNavigationEventArgs e )
    {
      string detail = null;
      try
      {
        bool isOk;
        if ( this.problemDetailsMultiView.GetActiveView() == this.passwordRestoreView )
        {
          detail = this.userNameTextBox.Text;
          isOk = RestorePassword( detail );
        }
        else
        {
          detail = this.emailTextBox.Text;
          isOk = RestoreUserName( detail );
        }

        e.Cancel = !isOk;
      }
      catch ( Exception exc )
      {
        log.FatalFormat(
          "Result of {0}\\{1}: {2}",
          this.problemDetailsMultiView.GetActiveView().ID,
          detail,
          exc
        );
        this.failureTextLiteral.Text = exc.Message;
        //this.failureTextLiteral.Text = WindowsIdentity.GetCurrent(false).Name;
        e.Cancel = true;
      }
    }

    private bool RestoreUserName( string email )
    {
      if ( !profile.IsValidEmail( email ) )
      {
        this.failureTextLiteral.Text = "Please provide a valid email.";
        return false;
      }

      string username = Membership.GetUserNameByEmail( email );
      if ( username == null )
      {
        this.failureTextLiteral.Text =
          string.Format(
            "Can't find a matching user for the provided email <b>{0}</b>",
            email
          );
        return false;
      }

      string templatePath = Path.Combine( HttpRuntime.AppDomainAppPath, "EmailTemplates" );
      templatePath = Path.Combine( templatePath, "restoreUsername.txt" );
      string messageText =
        File.ReadAllText( templatePath )
        .Replace( "<%UserName%>", username )
        .Replace( "<%AdminEmail%>", Settings.Default.AdminEmail );

      MailMessage mailMessage = new MailMessage( Settings.Default.AdminEmail, email );
      mailMessage.Subject = "Your FlyTrace user name";
      mailMessage.Body = messageText;
      mailMessage.IsBodyHtml = false;

      SmtpClient smtpClient = new SmtpClient();
      smtpClient.Send( mailMessage );

      this.completeTextLiteral.Text =
        string.Format(
          "We found your user name and emailed it to <b>{0}</b>",
          email );

      return true;
    }

    private bool RestorePassword( string userName )
    {
      MembershipUser user = Membership.GetUser( userName, false );

      if ( user == null )
      {
        this.failureTextLiteral.Text =
          string.Format(
            "Can't find a user name '<b>{0}</b>'",
            userName
          );

        if ( userName.Contains( "@" ) )
        {
          this.failureTextLiteral.Text +=
            "<br />Note that to restore a password you need to provide your USER NAME, not EMAIL.";
        }

        return false;
      }

      MailMessage mailMessage = new MailMessage( Settings.Default.AdminEmail, user.Email );
      mailMessage.Subject = "Your FlyTrace password";
      mailMessage.Body = GetRestorePasswordMessage( user );
      mailMessage.IsBodyHtml = false;

      SmtpClient smtpClient = new SmtpClient();
      smtpClient.Send( mailMessage );

      this.completeTextLiteral.Text =
        "We restored your password and emailed it to the<br />address you provided when creating your account";

      return true;
    }
  }
}