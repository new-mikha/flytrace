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
  public partial class Deg : CoordControlBase
  {

    protected void Page_Load( object sender , EventArgs e )
    {
      this.latDegRequiredFieldValidator.Enabled = IsLatitude;
      this.latDegRangeValidator.Enabled = IsLatitude;

      this.lonDegRequiredFieldValidator.Enabled = IsLongitude;
      this.lonDegRangeValidator.Enabled = IsLongitude;

      if ( IsLatitude )
      {
        this.degTextBox.MaxLength = 3; // including sign
      }
      else
      {
        this.degTextBox.MaxLength = 4; // including sign
      }
    }

    public override double Value
    {
      get
      {
        return Global.ToDouble( this.degTextBox.Text + "." + this.degFractionsTextBox.Text );
      }

      set
      {
        int sign = Math.Sign( value );
        double coord = Math.Abs( value );

        int deg = ( int ) Math.Floor( coord );
        this.degTextBox.Text = ( deg * sign ).ToString( );

        double fractionPart =
          ( coord - deg ) 
          *
          Math.Pow( 10.0 , this.degFractionsTextBox.MaxLength );

        int fractions = ( int ) Math.Round( fractionPart );
        this.degFractionsTextBox.Text = fractions.ToString( );
      }
    }
  }
}