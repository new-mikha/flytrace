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

namespace FlyTrace.Service.Test
{
  public partial class TestControl : System.Web.UI.Page
  {
    protected void Page_Load( object sender, EventArgs e )
    {
    }

    protected void Page_PreRender( object sender, EventArgs e )
    {
      if ( !IsPostBack )
      {
        IsAutoUpdateCheckBox.Checked = TestSource.Singleton.IsAutoUpdate;
      }
      else
      {
        TestSource.Singleton.IsAutoUpdate = IsAutoUpdateCheckBox.Checked;
      }

      PositionNumTextBox.Text = TestSource.Singleton.PositionNumber.ToString( );
    }

    protected void ResetButton_Click( object sender, EventArgs e )
    {
      TestSource.Singleton.PositionNumber = 0;
      MgrService.ClearCache( );
    }

    protected void IncreaseByOneButton_Click( object sender, EventArgs e )
    {
      TestSource.Singleton.PositionNumber++;
    }

    protected void IncreaseByTenButton_Click( object sender, EventArgs e )
    {
      TestSource.Singleton.PositionNumber += 10;
    }

    protected void SetPositionButton_Click( object sender, EventArgs e )
    {
      TestSource.Singleton.PositionNumber = int.Parse( PositionNumTextBox.Text );
    }
  }
}