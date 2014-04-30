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

namespace FlyTrace
{
  public partial class userInformation : System.Web.UI.Page
  {
    MembershipUser user;

    protected void Page_Load( object sender, EventArgs e )
    {
      this.user = Membership.GetUser( Request["user"] );
      if ( this.user == null )
      {
        this.mainMultiView.SetActiveView( this.wrongUserView );
      }
      else
      {
        this.mainMultiView.SetActiveView( this.normalView );
      }
    }

    protected void Page_PreRender( object sender, EventArgs e )
    {
      if ( this.user.UserName != null )
      {
        if ( Roles.IsUserInRole( this.user.UserName, Global.AdminRole ) )
        {
          this.roleLiteral.Text = "User is Administrator";
          this.makeAdminButton.Visible = false;
        }
        else
        {
          roleLiteral.Text = "User is NOT Administrator";
          this.makeNotAdminButton.Visible = false;
        }

        this.unlockButton.Visible = this.user.IsLockedOut;
      }
    }

    protected void adminButton_Click( object sender, EventArgs e )
    {
      if ( sender == this.makeAdminButton )
      {
        Roles.AddUserToRole( this.user.UserName, Global.AdminRole );
      }
      else if ( sender == this.makeNotAdminButton )
      {
        if ( this.user.UserName.ToLower( ) == User.Identity.Name.ToLower( ) )
        {
          this.errorLiteral.Text = "You can't revoke Admin rights from your own account";
        }
        else
        {
          Roles.RemoveUserFromRole( this.user.UserName, Global.AdminRole );
        }
      }
    }

    protected void SignOutLinkButton_Click( object sender, EventArgs e )
    {
      Response.Clear( );
      FormsAuthentication.SignOut( );
      Response.Redirect( "../default.aspx", true );
    }

    protected void unlockButton_Click( object sender, EventArgs e )
    {
      this.user.UnlockUser( );
    }
  }
}