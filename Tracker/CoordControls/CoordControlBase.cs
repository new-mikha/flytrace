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

namespace FlyTrace.CoordControls
{
  public class CoordControlBase : System.Web.UI.UserControl
  {
    public CoordType CoordType { get; set; }

    public string ValidationGroup { get; set; }

    protected bool IsLatitude
    {
      get { return CoordType == CoordType.Latitude; }
    }

    protected bool IsLongitude
    {
      get { return CoordType == CoordType.Longitude; }
    }

    public virtual double Value
    {
      get
      {
        // should be overriden in descendant. Not making the class abastract to let the designer work.
        throw new NotImplementedException( );
      }

      set
      {
        // should be overriden in descendant. Not making the class abastract to let the designer work.
        throw new NotImplementedException( );
      }
    }

    protected override void OnLoad( EventArgs e )
    {
      base.OnLoad( e );

      if ( !string.IsNullOrEmpty( ValidationGroup ) )
      {
        foreach ( Control control in Controls )
        {
          if ( control is BaseValidator )
          {
            ( control as BaseValidator ).ValidationGroup = ValidationGroup;
          }
        }
      }
    }
  }
}