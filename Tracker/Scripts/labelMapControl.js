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

// Define the overlay, derived from google.maps.OverlayView
function LabelMapControl(opt_options, fgColor, bckgColor, border)
{
    // Initialization
    this.setValues(opt_options);

    // LabelMapControl specific
    var span = this.span_ = document.createElement('span');
    span.style.cssText = 'position: relative; left: -50%; top: 5px;' +
                      'white-space: nowrap; ' +
                      'padding: 2px; background-color: ' + bckgColor + '; color: ' + fgColor + '; border:' + border;

    var div = this.div_ = document.createElement('div');
    div.appendChild(span);
    div.style.cssText = 'position: absolute; display: none';

    this.GetSpan = function ()
    {
        return this.span_;
    };
};
LabelMapControl.prototype = new google.maps.OverlayView;

// Implement onAdd
LabelMapControl.prototype.onAdd = function ()
{
    var pane = this.getPanes().overlayLayer;
    pane.appendChild(this.div_);

    // Ensures the label is redrawn if the text or position is changed.
    var me = this;
    this.listeners_ = [
   google.maps.event.addListener(this, 'position_changed',
       function () { me.draw(); }),
   google.maps.event.addListener(this, 'text_changed',
       function () { me.draw(); })
 ];
};

// Implement onRemove
LabelMapControl.prototype.onRemove = function ()
{
    this.div_.parentNode.removeChild(this.div_);

    // LabelMapControl is removed from the map, stop updating its position/text.
    for (var i = 0, I = this.listeners_.length; i < I; ++i)
    {
        google.maps.event.removeListener(this.listeners_[i]);
    }
};

// Implement draw
LabelMapControl.prototype.draw = function ()
{
    var projection = this.getProjection();
    var position = projection.fromLatLngToDivPixel(this.get('position'));

    var div = this.div_;
    div.style.left = position.x + 'px';
    div.style.top = position.y + 'px';

    var contents = this.get('text').toString();
    div.style.display = 'block';
    this.span_.innerHTML = contents;
};