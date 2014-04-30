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
  public partial class DegMinSec : CoordControlBase
  {
    protected void Page_Init( object sender , EventArgs e )
    {
      this.nsDropDownList.Visible = IsLatitude;
      this.latDegRangeValidator.Enabled = IsLatitude;

      this.weDropDownList.Visible = IsLongitude;
      this.lonDegRangeValidator.Enabled = IsLongitude;

      if ( IsLatitude )
      {
        this.degTextBox.MaxLength = 2;
        if ( DefHemisphere != 0 )
          this.nsDropDownList.SelectedValue = DefHemisphere.ToString( );
      }
      else
      {
        this.degTextBox.MaxLength = 3;
        if ( DefHemisphere != 0 )
          this.weDropDownList.SelectedValue = DefHemisphere.ToString( );
      }
    }

    public char DefHemisphere { get; set; }

    public override double Value
    {
      get
      {
        double result =
          Global.ToDouble( this.degTextBox.Text ) +
          Global.ToDouble( this.minTextBox.Text ) / 60.0 +
          Global.ToDouble( this.secTextBox.Text + "." + this.secFractionsTextBox.Text ) / 60.0 / 60.0;

        if (
            ( IsLatitude && this.nsDropDownList.SelectedValue == "S" )
            ||
            ( IsLongitude && this.weDropDownList.SelectedValue == "W" )
          )
        {
          result = -result;
        }

        return result;
      }

      set
      {
        char prefix;
        int deg;
        int min;
        int sec;
        int secFraction;

        char negPrefix;
        char posPrefix;
        if ( CoordType == CoordType.Latitude )
        {
          negPrefix = 'S';
          posPrefix = 'N';
        }
        else
        {
          negPrefix = 'W';
          posPrefix = 'E';
        }

        CoordToDegMinSec( value , negPrefix , posPrefix , out prefix , out deg , out min , out sec, out secFraction );

        if ( CoordType == CoordType.Latitude )
        {
          this.nsDropDownList.SelectedValue = prefix.ToString( );
        }
        else
        {
          this.weDropDownList.SelectedValue = prefix.ToString( );
        }

        this.degTextBox.Text = deg.ToString( );
        this.minTextBox.Text = min.ToString( );
        this.secTextBox.Text = sec.ToString( );
        this.secFractionsTextBox.Text = secFraction.ToString( );
      }
    }

    public static void CoordToDegMinSec(
      double coord ,
      char negPrefix ,
      char posPrefix ,
      out char prefix ,
      out int deg ,
      out int min ,
      out int sec ,
      out int secFraction )
    {
      if ( coord < 0 )
        prefix = negPrefix;
      else
        prefix = posPrefix;

      coord = Math.Abs( coord );
      deg = ( int ) Math.Floor( coord );
      double minTotal = ( coord - deg ) * 60.0;
      min = ( int ) Math.Floor( minTotal );
      double secTotal = Math.Round( ( minTotal - min ) * 60.0 , 1 );
      sec = ( int ) Math.Floor( secTotal );

      // e.g. (59.9 - Math.Floor( 59.9 )) == 0.89999999999999858 (due to double precision rounding error). 
      // So without Math.Round secFraction would 8, not 9. So use Round:
      secFraction = ( int ) Math.Round( ( secTotal - sec ) * 10 );
    }
  }
}