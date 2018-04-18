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
function LabelMapControl(opt_options, fgColor, bckgColor, border) {
    // Initialization
    this.setValues(opt_options);

    this._rowDivs = [];
    this._lineDivs = [];

    var visibleControlRootDiv = this._visibleControlRootDiv = document.createElement('div');
    visibleControlRootDiv.style.cssText = 'position: relative; left: -50%; top: 5px;' +
                      'white-space: nowrap;  ' +
                      'padding: 2px; background-color: transparent; color: ' + fgColor;


    var wrapperDiv = this.wrapperDiv_ = document.createElement('div');
    wrapperDiv.appendChild(visibleControlRootDiv);
    wrapperDiv.style.cssText = 'position: absolute; display: none';

    this._bckgColor = bckgColor;
    this._border = border;

    //this.GetSpan = function ()
    //{
    //    return this.span_;
    //};
};
LabelMapControl.prototype = new google.maps.OverlayView;

// Implement onAdd
LabelMapControl.prototype.onAdd = function () {
    var pane = this.getPanes().overlayLayer;
    pane.appendChild(this.wrapperDiv_);

    // Ensures the label is redrawn if the text or position is changed.
    var me = this;
    this.listeners_ = [
       google.maps.event.addListener(this, 'position_changed',
           function () { me.draw(); }),
    ];
};

// Implement onRemove
LabelMapControl.prototype.onRemove = function () {
    this.wrapperDiv_.parentNode.removeChild(this.wrapperDiv_);

    // LabelMapControl is removed from the map, stop updating its position/text.
    for (var i = 0, I = this.listeners_.length; i < I; ++i) {
        google.maps.event.removeListener(this.listeners_[i]);
    }
};



LabelMapControl.prototype.setText = function (lines) {

    function areArraysSame(arr1, arr2) {
        if (arr1 == null || arr2 == null)
            return false;

        if (arr1.length !== arr2.length)
            return false;

        for (var i = 0; i < arr1.length; i++) {
            if (arr1[i] !== arr2[i])
                return false;
        }

        return true;
    }


    if (this._linesCopy != null && areArraysSame(this._linesCopy, lines))
        return;

    this._linesCopy = lines.slice();

    if (lines.length > this._rowDivs.length) {
        this.appendRowDivs(lines.length);
    } else {
        this.removeRowDivs(lines.length);
    }

    for (var iLine = 0; iLine < lines.length; iLine++) {
        this._lineDivs[iLine].innerHTML = lines[iLine];
    }

    //// LabelMapControl specific
    //var line1Div = this.line1Div_ = document.createElement('div');
    //line1Div.style.cssText = 'background-color: ' + bckgColor + '; border:' + border + ';' +
    //    'padding: 2px';

    //var line2Div = this.line2Div_ = document.createElement('div');
    //line2Div.style.cssText = 'background-color: ' + bckgColor + '; display: inline-block; ' +
    //    'position: relative; top: -1px; padding: 2px 2px 0 2px;' +
    //    'border-left:' + border + ';' +
    //    'border-right:' + border + ';' +
    //    'border-bottom:' + border + ';';

    //visibleControlRootDiv.appendChild(line1Div);
    //visibleControlRootDiv.appendChild(line2Div);
}


LabelMapControl.prototype.appendRowDivs = function (targetCount) {
    while (this._rowDivs.length < targetCount) {
        var rowDiv = document.createElement('div');
        rowDiv.className = 'marker-text-row';

        // The class got relative position, so shifting DIV's up so they ovelap by 1px:
        rowDiv.style.cssText = 'top:-' + this._rowDivs.length.toString() + 'px;';
        this._visibleControlRootDiv.appendChild(rowDiv);
        this._rowDivs.push(rowDiv);

        var lineDiv = document.createElement('div');
        lineDiv.className = 'marker-text-cell';
        lineDiv.style.cssText = 'background-color: ' + this._bckgColor + '; border:' + this._border + ';' +
            'padding: 2px';
        rowDiv.appendChild(lineDiv);
        this._lineDivs.push(lineDiv);
    }
}

LabelMapControl.prototype.removeRowDivs = function (targetCount) {
    while (this._rowDivs.length > targetCount) {
        var div = this._rowDivs.pop();
        this._lineDivs.pop();
        div.parentNode.removeChild(div);
    }
}


// Implement draw
LabelMapControl.prototype.draw = function () {
    var projection = this.getProjection();
    var position = projection.fromLatLngToDivPixel(this.get('position'));

    var div = this.wrapperDiv_;
    div.style.left = position.x + 'px';
    div.style.top = position.y + 'px';
    div.style.display = 'block';

    //var text = this.get('text').toString();

    //this.line1Div_.innerHTML = line1Contents;

    //var line2Contents = this.get('line2').toString();
    //if (line2Contents == null || line2Contents === '') {
    //    this.line2Div_.style.display = 'none';
    //} else {
    //    this.line2Div_.style.display = 'inline-block';
    //    this.line1Div_.innerHTML = line2Contents;
    //}

};