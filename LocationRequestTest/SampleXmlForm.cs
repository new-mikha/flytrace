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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LocationRequestTest
{
  public partial class SampleXmlForm : Form
  {
    public SampleXmlForm( )
    {
      InitializeComponent( );
    }

    public string SampleXml
    {
      get
      {
        return this.xmlTextBox.Text;
      }

      set
      {
        this.xmlTextBox.Text = value;
        this.xmlTextBox.SelectionLength = 0;
        this.xmlTextBox.SelectionStart = this.xmlTextBox.Text.Length;
      }
    }

    private void resetButton_Click( object sender, EventArgs e )
    {
      this.xmlTextBox.Text = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";
    }
  }
}