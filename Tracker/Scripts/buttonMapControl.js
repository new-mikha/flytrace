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

// Buttons like "Show Pilot List"
function buttonMapControl(controlDiv, map, title, eventHandler, textColor)
{
    // Set CSS styles for the DIV containing the control
    // Setting padding to 5 px will offset the control
    // from the edge of the map
    controlDiv.style.padding = '5px';

    // Set CSS for the control border
    var controlUI = document.createElement('DIV');
    if (_ie8_or_less)
        controlUI.style.backgroundColor = 'white';
    else
        controlUI.style.backgroundColor = 'rgba(255,255,255, 0.8)';
    controlUI.style.borderStyle = 'solid';
    controlUI.style.borderWidth = '1px';
    controlUI.style.cursor = 'pointer';
    controlUI.style.textAlign = 'center';
    controlDiv.appendChild(controlUI);

    // Set CSS for the control interior
    this.controlText = document.createElement('DIV');
    this.controlText.style.fontFamily = 'Arial,sans-serif';
    this.controlText.style.fontSize = '14px';
    this.controlText.style.paddingLeft = '4px';
    this.controlText.style.paddingRight = '4px';
    this.defaultColor = this.controlText.style.color;
    if (textColor != null)
        this.controlText.style.color = textColor;
    else
        textColor = this.defaultColor;

    this.controlText.innerHTML = '<b>' + title + '</b>';
    this.isEnabled = true;
    this.normalColor = textColor;
    controlUI.appendChild(this.controlText);

    google.maps.event.addDomListener(controlUI, 'click', eventHandler);

    this.setText = function (text)
    {
        this.controlText.innerHTML = '<b>' + text + '</b>';
    };

    this.setDefaultColor = function ()
    {
        this.setColor(this.defaultColor);
    };

    this.setColor = function (color)
    {
        this.controlText.style.color = color;
        this.normalColor = color;
    };

    this.setEnabled = function (isEnabled)
    {
        this.isEnabled = isEnabled;
        if (this.isEnabled)
        {
            this.controlText.style.color = this.normalColor;
        } else
        {
            this.controlText.style.color = "#808080";
        }
    };

    this.getEnabled = function ()
    {
        return this.isEnabled;
    };

}
