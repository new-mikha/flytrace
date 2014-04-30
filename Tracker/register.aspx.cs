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
using System.Web;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;
using FlyTrace.Properties;

namespace FlyTrace
{
  public partial class register : System.Web.UI.Page
  {
    protected void Page_PreInit( object sender, EventArgs e )
    {
      this.createUserWizard.DisableCreatedUser = Settings.Default.DisableCreatedUsers;
      this.createUserWizard.MailDefinition.From = Settings.Default.AdminEmail;
    }

    protected void Page_Load( object sender, EventArgs e )
    {
    }

    protected void createUserWizard_ContinueButtonClick( object sender, EventArgs e )
    {
      if ( createUserWizard.ActiveStep == createUserWizard.CompleteStep )
      {
        Response.Redirect( "default.aspx" );
      }
    }

    protected void createUserWizard_SendingMail( object sender, MailMessageEventArgs e )
    {
      e.Cancel = !this.createUserWizard.DisableCreatedUser;
      if ( this.createUserWizard.DisableCreatedUser )
      {
        MembershipUser newUser = Membership.GetUser( this.createUserWizard.UserName );
        Guid newUserId = ( Guid ) newUser.ProviderUserKey;
        string urlBase = Request.Url.GetLeftPart( UriPartial.Authority ) + Request.ApplicationPath;
        string verificationUrl = urlBase + "/verification.aspx?ID=" + newUserId.ToString( );

        e.Message.Body =
          e.Message.Body
          .Replace( "<%VerificationUrl%>", verificationUrl )
          .Replace( "<%AdminEmail%>", Settings.Default.AdminEmail );
      }
    }

    protected void createUserWizard_CreatedUser( object sender, EventArgs e )
    {
      // If it's the first user in the database, ensure that there are predefined roles
      // and add user to Admins role:

      int totalUsersCount;
      MembershipUserCollection users = Membership.GetAllUsers( 0, 1, out totalUsersCount );

      if ( totalUsersCount == 1 &&
           users.Cast<MembershipUser>( ).First( ).UserName.ToLower( ) ==
            this.createUserWizard.UserName.ToLower( ) )
      {
        string[] roles = Roles.GetAllRoles( );

        if ( !roles.Contains( Global.AdminRole, StringComparer.InvariantCultureIgnoreCase ) )
        {
          Roles.CreateRole( Global.AdminRole );
        }
        Roles.AddUserToRole( this.createUserWizard.UserName, Global.AdminRole );

        if ( !roles.Contains( Global.SpotIdReaderRole, StringComparer.InvariantCultureIgnoreCase ) )
        {
          Roles.CreateRole( Global.SpotIdReaderRole );
        }
      }
    }
  }
}