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
using System.Net.Mail;
using System.IO;
using FlyTrace.Properties;
using log4net;

namespace FlyTrace
{
  public partial class profile : System.Web.UI.Page
  {
    private ILog log = LogManager.GetLogger( "profile" );

    protected void Page_Load( object sender, EventArgs e )
    {
      if ( !IsPostBack )
      {
        this.singleEventMode.Checked = Global.IsSimpleEventsModel;
        this.multipleEventsMode.Checked = !this.singleEventMode.Checked;

        this.showOwnerMessagesRadioButton.Checked = Global.ShowUserMessagesByDefault;
        this.hideOwnerMessagesRadioButton.Checked = !this.showOwnerMessagesRadioButton.Checked;

        this.Email.Text = Membership.GetUser().Email;
      }
    }

    protected void SignOutLinkButton_Click( object sender, EventArgs e )
    {
      Response.Clear();
      FormsAuthentication.SignOut();
      Response.Redirect( "~/default.aspx", true );
    }

    protected void changePasswordControl_ContinueButtonClick( object sender, EventArgs e )
    {
      Response.Redirect( "~/default.aspx", true );
    }

    protected void GoBackButton_Click( object sender, EventArgs e )
    {
      Response.Redirect( Request.Path, true );
    }

    protected void anyWizard_FinishButtonClick( object sender, WizardNavigationEventArgs e )
    {
      Response.Redirect( "~/default.aspx", true );
    }

    protected void settingWizard_NextButtonClick( object sender, WizardNavigationEventArgs e )
    {
      Global.IsSimpleEventsModel = this.singleEventMode.Checked;
    }

    protected void ownerMessagesDefaultModeWizard_NextButtonClick( object sender, WizardNavigationEventArgs e )
    {
      Global.ShowUserMessagesByDefault = this.showOwnerMessagesRadioButton.Checked;
    }

    public static bool IsValidEmail( string strIn )
    {
      // Return true if strIn is in valid e-mail format.
      return Regex.IsMatch( strIn, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$" );
    }

    protected void emailWizard_NextButtonClick( object sender, WizardNavigationEventArgs e )
    {
      string userName = null;
      string newEmail = null;
      try
      {
        newEmail = Email.Text;
        if ( !IsValidEmail( Email.Text ) )
        {
          throw new ApplicationException( "Please provide a valid email." );
        }

        MembershipUser user = Membership.GetUser();
        userName = user.UserName;
        if ( !Membership.ValidateUser( userName, PasswordForEmail.Text ) )
        {
          throw new ApplicationException( "Password is incorrect." );
        }

        user.Email = Email.Text;
        Membership.UpdateUser( user );
      }
      catch ( ApplicationException exc )
      {
        EmailChangeFailureText.Text = exc.Message;
      }
      catch ( Exception exc )
      {
        log.FatalFormat( "Can't change email from to {0} for {1}: {2}", userName, newEmail, exc );
        EmailChangeFailureText.Text = exc.Message;
      }

      e.Cancel = EmailChangeFailureText.Text != "";
    }

    protected void sendTestEmailButton_Click( object sender, EventArgs e )
    {
      string email = null;
      string userName = null;
      try
      {
        MembershipUser user = Membership.GetUser();
        email = user.Email;
        userName = user.UserName;

        string templatePath = Path.Combine( HttpRuntime.AppDomainAppPath, "EmailTemplates" );
        templatePath = Path.Combine( templatePath, "testEmail.txt" );
        string messageText =
          File.ReadAllText( templatePath )
          .Replace( "<%UserName%>", userName )
          .Replace( "<%AdminEmail%>", Settings.Default.AdminEmail );

        MailMessage mailMessage = new MailMessage( Settings.Default.AdminEmail, email );
        mailMessage.Subject = "FlyTrace test email";
        mailMessage.Body = messageText;
        mailMessage.IsBodyHtml = false;

        SmtpClient smtpClient = new SmtpClient();
        smtpClient.Send( mailMessage );

        this.testEmailSendResult.Text = string.Format( "<br />Email sent to <b>{0}</b>", email );
      }
      catch ( Exception exc )
      {
        log.FatalFormat( "Can't send test email to {0} {1}: {2}", userName, email, exc );
        this.testEmailSendResult.Text = string.Format( "<br /><span style='color:Red'>{0}</span>", exc.Message );
      }
    }
  }
}