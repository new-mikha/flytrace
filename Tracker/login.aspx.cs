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
using System.Web.UI;
using System.Web.UI.WebControls;

namespace FlyTrace
{
  public partial class LoginForm : System.Web.UI.Page
  {
    protected void Page_Load( object sender, EventArgs e )
    {
      // Taken from here: http://www.asp.net/web-forms/tutorials/security/membership/user-based-authorization-cs
      // If the login page is reached by an authenticated user with a querystring that includes the ReturnUrl parameter, 
      // then we know that this unauthenticated user just attempted to visit a page she is not authorized to view:
      if ( !IsPostBack &&
           Request.IsAuthenticated && 
           !string.IsNullOrEmpty( Request.QueryString["ReturnUrl"] ) )
      {
        Response.Redirect( "~/unauthorizedAccess.aspx" );
      }
    }
  }
}