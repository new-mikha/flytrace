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


// Page.MaintainScrollPositionOnPostback="true" doesn't seem to be working for Chrome, so making it manually.
// Note that MaintainScrollPositionOnPostback should be false or default if this csrip is in use.

// Code here taken from
// http://stackoverflow.com/questions/3584802/is-there-any-way-to-implement-maintainscrollpositiononpostback-functionality-in
// (with modification mentioned in the comment there:
// "I changed this line: $(window).scroll(curPosition); to use the scrollTop function, and this worked great."
// That didn't work without changing to scrollTop here as well.

// client id of the hidden input field. Need to be set by setScrollHiddenInputId
var _scrollingHiddenInputId;

// store the current scroll position into the input
function storeScrollPosition()
{
    $('#' + _scrollingHiddenInputId)[0].value = scrollPosition();
}

// load the value out of the input and scroll the page
function loadScrollPosition()
{
    var curPosition = $('#' + _scrollingHiddenInputId)[0].value;
    if (curPosition > 0)
    {
        $(window).scrollTop(curPosition);
    }
}

// determine the scroll position (cross browser code)
function scrollPosition()
{
    var n_result = window.pageYOffset ?
                   window.pageYOffset : 0;
    var n_docel = document.documentElement ?
                  document.documentElement.scrollTop : 0;
    var n_body = document.body ?
                 document.body.scrollTop : 0;
    if (n_docel && (!n_result || (n_result > n_docel)))
        n_result = n_docel;
    return n_body && (!n_result || (n_result > n_body)) ? n_body : n_result;
}

// on load of the page, load the previous scroll position
$(document).ready(function () { loadScrollPosition(); });
// on scroll of the page, update the input field
$(window).scroll(function () { storeScrollPosition(); });


function setScrollHiddenInputId(inputId)
{
    _scrollingHiddenInputId = inputId;
}