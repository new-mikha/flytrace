///////////////////////////////////////////////////////////////////////////////
//  Flytrace, online viewer for GPS trackers.
//  Copyright (C) 2011-2014 Mikhail Karmazin
//  
//  This file is part of Flytrace.
//  
//  Flytrace is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//  
//  Flytrace is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//  
//  You should have received a copy of the GNU Affero General Public License
//  along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
///////////////////////////////////////////////////////////////////////////////

function confirmDeletingFromGrid(ctrl, message)
{
    var parent = ctrl.parentNode;
    var origClassName;
    var origBckgColor;
    while (parent != null)
    {
        if (parent.tagName == 'TR')
        {
            origClassName = parent.className;
            origBckgColor = parent.style.backgroundColor;

            // the row might be already selected, so use a style that differs from multi-select style 
            // used in selectDeselectRow, as well as from "just added" color 
            parent.className = "WarningRow";
            parent.style.backgroundColor = "";

            break;
        }
        parent = parent.parentNode;
    }

    var result = confirm(message);

    if (!result && (parent != null))
    {
        parent.className = origClassName;
        parent.style.backgroundColor = origBckgColor;
    }

    return result;
}