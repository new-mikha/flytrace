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
function LabelMapControl(opt_options, fgColor, bckgColor, borderSizePx, borderStyleAndColor) {
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
    this._borderSizePx = borderSizePx;
    this._border = borderSizePx + 'px ' + borderStyleAndColor;

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

    this.arrangeLineBorders();

    this._needsUpdate = true;
}


LabelMapControl.prototype.appendRowDivs = function (targetCount) {
    while (this._rowDivs.length < targetCount) {
        var rowDiv = document.createElement('div');
        rowDiv.className = 'marker-text-row';

        var shiftPx = this._borderSizePx * this._rowDivs.length;

        // The class got relative position, so shifting DIV's up so they ovelap by 1px:
        rowDiv.style.cssText = 'top:-' + shiftPx + 'px;';
        this._visibleControlRootDiv.appendChild(rowDiv);
        this._rowDivs.push(rowDiv);

        var lineDiv = document.createElement('div');
        lineDiv.className = 'marker-text-cell';
        lineDiv.style.cssText = 'background-color: ' + this._bckgColor + '; border:' +
            this._border + ';' + 'padding: 2px; padding-bottom: 1px';
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

    if (this._needsUpdate) {
        this.arrangeLineBorders();

        this._needsUpdate = false;
    }
};

LabelMapControl.prototype.arrangeLineBorders = function () {
    if (!this._borderSizePx)
        return;

    if (!this._lineDivs || this._lineDivs.length === 0)
        return;

    var lineDivsOrderedByWidthDesc = this._lineDivs.slice();
    lineDivsOrderedByWidthDesc.sort(function (div1, div2) {
        return div2.clientWidth - div1.clientWidth;
    });


    var i;
    
    for (i = 0; i < lineDivsOrderedByWidthDesc.length; i++) {
        var rowDiv = lineDivsOrderedByWidthDesc[i].parentElement;
        rowDiv.style.zIndex = i;
    }

    var prevWidth;
    var thisWidth;
    var nextWidth = this._lineDivs[0].clientWidth;
    for (i = 0; i < this._lineDivs.length; i++) {
        thisWidth = nextWidth;

        nextWidth = (i < (this._lineDivs.length - 1)) ? this._lineDivs[i + 1].clientWidth : null;

        var hasTopBorder = prevWidth == null || prevWidth < thisWidth;
        var hasBottomBorder = nextWidth == null || nextWidth <= thisWidth;

       // console.log(i, hasTopBorder, hasBottomBorder);

        var div = this._lineDivs[i];

        if (hasTopBorder && hasBottomBorder) {
            div.style.borderLeft = '';
            div.style.borderRight = '';
            div.style.borderTop = '';
            div.style.borderBottom = '';
            div.style.border = this._border;
        } else if (hasTopBorder) {
            div.style.border = '';
            div.style.borderLeft = this._border;
            div.style.borderRight = this._border;
            div.style.borderTop = this._border;
            div.style.borderBottom = '';
        } else if (hasBottomBorder) {
            div.style.border = '';
            div.style.borderLeft = this._border;
            div.style.borderRight = this._border;
            div.style.borderTop = '';
            div.style.borderBottom = this._border;
        } else {
            div.style.border = '';
            div.style.borderLeft = this._border;
            div.style.borderRight = this._border;
            div.style.borderTop = '';
            div.style.borderBottom = '';
        }

        prevWidth = thisWidth;
    }
}