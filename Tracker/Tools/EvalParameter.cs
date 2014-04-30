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

namespace FlyTrace.Tools
{
  public class EvalParameter : System.Web.UI.WebControls.Parameter
  {
    public string Expression { get; set; }

    protected override object Evaluate( HttpContext context , System.Web.UI.Control control )
    {
      if ( string.IsNullOrEmpty( Expression ) )
        throw new ArgumentException( "Expression cannot be empty" );

      return DataBinder.Eval( context.CurrentHandler , Expression );
    }
  }
}