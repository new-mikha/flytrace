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
  public partial class verification : System.Web.UI.Page
  {
    protected void Page_Load( object sender, EventArgs e )
    {
      if ( string.IsNullOrEmpty( Request.QueryString["ID"] ) )
        this.infoLabel.Text = "The UserId was not included in the querystring...";
      else
      {
        Guid userId;
        try
        {
          userId = new Guid( Request.QueryString["ID"] );
        }

        catch
        {
          this.infoLabel.Text = "The UserId passed into the querystring is not in the proper format...";
          return;
        }

        MembershipUser usr = Membership.GetUser( userId );
        if ( usr == null )
          this.infoLabel.Text = "User account could not be found...";
        else
        {
          // Approve the user
          usr.IsApproved = true;

          Membership.UpdateUser( usr );
          this.infoLabel.Text = "Your account has been approved. Please <a href=\"login.aspx\">login</a> to the site.";
        }
      }
    }
  }
}