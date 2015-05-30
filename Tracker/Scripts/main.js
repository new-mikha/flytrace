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

var _map;
var _stdShadow;
var _arrowShadow;
var _flagShadow;
var _infoWindow = new google.maps.InfoWindow();

var _tracksEnabled = true;

var _autoUpdateEnabled = true;

function initialize() {
    try {
        var queryString = function () {
            // This function is anonymous, is executed immediately and 
            // the return value is assigned to queryString!
            var query_string = {};
            var query = window.location.search.substring(1);
            var vars = query.split("&");
            for (var i = 0; i < vars.length; i++) {
                var pair = vars[i].split("=");

                var key = pair[0];
                if (typeof key !== "undefined")
                    key = key.toLowerCase();

                var val = pair[1];
                if (typeof val !== "undefined")
                    val = val.toLowerCase();

                if (typeof query_string[key] === "undefined") {
                    // If first entry with this name
                    query_string[key] = val;
                } else if (typeof query_string[key] === "string") {
                    // If second entry with this name
                    var arr = [query_string[key], val];
                    query_string[key] = arr;
                } else {
                    // If third or later entry with this name
                    query_string[key].push(val);
                }
            }
            return query_string;
        }();

        if (_shouldLog)
            $('#logDiv').show();
        else
            $('#logDiv').hide();

        _autoUpdateEnabled = !(queryString.useautoupdate == "0" || queryString.useautoupdate == "false");

        if (_groupId == 132 ||
            _groupId == 138 ||
            _groupId == 139 ||
            _groupId == 140 ||
            _groupId == 141 ||
            _groupId == 142 ||
            _groupId == 143)
            _tracksEnabled = false;

        _stdShadow = new google.maps.MarkerImage(
            'http://maps.google.com.au/mapfiles/ms/micons/msmarker.shadow.png',
            new google.maps.Size(59, 32), // size
            new google.maps.Point(0, 0), // origin
            new google.maps.Point(15, 32)	// anchor
        );

        _arrowShadow = new google.maps.MarkerImage(
            'http://maps.google.com/mapfiles/arrowshadow.png',
            new google.maps.Size(39, 34), // size
            new google.maps.Point(0, 0), // origin
            new google.maps.Point(11, 34)	// anchor
        );

        _flagShadow = new google.maps.MarkerImage(
            'App_Themes/Default/flag_shadow.png',
            new google.maps.Size(51, 37), // size
            new google.maps.Point(0, 0), // origin
            new google.maps.Point(22, 36)	// anchor
        );

        var suggLat = Number($('#' + _reloadLatId)[0].value);
        var suggLon = Number($('#' + _reloadLonId)[0].value);
        var suggZoom = Number($('#' + _reloadZoomId)[0].value);

        if (suggLat == "" || suggLat == null || isNaN(suggLat) ||
            suggLon == "" || suggLon == null || isNaN(suggLon) ||
            suggZoom == "" || suggZoom == null || suggZoom <= 0 || isNaN(suggZoom)) {
            suggLat = -33.361073;
            suggLon = 147.9303;
            suggZoom = 12;
            _reloaded = false;
        } else {
            _reloaded = true;
        }

        var latlng;
        try {
            latlng = new google.maps.LatLng(suggLat, suggLon);
        }
        catch (ignored) {
            latlng = new google.maps.LatLng(-33.361073, 147.9303)
            _reloaded = false;
        }

        var myOptions =
        {
            zoom: suggZoom,
            center: latlng,
            scaleControl: true,
            streetViewControl: false,
            mapTypeId: google.maps.MapTypeId.ROADMAP
        };

        _map = new google.maps.Map(document.getElementById("mapPanel"), myOptions);

        addMapButton("Update Now", onLookup, google.maps.ControlPosition.TOP_LEFT);

        addMapButton("Show Pilot List", togglePanels, google.maps.ControlPosition.TOP_LEFT);

        var tempCoordFormat = queryString.defcoordformat;

        if (tempCoordFormat !== 'deg' &&
            tempCoordFormat !== 'degmin' &&
            tempCoordFormat !== 'degminsec') {
            tempCoordFormat = getCookie('flytrace_coord_format');
            if (typeof tempCoordFormat !== "undefined")
                tempCoordFormat = tempCoordFormat.toLowerCase();
        }

        if (tempCoordFormat == 'deg' ||
            tempCoordFormat == 'degmin' ||
            tempCoordFormat == 'degminsec') {
            _coordFormat = tempCoordFormat;
            $("#coordFormatSelect").val(_coordFormat);
        }

        setPreFormatLink();

        _numAllTracksAlertShown = getCookie('numAllTracksAlertShown');
        if (isNaN(parseInt(_numAllTracksAlertShown))) {
            _numAllTracksAlertShown = 0;
        } else {
            _numAllTracksAlertShown = parseInt(_numAllTracksAlertShown);
        }

        _showTracksButton =
            addMapButton(
                "Show tracks",
                onAllTracksButtonPressed,
                google.maps.ControlPosition.LEFT_TOP
            );

        addMapButton(
            "Fit All Pilots",
            function () {
                fitAllTrackers(true);
            },
            google.maps.ControlPosition.LEFT_TOP
        );

        setInterval("clock()", 1000);

        if (_useLocationSensor) {
            addUserLocationMarker();
        }

        syncAllTracksButton();

        showTask();

        showLogos();
    }
    catch (exc) {
        alert(exc.message);
    }
}

function updateUrlParameter(url, param, paramVal) {
    var newAdditionalURL = "";
    var tempArray = url.split("?");
    var baseURL = tempArray[0];
    var additionalURL = tempArray[1];
    var temp = "";
    if (additionalURL) {
        tempArray = additionalURL.split("&");
        for (i = 0; i < tempArray.length; i++) {
            if (tempArray[i].split('=')[0] != param) {
                newAdditionalURL += temp + tempArray[i];
                temp = "&";
            }
        }
    }

    var rows_txt = temp + "" + param + "=" + paramVal;
    return baseURL + "?" + newAdditionalURL + rows_txt;
}

function setPreFormatLink() {
    var format = $("#coordFormatSelect").val();

    var url = window.location.href.toLowerCase();
    url = updateUrlParameter(url, "defcoordformat", format);
    document.getElementById('preFormatLink').setAttribute('href', url);
}


function showLogos() {
    if (_logoSource == "" && _smallLogoSource == "" ) return;

    var logoToUse;

    if (_logoSource == "")
        logoToUse = _smallLogoSource;
    else if (_smallLogoSource == "")
        logoToUse = _logoSource;
    else if (screen.width < 500 || screen.height < 500)
        logoToUse = _smallLogoSource;
    else
        logoToUse = _logoSource;

    var homeControlDiv = document.createElement('div');
    homeControlDiv.style.padding = '5px';
    homeControlDiv.innerHTML = "<a href='http://www.advridermag.com.au' onclick='logoClicked();' target='flytrace_advridermag'><img src='" + logoToUse + "'/></a>";

    homeControlDiv.index = 1;
    _map.controls[google.maps.ControlPosition.RIGHT_BOTTOM].push(homeControlDiv);
}

var WpElemNum = 4;

var _showTracksButton = null;

var _allTracksRequested = false;

var _reloaded;

var _testDlbFlag = false;
var _testDlb = 0;

function onAllTracksButtonPressed() {
    _testDlbFlag = true;

    if (!_tracksEnabled) {
        alert('Tracks disabled for this group.');
        log('Tracks disabled for this group.');
    } else if (!_showTracksButton.getEnabled()) {
        alert('No coordinates received yet for pilots in the group, so no tracks to show.');
        log('No coordinates received yet for pilots in the group, so no tracks to show.');
    } else {
        if (_allTracksRequested) {
            hideAllTracks();

            if (_shouldLog)
                log('Hiding all tracks...');
        } else {
            showAllTracks();

            if (_shouldLog)
                log('Showing all tracks...');
        }
    }
}

function showTask() {
    try {
        if (_task == null) return;

        var nWaypoints = _task.length / WpElemNum;

        if (nWaypoints < 2) return;

        taskPath = new Array();


        for (var iWp = 0; iWp < nWaypoints; iWp++) {
            var name = _task[iWp * WpElemNum];
            var lat = _task[iWp * WpElemNum + 1];
            var lon = _task[iWp * WpElemNum + 2];
            var radius = _task[iWp * WpElemNum + 3];

            var latlng = new google.maps.LatLng(lat, lon);

            taskPath.push(latlng);

            var radiusOptions = {
                clickable: false,
                strokeColor: "#008800",
                strokeWeight: 1,
                fillColor: "#008800",
                fillOpacity: 0.05,
                map: _map,
                center: latlng,
                radius: radius,
                zIndex: 0
            };
            var radiusCircle = new google.maps.Circle(radiusOptions);

            var title = iWp.toString() + ': ' + name;

            // It doesn't work without binding the label to a marker. Worked earlier. Looks
            // like a GoogleMaps bug? Even if not, they definitely have changed something.

            var fooImage = new google.maps.MarkerImage(
                "App_Themes/Default/dot.png",
                new google.maps.Size(1, 1), // size
                new google.maps.Point(0, 0), // origin
                new google.maps.Point(0, 0)
            );

            var fooMarker = new google.maps.Marker({
                position: latlng,
                map: _map,
                icon: fooImage,
                clickable: false,
                title: title,
                zIndex: 1
            });

            var label = new LabelMapControl(
                {
                    map: _map,
                    zIndex: 1
                },
                '#FF0000',
                'rgba(0,0,0,0)',
                '0px'
            );
            label.bindTo('position', fooMarker, 'position');
            label.bindTo('text', fooMarker, 'title');
        }

        _taskPolyline =
            new google.maps.Polyline({
                clickable: false,
                path: taskPath,
                strokeColor: "#FF0000",
                strokeOpacity: 0.8,
                strokeWeight: 2,
                zIndex: 0
            });
        _taskPolyline.setMap(_map);
    }
    catch (exc) {
        alert(exc.message);
    }

}

var _taskPolyline = null;

function addUserLocationMarker() {
    try {
        navigator.geolocation.watchPosition(
            gotPosition,
            errorGettingPosition,
            { 'enableHighAccuracy': true, 'timeout': 10000, 'maximumAge': 20000 });
    }
    catch (exc) {
        // ignore
    }
}

function errorGettingPosition(err) {
    // ignore
}

var _currentLocationMarker;

var _accuracyCircle;

function gotPosition(pos) {
    try {
        var latlng = new google.maps.LatLng(pos.coords.latitude, pos.coords.longitude);

        if (_currentLocationMarker != null) {
            _currentLocationMarker.setPosition(latlng);
            _accuracyCircle.setCenter(latlng);
            _accuracyCircle.setRadius(pos.coords.accuracy);
        } else {
            var image = new google.maps.MarkerImage(
                "App_Themes/Default/current_position.png",
                new google.maps.Size(16, 16), // size
                new google.maps.Point(0, 0), // origin
                new google.maps.Point(8, 8)
            );

            var accuracyCircleOptions = {
                strokeWeight: 0,
                fillColor: "#0000FF",
                fillOpacity: 0.15,
                map: _map,
                center: latlng,
                radius: pos.coords.accuracy,
                zIndex: 0
            };
            _accuracyCircle = new google.maps.Circle(accuracyCircleOptions);

            _currentLocationMarker = new google.maps.Marker({
                position: latlng,
                map: _map,
                icon: image,
                zIndex: 255
            });

            addInfoWindowHandler(_currentLocationMarker);

            addMapButton("My Position", function () {
                _map.setCenter(_currentLocationMarker.getPosition());

            },
                google.maps.ControlPosition.LEFT_TOP);
        }
    }
    catch (exc) {
        alert(exc.message);
    }
}

function addMapButton(title, eventHandler, screenPosition, textColor) {
    var controlDiv = document.createElement('DIV');
    var control = new buttonMapControl(controlDiv, _map, title, eventHandler, textColor);
    controlDiv.index = 1;
    _map.controls[screenPosition].push(controlDiv);

    return control;
}

function isMarkerDisplayble(netTrackerData) {
    return isHavingCoordinates(netTrackerData) &&
        (isSosType(netTrackerData) || !netTrackerData.IsHidden);
}

function isHavingCoordinates(netTrackerData) {
    return netTrackerData.Type != null && !isWaitType(netTrackerData);
}

function isWaitType(netTrackerData) {
    return netTrackerData.Type === "wait";
}

function isTrackType(netTrackerData) {
    return netTrackerData.Type === "" || netTrackerData.Type === "TRACK";
}

function isOkType(netTrackerData) {
    return netTrackerData.Type === "OK" || netTrackerData.Type === "TEST";
}

function isCustomType(netTrackerData) {
    return netTrackerData.Type === "CUSTOM";
}

function isHelpType(netTrackerData) {
    return false; // at the moment, treat HELP as SOS
}

function isSosType(netTrackerData) {
    return !isTrackType(netTrackerData) && !isOkType(netTrackerData) && !isCustomType(netTrackerData) && !isHelpType(netTrackerData);
}

var _isInfoWindowOpen = false;

var _infoWindowOpenMarker = null;

function getMarkerImageAndShadow(netTrackerData) {
    var result = new Object();
    var iconPath;

    var trackerName = netTrackerData.Name;

    if (isTrackType(netTrackerData)) {
        iconPath = 'http://maps.google.com.au/intl/en_us/mapfiles/ms/micons/red-dot.png';

        var anchor = new google.maps.Point(15, 32); // seems that Google icons have differen anchors.
        if (trackerName != null && trackerName != "") {
            var firstChar = trackerName.toUpperCase().charAt(0);
            if (firstChar >= 'A' && firstChar <= 'Z') {
                iconPath = 'http://maps.google.com.au/mapfiles/marker' + firstChar + '.png';

                anchor = new google.maps.Point(9, 32);
            }
        }

        result.image = new google.maps.MarkerImage(
            iconPath,
            new google.maps.Size(32, 32), // size
            new google.maps.Point(0, 0), // origin
            anchor
        );

        result.shadow = _stdShadow;
    } else {
        if (isOkType(netTrackerData) || isCustomType(netTrackerData) || isHelpType(netTrackerData)) {
            if (isOkType(netTrackerData)) {
                iconPath = 'App_Themes/Default/finish.png';
            } else if (isCustomType(netTrackerData)) {
                iconPath = 'App_Themes/Default/finish-custom.png';
            } else {
                iconPath = 'http://maps.google.com/mapfiles/arrow.png';
            }

            result.image = new google.maps.MarkerImage(
                iconPath,
                new google.maps.Size(32, 37), // size
                new google.maps.Point(0, 0), // origin
                new google.maps.Point(16, 37) // anchor
            );

            result.shadow = _flagShadow;
        }
        else {
            iconPath = 'App_Themes/Default/redarrow.png';

            result.image = new google.maps.MarkerImage(
                iconPath,
                new google.maps.Size(39, 34), // size
                new google.maps.Point(0, 0), // origin
                new google.maps.Point(11, 34) // anchor
            );

            result.shadow = _arrowShadow;
        }
    }

    return result;
}

function addInfoWindowHandler(marker) {
    google.maps.event.addListener(marker, 'click', function () {
        try {
            if (_isInfoWindowOpen && _infoWindowOpenMarker == marker) {
                closeInfoWindow();
            } else {
                _infoWindow.setContent(getContent(marker));
                _infoWindow.open(_map, marker);
                _infoWindowOpenMarker = marker;
                _isInfoWindowOpen = true;
            }
        } catch (exc) {
            alert(exc.message);
        }
    });
}

var _prevTime;

var _isOnCall = false;

var _requiredSecondsBetweenCalls = 30;

function setupMarkerAndLabel(netTrackerData) {
    var location = new google.maps.LatLng(netTrackerData.Lat, netTrackerData.Lon);

    var imageDescr = getMarkerImageAndShadow(netTrackerData);

    var marker = new google.maps.Marker({
        position: location,
        map: _map,
        icon: imageDescr.image,
        shadow: imageDescr.shadow,
        title: netTrackerData.Name,
        zIndex: 1
    });

    addInfoWindowHandler(marker);

    var lblBackColor;
    if (_ie8_or_less)
        lblBackColor = 'white';
    else
        lblBackColor = 'rgba(255,255,255, 0.8)';

    var label = new LabelMapControl(
        {
            map: _map,
            zIndex: 2
        },
        'black',
        lblBackColor,
        '1px solid green'
    );
    label.bindTo('position', marker, 'position');
    label.bindTo('text', marker, 'title');

    marker.label = label;

    return marker;
}

function clock() {
    try {
        if (_testDlbFlag && _shouldLog) {
            _testDlb++;
            if (_testDlb == 10)
                sendLog();

            if (_testDlb == 30)
                sendLog();
        }

        var d = new Date();
        var secondsPassed;
        var secondsLeft;
        if (_prevTime != null) {
            secondsPassed = Math.round((d.getTime() - _prevTime.getTime()) / 1000);
            secondsLeft = Math.max(1, _requiredSecondsBetweenCalls - secondsPassed);
        }

        if (!_isOnCall) {
            var shouldCall = false;
            if (secondsPassed == null) {
                shouldCall = true;
            } else {
                shouldCall = (secondsPassed >= _requiredSecondsBetweenCalls);

                var statusStr;
                if (_succCallTime == null) {
                    statusStr = "Positions haven't been retrieved yet, " +
                        secondsLeft +
                        " sec before the next attempt (or press 'Update Now').";
                } else {
                    statusStr = "Positions refreshed " + getAgeStrWithSeconds(_succCallTime) + " ago";
                    if (anyPilotShown()) {
                        statusStr = statusStr + ".";
                    } else if (_trackerHolders.length == 0) {
                        statusStr = statusStr + ", there are no pilots assigned to the group.";
                    }
                    else {
                        statusStr = statusStr + ", no coordinates received yet for pilots in the group.";
                    }
                }

                showStatus(statusStr);
            }

            if (shouldCall && _autoUpdateEnabled) {
                onLookup();
            }
        }

        checkTrackers();

        if (_hasError && _errorTs != null) {
            var errAge = getAgeStrWithSeconds(_errorTs);
            var msg;

            if (_errorMsg == "")
                msg = "Failed to connect"
            else
                msg = "Got '" + _errorMsg + "' error";

            msg = msg + " " + errAge + " ago, try to fix it by 'Update Now'";

            if (secondsLeft != null && _autoUpdateEnabled) {
                msg = msg + " or wait for auto-update that should happen in " + secondsLeft.toString() + " sec";
            }

            document.getElementById("errorLabel").innerHTML = msg;
            positionContent();
        }

        if (_alertsToShowOnNextTimer != null) {
            for (var iMsg = 0; iMsg < _alertsToShowOnNextTimer.length; iMsg++) {
                alert(_alertsToShowOnNextTimer[iMsg]);
            }
            _alertsToShowOnNextTimer = null;
        }

        if (_shouldAutoFitOnTimer && $('#mapPanel').is(':visible')) {
            fitAllTrackers(false);
            _shouldAutoFitOnTimer = false;
        }

    }
    catch (e) {
        showError(e.message);
    }
}

var _alertsToShowOnNextTimer = null;

// Use this instead of alert() to show a message when all screen updates are done (original alert blocks execution)
function setAlertToShowOnNextTimer(msg) {
    if (_alertsToShowOnNextTimer == null) {
        _alertsToShowOnNextTimer = new Array();
    }
    _alertsToShowOnNextTimer.push(msg);
}

function anyPilotShown() {
    var result = false;

    if (_trackerHolders != null) {
        for (var iTrackerHolder = 0; iTrackerHolder < _trackerHolders.length; iTrackerHolder++) {
            var trackerHolder = _trackerHolders[iTrackerHolder];
            if (trackerHolder.NetTrackerData.Type != null) {
                result = true;
                break;
            }
        }
    }

    return result;
}

function getAgeStr(startTime, additionalSeconds) {
    var d = new Date();

    var secondsMain = Math.round((d.getTime() - startTime.getTime()) / 1000);
    var ageTotalMinutes = Math.floor((secondsMain + additionalSeconds) / 60);

    var days = Math.floor(ageTotalMinutes / 60 / 24);
    var hours = Math.floor(ageTotalMinutes / 60) % 24;
    var minutes = Math.floor(ageTotalMinutes) % 60;

    ageStr = minutes.toString() + " min";
    if (hours != 0) ageStr = hours.toString() + " hr " + ageStr;
    if (days != 0) ageStr = days.toString() + " d " + ageStr;

    return ageStr;
}

function getAgeStrWithSeconds(startTime) {
    var d = new Date();
    var seconds = Math.round((d.getTime() - startTime.getTime()) / 1000);

    if (seconds >= 60)
        return getAgeStr(_succCallTime, 0);

    return seconds.toString() + " sec";
}

var _secondsToBounce = 10;

function checkTrackers() {
    if (_trackerHolders == null) return;

    var d = new Date();
    for (var iTrackerHolder = 0; iTrackerHolder < _trackerHolders.length; iTrackerHolder++) {
        var trackerHolder = _trackerHolders[iTrackerHolder];

        if (trackerHolder.BouncingStartTime != null) {
            var secondsBouncing = Math.round((d.getTime() - trackerHolder.BouncingStartTime.getTime()) / 1000);
            if (secondsBouncing > _secondsToBounce) {
                trackerHolder.marker.setAnimation(null);
                trackerHolder.BouncingStartTime = null;
            }
        }

        showAge(trackerHolder);
    }
}

function showStatus(statusStr) {
    document.getElementById("statusLabel").innerHTML = statusStr;
    positionContent();
}

var _errorTs;
var _errorMsg;
var _hasError = false;


function showError(message) {
    _hasError = true;
    //document.getElementById("horSplitter").style.display = "inherit";
    document.getElementById("horSplitter").style.backgroundColor = "";
    document.getElementById("errorLabel").innerHTML = "<b>&nbsp;" + message + "</b>";
    _errorMsg = message;
    _errorTs = new Date();
    positionContent();
}

function clearError() {
    _hasError = false;
    //document.getElementById("horSplitter").style.display = "none";
    document.getElementById("horSplitter").style.backgroundColor = "#00FF00";
    document.getElementById("errorLabel").innerHTML = "";
    _errorTs = null;
    _errorMsg = null;
    positionContent();
}

function onLookup() {
    try {
        _prevTime = new Date();
        FlyTrace.Service.TrackerService.GetCoordinates(_groupId, _currentSeed, _prevTime, onLookupComplete, onError);
        showStatus("Getting data from the server...");
        _isOnCall = true;
    }
    catch (e) {
        showError(e.message);
    }
}

function onError(result) {
    _isOnCall = false;
    showStatus("Can't connect.");
    showError("");
    checkFullTracks(true);
}

var _succCallTime;

var _trackColors =
    ['#CC0099',
        '#382512',
        '#0000CC',
        '#FF6600',
        '#006600'
    ];

var _currentSeed = null;

var _currentVersion = null;

function onLookupComplete(result) {
    _isOnCall = false;
    try {
        clearError();

        log("Current seed: " + _currentSeed);
        log("Source seed: " + result.Src);
        log("Result seed: " + result.Res);

        log("Current version : " + _currentVersion);
        log("Result version : " + result.Ver);

        if (_currentVersion != null && result.Ver != null && _currentVersion != result.Ver) {
            $('#' + _reloadLatId)[0].value = _map.getCenter().lat();
            $('#' + _reloadLonId)[0].value = _map.getCenter().lng();
            $('#' + _reloadZoomId)[0].value = _map.getZoom();
            __doPostBack(_reloadLatId, "");
            return;
        }

        _currentVersion = result.Ver;

        var isIncremental = result.Src != null && result.Src !== "";

        var hasAnythingToProcess =
            result.Res != "NIL" &&
                (!isIncremental ||
                  result.Src == _currentSeed // if incremental, but Src!=_currentSeed then it's result of some old call and this incremental result is just wrong for the current state
                );

        if (result.IncrSurr) {
            // case when it's isIncremental but Src != _currentSeed is a bit too complicated to check, so avoid testing it.
            // But if it's incremental and Src == _currentSeed then go ahead with testing:
            hasAnythingToProcess = !isIncremental || result.Src == _currentSeed;

            if (result.Res == "NIL") isIncremental = true;
        }

        log("hasAnythingToProcess: " + hasAnythingToProcess);

        if (hasAnythingToProcess)
            processResult(result, isIncremental);

        _succCallTime = new Date();
    }
    catch (e) {
        log(e.message);
        showStatus("Can't process retrieved data.");
        showError(e.message);
    }

    checkFullTracks(false); // now check for tracks that were requested but waiting for retrieve
    syncAllTracksButton();
}

var _startTs = null;

function processResult(result, isIncremental) {
    showStatus("Processing retrieved data...");
    if (_trackerHolders == null) {
        _trackerHolders = new Array();
    }

    var incrDebugMsg = "";

    var gotMarkersAdded = false;

    var isStartTsChanged = false;
    if (!isIncremental) {
        var oldStartTs = _startTs;

        if (result.StartTs === undefined)
            _startTs = null;
        else
            _startTs = result.StartTs;

        isStartTsChanged = oldStartTs !== _startTs;
    }

    if (result.Trackers != null) {
        var addedRow = false;
        for (var iResult = 0; iResult < result.Trackers.length; iResult++) {
            var netTrackerData = result.Trackers[iResult];

            if (netTrackerData.Lat === undefined)
                netTrackerData.Lat = 0;

            if (netTrackerData.Lon === undefined)
                netTrackerData.Lon = 0;

            if (netTrackerData.IsHidden === undefined)
                netTrackerData.IsHidden = false;

            var trackerHolder = FindTrackerHolder(netTrackerData.Name);

            // logically it's possible that we already have coordinates (or error, or whatever) when 
            // next call returns "wait" for the tracker. In this case we ignore this tracker for the moment.
            if (isWaitType(netTrackerData) &&
                trackerHolder != null &&
                trackerHolder.NetTrackerData != null) {
                if (isIncremental) {
                    incrDebugMsg += "Incremental but got WAIT tracker " + netTrackerData.Name + "\r\n";
                }
                continue;
            }

            var trackPointsToAdd = 0;

            if (trackerHolder == null) {
                if (isIncremental) {
                    incrDebugMsg += "Incremental but got new tracker " + netTrackerData.Name + "\r\n";
                }

                trackerHolder = new Object();
                trackerHolder.Name = netTrackerData.Name;

                trackerHolder.trackData = new Object();
                trackerHolder.trackData.track = null;
                trackerHolder.trackData.shouldDisplayTrack = false;
                trackerHolder.trackData.needGetTrack = false;
                trackerHolder.trackData.isFirstPriority = false;
                trackerHolder.trackData.latestReceivedTs = null;
                trackerHolder.trackData.trackPolyline = null;
                trackerHolder.trackData.color = _trackColors[iResult % _trackColors.length];

                if (isMarkerDisplayble(netTrackerData)) {
                    trackerHolder.marker = setupMarkerAndLabel(netTrackerData);
                    gotMarkersAdded = true;
                } else {
                    trackerHolder.marker = null;
                }

                trackerHolder.StatusControlsSet = addTrackerToTable(trackerHolder);
                _trackerHolders.push(trackerHolder);
                addedRow = true;

                SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.NameCtl, netTrackerData.Name);
            } else if (!isMarkerDisplayble(netTrackerData)) {
                hideMarker(trackerHolder);
            } else {
                var oldTs = trackerHolder.NetTrackerData.Ts;
                var newTs = netTrackerData.Ts;

                if (isIncremental) {
                    if (netTrackerData.IncrTest &&
                        oldTs.getTime() > newTs.getTime()) {
                        var oldTsString = getTsString(oldTs);
                        var newTsString = getTsString(newTs);
                        incrDebugMsg +=
                            "Incr update for " + netTrackerData.Name +
                                ", but its time is wrong: old is " + oldTsString +
                                ", new is " + newTsString + "\r\n";
                    }
                    else if (!netTrackerData.IncrTest &&
                        oldTs.getTime() != newTs.getTime()) {
                        var oldTsString = getTsString(oldTs);
                        var newTsString = getTsString(newTs);
                        incrDebugMsg += netTrackerData.Name +
                            " should not be updated in incr.update, but times are wrong: old is " +
                            oldTsString + ", new is " + newTsString + "\r\n";
                    }
                }

                // at this point it's known that new netTrackerData is displayable (see checks above),
                // check if it wasn't and that's actually new marker on the map:
                if (!isMarkerDisplayble(trackerHolder.NetTrackerData)) {
                    gotMarkersAdded = true;
                }

                updateMarkerPosition(trackerHolder, netTrackerData);
            }

            if (trackerHolder.trackData.track != null &&
                isHavingCoordinates(trackerHolder.NetTrackerData)) { // doesn't matter if it's shown or not - we add the point anyway
                var trackData = trackerHolder.trackData;
                var track = trackData.track;
                if (track.length > 0) {
                    log('Checking track for ' + trackerHolder.Name);
                    var receivedTime = netTrackerData.Ts.getTime();
                    var trackTime = track[0].Ts.getTime();

                    if (receivedTime > trackTime) {
                        log('Updating track for ' + trackerHolder.Name + ': netTrackerData.PrevTs is ' + netTrackerData.PrevTs
                            + ', track[0].Ts is ' + track[0].Ts);
                        var currPoint = new Object();
                        currPoint.Lat = netTrackerData.Lat;
                        currPoint.Lon = netTrackerData.Lon;
                        currPoint.Ts = netTrackerData.Ts;
                        currPoint.Age = netTrackerData.Age;
                        track.splice(0, 0, currPoint);
                        trackPointsToAdd = 1;
                        if (netTrackerData.PrevTs != null &&
                            netTrackerData.PrevTs.getTime() == trackTime) {
                            log('Simply added a point to the track for ' + trackerHolder.Name);
                            trackData.latestReceivedTs = netTrackerData.Ts;
                        } else {
                            log('Refresh need for the whole track for ' + trackerHolder.Name);
                            // we will show track to the new point, but some points are probably missing 
                            // and we need to get them. So we DON'T update trackData.latestReceivedTs
                            trackData.needGetTrack = true;

                            if (netTrackerData.PrevLat != null && netTrackerData.PrevLat != 0) {
                                var prevPoint = new Object();
                                prevPoint.Lat = netTrackerData.PrevLat;
                                prevPoint.Lon = netTrackerData.PrevLon;
                                prevPoint.Ts = netTrackerData.PrevTs;
                                prevPoint.Age = netTrackerData.PrevAge;
                                track.splice(1, 0, prevPoint);
                                trackPointsToAdd++; // should be 2 after that
                            }
                        }

                        var logStr = '';
                        for (var iFoo = 0; iFoo < trackData.track.length; iFoo++) {
                            logStr += '... ' + trackData.track[iFoo].Ts + ": " + trackData.track[iFoo].Lat + '<br />';
                        }
                        log(logStr);
                    }

                    removeOldPoints(trackData.track);
                }
            }

            trackerHolder.UpdateTs = new Date();
            trackerHolder.NetTrackerData = netTrackerData;

            if (isStartTsChanged)
                trackPointsToAdd = -1; // refresh the whole line

            syncTrackLine(trackerHolder, trackPointsToAdd);
        }
        if (addedRow) {
            sortTable();
        }
    }

    // If not incremental, now remove trackers that are not in the response:
    if ((!isIncremental || result.IncrSurr) && _trackerHolders != null) {
        for (var iTrackerHolder = _trackerHolders.length - 1; iTrackerHolder >= 0; iTrackerHolder--) {
            var found = false;

            if (result.Trackers != null) {
                for (var iResult = 0; iResult < result.Trackers.length; iResult++) {
                    if (_trackerHolders[iTrackerHolder].Name == result.Trackers[iResult].Name) {
                        found = true;
                        break;
                    }
                }
            }

            if (!found) {
                if (isIncremental) {
                    incrDebugMsg += "Incremental, but " + _trackerHolders[iTrackerHolder].Name +
                        " is absent in the update.\r\n";
                }

                hideMarker(_trackerHolders[iTrackerHolder]);

                $(_trackerHolders[iTrackerHolder].StatusControlsSet.Row).remove();
                _trackerHolders.splice(iTrackerHolder, 1);
            }
        }
    }

    if (incrDebugMsg != "") {
        try {
            incrDebugMsg =
                "Group id: " + _groupId + "\r\n" +
                    "Call id: " + result.CallId + "\r\n" +
                    "isIncremental: " + isIncremental.toString() + "\r\n" +
                    "_currentSeed: " + _currentSeed + "\r\n" +
                    "src: " + result.Src + "\r\n" +
                    "res: " + result.Res + "\r\n" +
                    "IncrSurr: " + result.IncrSurr + "\r\n" +
                    incrDebugMsg;

            log(incrDebugMsg);

            FlyTrace.Service.TrackerService.TestCheck(incrDebugMsg);
        }
        catch (debugMsgExc) {
            log(debugMsgExc.message);
        }
    }


    updateTrackersDisplay();

    if (!_reloaded) {
        // first data retrieve or got new marker(s) on the map. Do not "fit all" if marker(s) were added 
        // it already did "auto fit".
        if (_succCallTime == null || (gotMarkersAdded && !_autoFitHappened)) {
            _shouldAutoFitOnTimer = true;

            if (gotMarkersAdded)
                _autoFitHappened = true;
        }
    }

    if (result.Res == null || result == '') {
        _currentSeed = null;
    } else if (result.Res != "NIL") {
        _currentSeed = result.Res;
    }
}

function sortTable() {
    var $tbody = $('#listTable tbody');
    var rows = $('#listTable tbody').find('tr');

    rows.sort(function (a, b) {
        if (a.trackerHolder == null &&
            b.trackerHolder == null)
            return 0;

        if (a.trackerHolder == null)
            return -1;

        if (b.trackerHolder == null)
            return 1;

        var order = 1;
        var aVal = a.trackerHolder.Name.toUpperCase();
        var bVal = b.trackerHolder.Name.toUpperCase();

        if (aVal < bVal)
            return -1 * order;

        if (aVal == bVal)
            return 0;

        return 1 * order;
    });

    $.each(rows, function (index, row) {
        $tbody.append(row);
    });
}

var _shouldAutoFitOnTimer = false;

var _autoFitHappened = false;

function hideMarker(trackerHolder) {
    hideTrackForHolder(trackerHolder);

    if (trackerHolder.LineToPrev != null) {
        trackerHolder.LineToPrev.setMap(null);
        trackerHolder.LineToPrev = null;
    }

    var marker = trackerHolder.marker;
    if (marker != null) {
        if (marker.label != null) {
            marker.label.setMap(null);
            marker.label = null;
        }

        if (_isInfoWindowOpen && _infoWindowOpenMarker == marker) {
            closeInfoWindow();
        }

        marker.setMap(null);
        trackerHolder.marker = null;
    }
}

function updateMarkerPosition(trackerHolder, newNetTrackerData) {
    var oldNetTrackerData = trackerHolder.NetTrackerData;

    var isOldDisplayed = isMarkerDisplayble(oldNetTrackerData);

    if (isOldDisplayed &&
        oldNetTrackerData.Ts.getTime() == newNetTrackerData.Ts.getTime()) {
        return;
    }

    if (!isOldDisplayed) {
        hideMarker(trackerHolder);
        trackerHolder.marker = setupMarkerAndLabel(newNetTrackerData);
    } else {
        var newLatLng = new google.maps.LatLng(newNetTrackerData.Lat, newNetTrackerData.Lon);
        trackerHolder.marker.setPosition(newLatLng);

        trackerHolder.marker.setAnimation(google.maps.Animation.BOUNCE);
        trackerHolder.BouncingStartTime = new Date();
    }

    if (oldNetTrackerData.Type == null ||
        oldNetTrackerData.Type != newNetTrackerData.Type) {
        var imageDescr = getMarkerImageAndShadow(newNetTrackerData);

        trackerHolder.marker.setIcon(imageDescr.image);
        trackerHolder.marker.setShadow(imageDescr.shadow);
    }
}

function fixLineToPrev(trackerHolder) {
    var pathToPrev = null;

    var netTrackerData = trackerHolder.NetTrackerData;

    var isOkToShow2ndPoint =
        _startTs == null ||
            netTrackerData.PrevTs.getTime() >= _startTs.getTime();

    if (isOkToShow2ndPoint &&
        netTrackerData.PrevLat != null &&
        netTrackerData.PrevLat != 0) {
        var prevLat = netTrackerData.PrevLat;
        var prevLon = netTrackerData.PrevLon;

        if (netTrackerData.Lat == prevLat && netTrackerData.Lon == prevLon) { // This situation has been seen. So add a tiny bit to the track to make it visible at least in max zoom.
            prevLat = prevLat - 0.000003;
        }

        pathToPrev =
            new google.maps.MVCArray(
                [
                    new google.maps.LatLng(netTrackerData.Lat, netTrackerData.Lon),
                    new google.maps.LatLng(prevLat, prevLon)
                ]
            );
    }

    if (pathToPrev == null || !isMarkerDisplayble(netTrackerData)) {
        if (trackerHolder.LineToPrev != null) {
            trackerHolder.LineToPrev.setMap(null);
            trackerHolder.LineToPrev = null;
            log(trackerHolder.Name + ': hiding the newest leg');
        }
    } else {
        if (trackerHolder.LineToPrev != null) {
            var existingPath = trackerHolder.LineToPrev.getPath();
            if (existingPath == null ||
                existingPath.getLength() != 2 || !existingPath.getAt(0).equals(pathToPrev.getAt(0)) || !existingPath.getAt(1).equals(pathToPrev.getAt(1))) {
                log(trackerHolder.Name + ': setting the newest leg line');
                trackerHolder.LineToPrev.setPath(pathToPrev);
            } else {
                log(trackerHolder.Name + ': no update to the newest leg required');
            }
        } else {
            log(trackerHolder.Name + ': creating the newest leg line');
            trackerHolder.LineToPrev =
                new google.maps.Polyline({
                    path: pathToPrev,
                    strokeColor: "#00FF00",
                    strokeOpacity: 1,
                    strokeWeight: 2,
                    zIndex: 0
                });
            trackerHolder.LineToPrev.setMap(_map);
        }
    }
}

function getContent(marker) {
    if (marker == null) return "";

    try {
        if (marker == _currentLocationMarker) {
            return "<img src=App_Themes/Default/kolobok_cool.gif>&nbsp;&nbsp;This is your position";
        }

        var trackerHolder = null;

        var content = "";

        for (var iTrackerHolder = 0; iTrackerHolder < _trackerHolders.length; iTrackerHolder++) {
            if (_trackerHolders[iTrackerHolder].marker == marker) {
                trackerHolder = _trackerHolders[iTrackerHolder];
                break;
            }
        }

        if (trackerHolder != null) {
            var iconSource;
            if (isTrackType(trackerHolder.NetTrackerData)) {
                iconSource = "App_Themes/Default/info.png";
            } else if (isOkType(trackerHolder.NetTrackerData)) {
                iconSource = "App_Themes/Default/finish.png";
            } else if (isCustomType(trackerHolder.NetTrackerData)) {
                iconSource = "App_Themes/Default/finish-custom.png";
            } else {
                // help & sos
                iconSource = "App_Themes/Default/help.png";
            }
            var ageStr = getAgeStr(trackerHolder.UpdateTs, trackerHolder.NetTrackerData.Age);

            var errAddOn = "";
            if (trackerHolder.NetTrackerData.Error != null) {
                errAddOn = trackerHolder.NetTrackerData.Error + "<br />";
            }

            var coordsStr = getDegrees(trackerHolder.NetTrackerData.Lat, trackerHolder.NetTrackerData.Lon, true);
            var coordsStrFlat = getDegrees(trackerHolder.NetTrackerData.Lat, trackerHolder.NetTrackerData.Lon, false);

            var ts = trackerHolder.NetTrackerData.Ts;

            var tsString =
                ts.getDate() + " " + getMonthStr(ts.getMonth()) + " " + ts.getHours() + ":" + padInteger(ts.getMinutes(), 2);

            coordsTableStr =
                "<table style=\"width: 100%; font-family: 'courier New' , Courier, monospace;\"><tr><td>"
                    + coordsStr + "</td><td style=\"text-align: center;\">" +
                    "<a href='javascript: copyToClipboard(\"" + escape(coordsStrFlat) + "\");'>Copy<br />coords</a>" +
                    "</td></tr></table>";

            var statusStr =
                "<a href='javascript: centerAndZoom(\"" + trackerHolder.Name + "\");'>" + getDisplayType(trackerHolder.NetTrackerData) + "</a>";

            var formattedUserMessage = formatUserMessage(trackerHolder.NetTrackerData);
            if (formattedUserMessage != "") {
                formattedUserMessage = "<div class='infoUsrMsg'>" + formattedUserMessage + "</div>";
            }

            content =
                "<div style='margin-bottom: 0.5em'><img align='left' style='margin-right: 0.5em' src=" + iconSource + "> " +
                    tsString + " (" + ageStr + " ago)<br />" +
                    "<span style='font-size: larger; white-space:nowrap'><b>" + trackerHolder.Name + ": " + statusStr + "</b></span></div>" +
                    formattedUserMessage +
                    coordsTableStr +
                    errAddOn +
                    getTrackControlStr(trackerHolder);
        }

        return content;
    } catch (exc) {
        return exc.message;
    }
}

function getTsString(ts) {
    return ts.getDate() + " " + getMonthStr(ts.getMonth()) + " " + ts.getHours() + ":" + padInteger(ts.getMinutes(), 2);
}

function getTrackControlStr(trackerHolder) {
    var trackStr;
    var trackData = trackerHolder.trackData;
    if (!trackData.shouldDisplayTrack) {
        trackStr = "<a href='javascript: showTrack(\"" + trackerHolder.Name + "\");'>Show full track</a>";
    } else if (trackData.shouldDisplayTrack && trackData.needGetTrack) {
        trackStr = "Getting full track... (<a href='javascript: hideTrack(\"" + trackerHolder.Name + "\");'>cancel</a>)";
    } else {
        trackStr = "<a href='javascript: hideTrack(\"" + trackerHolder.Name + "\");'>Hide full track</a>";
    }

    return trackStr;
}

function hideTrack(name) {
    try {
        var trackerHolder = FindTrackerHolder(name);
        if (trackerHolder != null) {
            hideTrackForHolder(trackerHolder);
        }
        syncAllTracksButton();
    } catch (exc) {
        alert(exc.message);
    }
}

function hideAllTracks() {
    try {
        for (var iTrackerHolder = 0; iTrackerHolder < _trackerHolders.length; iTrackerHolder++) {
            var trackerHolder = _trackerHolders[iTrackerHolder];
            hideTrackForHolder(trackerHolder);
        }
        syncAllTracksButton();
    } catch (exc) {
        alert(exc.message);
    }
}

function hideTrackForHolder(trackerHolder) {
    trackerHolder.trackData.shouldDisplayTrack = false;

    if (_isInfoWindowOpen && _infoWindowOpenMarker == trackerHolder.marker) {
        _infoWindow.setContent(getContent(trackerHolder.marker));
    }

    syncTrackLine(trackerHolder, 0);
    showAge(trackerHolder);
}

function showTrack(name) {
    if (!_tracksEnabled) {
        alert('Tracks disabled for this group.');
        return;
    }

    try {
        var trackerHolder = FindTrackerHolder(name);
        if (trackerHolder != null) {
            showTrackForHolder(trackerHolder);
        }
        checkFullTracks(true);
        syncAllTracksButton();
    } catch (exc) {
        alert(exc.message);
    }
}

var _showAllTracksPressed = false;

function showAllTracks() {
    if (!_tracksEnabled) {
        alert('Tracks disabled for this group.');
        return;
    }

    try {
        _showAllTracksPressed = true;
        for (var iTrackerHolder = 0; iTrackerHolder < _trackerHolders.length; iTrackerHolder++) {
            var trackerHolder = _trackerHolders[iTrackerHolder];
            showTrackForHolder(trackerHolder);
        }
        checkFullTracks(true);

        syncAllTracksButton();
    } catch (exc) {
        alert(exc.message);
    }
}

function setCookie(c_name, c_value) {
    var exdate = new Date();
    exdate.setDate(exdate.getDate() + 30 * 4);
    document.cookie = c_name + '=' + escape(c_value) + ';expires=' + exdate.toUTCString();
}

function getCookie(c_name) {
    var i, x, y, ARRcookies = document.cookie.split(";");
    for (i = 0; i < ARRcookies.length; i++) {
        x = ARRcookies[i].substr(0, ARRcookies[i].indexOf("="));
        y = ARRcookies[i].substr(ARRcookies[i].indexOf("=") + 1);
        x = x.replace(/^\s+|\s+$/g, "");
        if (x == c_name) {
            return unescape(y);
        }
    }
}

var _numAllTracksAlertShown = 0;
var _maxTimesToShowAllTracksAlert = 1;

function syncAllTracksButton() {
    var nTracksToDisplay = 0;
    var nNotRequestedTracks = 0;
    var nVisibleTracks = 0;

    if (_trackerHolders != null) {
        for (var iTrackerHolder = 0; iTrackerHolder < _trackerHolders.length; iTrackerHolder++) {
            var trackerHolder = _trackerHolders[iTrackerHolder];
            var trackData = trackerHolder.trackData;

            // Use only trackers that are on the map. For others wait for curr coords to be retrieved, otherwise this call
            // would be just a wasted network call.
            if (isMarkerDisplayble(trackerHolder.NetTrackerData)) {
                if (trackData.shouldDisplayTrack) {
                    nTracksToDisplay++;
                    if (!trackData.needGetTrack)
                        nVisibleTracks++;
                }
                else { // if !trackData.shouldDisplayTrack
                    nNotRequestedTracks++;
                }

            }
        }
    }

    var buttonEnabled = true;
    if (nTracksToDisplay == 0 && nNotRequestedTracks == 0) {
        _allTracksRequested = false;
        buttonEnabled = false;
    } else if (nTracksToDisplay != 0 && nNotRequestedTracks != 0) {
        _allTracksRequested = false;
    } else {
        _allTracksRequested = (nNotRequestedTracks == 0);
    }

    if (_allTracksRequested) {
        if (nTracksToDisplay == nVisibleTracks) {
            _showTracksButton.setText("Hide tracks");
            if (_showAllTracksPressed &&
                _numAllTracksAlertShown < _maxTimesToShowAllTracksAlert) {
                setAlertToShowOnNextTimer('All available tracks retrieved. Note that full tracks can be shown & hidden individually. Click on a marker for that.');
                _numAllTracksAlertShown++;
                setCookie('numAllTracksAlertShown', _numAllTracksAlertShown.toString());
                _showAllTracksPressed = false;
            }
        } else {
            _showTracksButton.setText("Getting tracks...");
        }
    } else {
        _showTracksButton.setText("Show tracks");
    }

    _showTracksButton.setEnabled(buttonEnabled && _tracksEnabled);
}

function showTrackForHolder(trackerHolder) {
    if (!isMarkerDisplayble(trackerHolder.NetTrackerData))
        return;

    var trackData = trackerHolder.trackData;
    trackData.shouldDisplayTrack = true;

    if (trackData.track == null ||
        trackData.track.length == 0 ||
        trackData.track[0].Ts.getTime() != trackerHolder.NetTrackerData.Ts.getTime()) {
        trackData.needGetTrack = true;
        trackData.isFirstPriority = true;
        trackData.requestTs = new Date();
    } else {
        syncTrackLine(trackerHolder, -1);
    }

    if (_isInfoWindowOpen && _infoWindowOpenMarker == trackerHolder.marker) {
        _infoWindow.setContent(getContent(trackerHolder.marker));
    }

    showAge(trackerHolder);
}

var _getTracksCallStartTs = null;

function checkFullTracks(firstPriorityOnly) {
    if (_getTracksCallStartTs != null) {
        var d = new Date();
        var secondsPassed = Math.round((d.getTime() - _getTracksCallStartTs.getTime()) / 1000);

        if (secondsPassed < 30) return;
    }

    try {
        var indexesToCheck = new Array();
        for (var iTrackerHolder = 0; iTrackerHolder < _trackerHolders.length; iTrackerHolder++) {
            var trackerHolder = _trackerHolders[iTrackerHolder];
            var trackData = trackerHolder.trackData;

            // Use only trackers that are on the map. For others wait for curr coords to be retrieved, otherwise this call
            // would be just a wasted network call.
            if (isMarkerDisplayble(trackerHolder.NetTrackerData) &&
                trackData.shouldDisplayTrack &&
                trackData.needGetTrack &&
                (trackData.isFirstPriority || !firstPriorityOnly)) {
                trackData.isFirstPriority = false; // a tracker has only one attempt to be treated as a first priority.
                indexesToCheck.push(iTrackerHolder);
            }
        }

        if (indexesToCheck.length == 0) return;

        indexesToCheck.sort(function (i1, i2) {
            var requestTs1 = _trackerHolders[i1].trackData.requestTs;
            var requestTs2 = _trackerHolders[i2].trackData.requestTs;

            return requestTs1.getTime() - requestTs2.getTime();
        });

        var request = new Object();
        request.Items = new Array();

        for (var iIndex = 0; iIndex < indexesToCheck.length; iIndex++) {
            var iTrackerHolder = indexesToCheck[iIndex];
            var trackerHolder = _trackerHolders[iTrackerHolder];

            var reqItem = new Object();
            reqItem.TrackerName = trackerHolder.Name;
            if (trackerHolder.trackData.latestReceivedTs != null)
                reqItem.LaterThan = trackerHolder.trackData.latestReceivedTs;

            request.Items.push(reqItem);

            if (request.Items.length >= 10) break;
        }

        if (request.Items.length > 0) {
            _getTracksCallStartTs = new Date();
            FlyTrace.Service.TrackerService.GetTracks(_groupId, request, _getTracksCallStartTs, onGetTracksComplete, onError);
            showStatus("Getting track(s) from the server...");
        }
    }
    catch (e) {
        showError(e.message);
    }
}

function log(msg) {
    if (_shouldLog) {
        var logElement = document.getElementById('logDiv');
        var d = new Date();
        var tsString = d.getHours() + ":" + padInteger(d.getMinutes(), 2);
        logElement.innerHTML += "<br />" + tsString + ": " + msg.toString();
    }
}

function onGetTracksComplete(result) {
    try {
        log('onGetTracksComplete');
        _getTracksCallStartTs = null;

        for (var iTrackResponseItem = 0; iTrackResponseItem < result.length; iTrackResponseItem++) {
            var trackResponseItem = result[iTrackResponseItem];
            if (trackResponseItem.Track == null || trackResponseItem.Track.length == 0) continue;

            var newestTrackPoint = trackResponseItem.Track[0];
            var oldestTrackPoint = trackResponseItem.Track[trackResponseItem.Track.length - 1];

            var trackerHolder = FindTrackerHolder(trackResponseItem.TrackerName);

            log('Received for ' + trackerHolder.Name + ': ' + trackResponseItem.Track.length);

            // Ensure that it's a valid tracker that already have marker on the map
            if (trackerHolder != null && isMarkerDisplayble(trackerHolder.NetTrackerData)) {
                var trackData = trackerHolder.trackData;

                if (trackData.track == null) {
                    trackData.track = trackResponseItem.Track;
                } else {
                    log('Track wasn\'t null for ' + trackerHolder.Name + ', had ' + trackData.track.length + ' points, removong old...');
                    log('The oldest point: ' + oldestTrackPoint.Ts);
                    removeNewTrackPoints(trackData.track, oldestTrackPoint.Ts);
                    trackData.track = trackResponseItem.Track.concat(trackData.track);
                    log('Now track for ' + trackerHolder.Name + ' has ' + trackData.track.length + ' points.');
                }

                // remove points that are too old for today track
                removeOldPoints(trackData.track);
                log('And after removing old points track for ' + trackerHolder.Name + ' has ' + trackData.track.length + ' points.');
                log('Marker curr.time is ' + trackerHolder.NetTrackerData.Ts);
                log('Marker prev.time is ' + trackerHolder.NetTrackerData.PrevTs);

                var logStr = '';
                for (var iFoo = 0; iFoo < trackData.track.length; iFoo++) {
                    logStr += '... ' + trackData.track[iFoo].Ts + ": " + trackData.track[iFoo].Lat + '<br />';
                }
                log(logStr);

                var trackNeedUpdate = false;

                if (newestTrackPoint.Ts.getTime() == trackerHolder.NetTrackerData.Ts.getTime()) {
                    log(trackerHolder.Name + ' had corresponding marker and newestTrackPoint.Ts: ' + newestTrackPoint.Ts);
                    trackNeedUpdate = true;
                } else if (newestTrackPoint.Ts.getTime() == trackerHolder.NetTrackerData.PrevTs.getTime()) {
                    log(trackerHolder.Name + ' had corresponding marker PREV point and newestTrackPoint.Ts: ' + newestTrackPoint.Ts);
                    // Almost impossible situation, but let's handle that.
                    // Create a "mock-up" of the TrackPoint object as it would arrive from the server:
                    var currPoint = new Object();
                    currPoint.Lat = trackerHolder.NetTrackerData.Lat;
                    currPoint.Lon = trackerHolder.NetTrackerData.Lon;
                    currPoint.Ts = trackerHolder.NetTrackerData.Ts;
                    currPoint.Age = trackerHolder.NetTrackerData.Age;
                    trackData.track.splice(0, 0, currPoint);

                    trackNeedUpdate = true;
                } else if (newestTrackPoint.Ts > trackerHolder.NetTrackerData.Ts) {
                    log('Full track arrived before update to the marker for ' + trackerHolder.Name);
                    // Possible if the full track arrived before update to the point.
                    // Create a "mock-up" of the Tracker object as it would arrive from the server:
                    var netTrackerData = new Object();
                    netTrackerData.Name = trackerHolder.Name;
                    netTrackerData.Lat = newestTrackPoint.Lat;
                    netTrackerData.Lon = newestTrackPoint.Lon;
                    netTrackerData.Type = newestTrackPoint.Type; // it's always set for the newest point, and always null for all other points
                    netTrackerData.Ts = newestTrackPoint.Ts;
                    netTrackerData.Age = newestTrackPoint.Age;
                    netTrackerData.IsHidden = trackerHolder.NetTrackerData.IsHidden;
                    if (trackData.track.length == 0) {   // TODO: is it possible at all?????
                        netTrackerData.PrevLat = trackerHolder.NetTrackerData.Lat;
                        netTrackerData.PrevLon = trackerHolder.NetTrackerData.Lon;
                        netTrackerData.PrevTs = trackerHolder.NetTrackerData.Ts;
                        netTrackerData.PrevAge = trackerHolder.NetTrackerData.Age;
                    } else {
                        var prevTrackPoint = trackResponseItem.Track[1];
                        netTrackerData.PrevLat = prevTrackPoint.Lat;
                        netTrackerData.PrevLon = prevTrackPoint.Lon;
                        netTrackerData.PrevTs = prevTrackPoint.Ts;
                        netTrackerData.PrevAge = prevTrackPoint.Age;
                    }

                    updateMarkerPosition(trackerHolder, netTrackerData);

                    trackerHolder.UpdateTs = new Date();
                    trackerHolder.NetTrackerData = netTrackerData;

                    trackNeedUpdate = true;
                } else {
                    log(trackerHolder.Name + ' had newestTrackPoint.Ts < trackerHolder.NetTrackerData.Ts');

                    // it means newestTrackPoint.Ts < trackerHolder.NetTrackerData.Ts (or even < PrevTs if latter exists)
                    // This situation is practically impossible. But if it happens, just skip the tracker: the track will be 
                    // retrieved on the next call to checkFullTracks. 
                    // So we DON'T set needGetTrack to false here.
                }

                log(trackerHolder.Name + ': trackNeedUpdate==' + trackNeedUpdate.toString());
                if (trackNeedUpdate) {
                    trackData.needGetTrack = false;
                    trackData.latestReceivedTs = trackerHolder.NetTrackerData.Ts;

                    syncTrackLine(trackerHolder, -1);
                }

            }
        }

        updateTrackersDisplay();

        // if other tracks were requested while we've been on call, make another call for those just requested only:
        checkFullTracks(true);
        syncAllTracksButton();
    }
    catch (exc) {
        showError(exc.message);
    }
}

// First it shows either the full track or just the final line depending on trackerHolder.track flags,
// or hides track/latest line if should not be displayed.
// Then if the full track was shown:
//      If nPointsToAdd==0, then just returns.
//      If nPointsToAdd>0, then it adds the first (nPointsToAdd) points to the track.
//      If nPointsToAdd<0, then the whole polyline is replaced.
function syncTrackLine(trackerHolder, nPointsToAdd) {
    try {
        var trackData = trackerHolder.trackData;

        if (!isMarkerDisplayble(trackerHolder.NetTrackerData) || !trackData.shouldDisplayTrack ||
            trackData.track == null ||
            trackData.track.length < 2) {
            log(trackerHolder.Name + ': track should not be shown, checking the latest line...');

            if (trackerHolder.trackData.trackPolyline != null) {
                trackerHolder.trackData.trackPolyline.setMap(null);
                trackerHolder.trackData.trackPolyline = null;
            }

            fixLineToPrev(trackerHolder); // hides line if needed e.g. if !isMarkerDisplayble

            return;
        }

        log(trackerHolder.Name + ': showing full track');

        if (trackerHolder.LineToPrev != null) {
            trackerHolder.LineToPrev.setMap(null);
            trackerHolder.LineToPrev = null;
        }

        if (trackData.trackPolyline != null && nPointsToAdd < 0) {   // we need to replace the whole polyline, so remove the existing one:
            log(trackerHolder.Name + ': replacing the whole track');
            trackData.trackPolyline.setMap(null);
            trackData.trackPolyline = null;
        }

        if (trackData.trackPolyline == null) {
            nPointsToAdd = trackData.track.length;
        }
        // after two previous "if" nPointsToAdd is >= 0

        if (nPointsToAdd == 0) {   // nothing to do: trackPolyline is already not null (otherwise nPointsToAdd==track.length,
            // and we already checked that latter is greater than 1), which means it's on the map (we 
            // don't keep invisible polylines) and no points has to be added. So just return:
            log(trackerHolder.Name + ': no update to the track required');
            return;
        }

        var polylinePoints;
        if (trackData.trackPolyline != null) {
            polylinePoints = trackData.trackPolyline.getPath();
        } else {
            polylinePoints = new google.maps.MVCArray();
        }

        for (var iPoint = 0; iPoint < nPointsToAdd; iPoint++) {
            var trackPoint = trackData.track[iPoint];

            if (_startTs != null &&
                trackPoint.Ts.getTime() < _startTs.getTime())
                break;

            var latLng = new google.maps.LatLng(trackPoint.Lat, trackPoint.Lon);
            polylinePoints.insertAt(iPoint, latLng);
        }

        log("nPointsToAdd: " + nPointsToAdd);

        if (trackData.trackPolyline == null) {
            trackData.trackPolyline =
                new google.maps.Polyline({
                    path: polylinePoints,
                    strokeColor: trackData.color,
                    strokeOpacity: 0.5,
                    strokeWeight: 4,
                    zIndex: 0
                });
            trackData.trackPolyline.setMap(_map);
        }
    }
    catch (exc) {
        log(trackerHolder.Name + ': error: ' + exc.message);
        throw exc;
    }
}

function removeOldPoints(trackPointsArray) {
    if (trackPointsArray == null || trackPointsArray.length == 0) return;

    var newestTrackPoint = trackPointsArray[0];

    // remove points that are too old, this would work for very long 
    var oldPointThreshold = new Date();
    oldPointThreshold.setTime(newestTrackPoint.Ts.getTime() - 12 * 3600 * 1000);
    log('oldPointThreshold: ' + oldPointThreshold);

    // array is already sorted by Ts descending
    var iRemoveStart = -1;
    for (var i = 0; i < trackPointsArray.length; i++) {
        if (trackPointsArray[i].Ts < oldPointThreshold) {
            iRemoveStart = i;
            break;
        }
    }

    if (iRemoveStart >= 0) {
        trackPointsArray.splice(iRemoveStart, trackPointsArray.length - iRemoveStart);
    }
}

// remove track points that are the same age as eldestTsToRemove or newer.
function removeNewTrackPoints(trackPointsArray, eldestTsToRemove) {
    // array is already sorted by Ts descending
    var numToRemove = 0;
    while (numToRemove < trackPointsArray.length) {
        if (trackPointsArray[numToRemove].Ts < eldestTsToRemove) {
            break;
        }
        numToRemove++;
    }

    if (numToRemove > 0) {
        trackPointsArray.splice(0, numToRemove);
    }
}

function getMonthStr(month) {
    switch (month) {
        case 0:
            return "Jan";
        case 1:
            return "Feb";
        case 2:
            return "Mar";
        case 3:
            return "Apr";
        case 4:
            return "May";
        case 5:
            return "Jun";
        case 6:
            return "Jul";
        case 7:
            return "Aug";
        case 8:
            return "Sep";
        case 9:
            return "Oct";
        case 10:
            return "Nov";
        case 11:
            return "Dec";
    }

    return month;
}

function centerAndZoom(name) {
    try {
        var trackerHolder = FindTrackerHolder(name);
        if (trackerHolder != null) {
            if (_isInfoWindowOpen && _infoWindowOpenMarker == trackerHolder.marker) {
                closeInfoWindow();
            }

            _map.setZoom(17);
            _map.setCenter(trackerHolder.marker.getPosition());
        }
    } catch (exc) {
        alert(exc.message);
    }
}

function closeInfoWindow() {
    if (_isInfoWindowOpen) {
        _infoWindow.close();
        _isInfoWindowOpen = false;
    }
}

function formatUserMessage(netTrackerData) {
    if (netTrackerData.UsrMsg != null && netTrackerData.UsrMsg != "")
        return "&quot;<i>" + netTrackerData.UsrMsg + "</i>&quot;";
    else
        return "";
}

function getDisplayType(netTrackerData) {
    if (isTrackType(netTrackerData)) {
        return "ON TRACK";
    }

    if (netTrackerData.Type == "OK" || netTrackerData.Type == "TEST") {
        if (netTrackerData.UsrMsg != null && netTrackerData.UsrMsg != "")
            return "OK";
        else
            return "LANDED (Ok)";
    }

    if (netTrackerData.Type == "CUSTOM") {
        if (netTrackerData.UsrMsg != null && netTrackerData.UsrMsg != "")
            return "CUSTOM";
        else
            return "LANDED (custom)";
    }

    return netTrackerData.Type;
}

//function getDisplayType(netTrackerData)
//{
//    var usrMsg = "";
//    if (netTrackerData.UsrMsg != null && netTrackerData.UsrMsg != "")
//    {
//        usrMsg = ": &quot;<i>" + netTrackerData.UsrMsg + "</i>&quot;";
//    }

//    if (isTrackType(netTrackerData))
//    {
//        return "ON TRACK";
//    }

//    if (netTrackerData.Type == "OK" || netTrackerData.Type == "TEST")
//    {
//        return "OK" + usrMsg;
//    }

//    if (netTrackerData.Type == "CUSTOM")
//    {
//        return "CUST";
//    }

//    return netTrackerData.Type + usrMsg;
//}

var _trackerHolders;

function updateTrackersDisplay() {
    if (_trackerHolders == null) return;

    var mapHasHiddenTrackers = false;

    for (var iTrackerHolder = 0; iTrackerHolder < _trackerHolders.length; iTrackerHolder++) {
        var trackerHolder = _trackerHolders[iTrackerHolder];

        var name = trackerHolder.NetTrackerData.Name;
        if (trackerHolder.NetTrackerData.IsHidden) {
            SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.NameCtl, name + "&nbsp;&ndash;&nbsp;<span class='InfoMessage'>hidden&nbsp;*</span>");
            mapHasHiddenTrackers = true;

            trackerHolder.StatusControlsSet.Row.style.color = "#707070";
        }
        else {
            SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.NameCtl, name);
            trackerHolder.StatusControlsSet.Row.style.color = "";
        }

        if (trackerHolder.NetTrackerData.Type == null) {
            SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.CoordsCtl, "");
            SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.StatusCtl, "None");
        } else if (isWaitType(trackerHolder.NetTrackerData)) {
            SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.CoordsCtl, "");
            SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.StatusCtl, "Waiting for the data...");
        } else {
            var coordStr = getDegrees(trackerHolder.NetTrackerData.Lat, trackerHolder.NetTrackerData.Lon, true);
            SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.CoordsCtl, coordStr);

            var displayType = getDisplayType(trackerHolder.NetTrackerData);

            var formattedUserMessage = formatUserMessage(trackerHolder.NetTrackerData);
            if (formattedUserMessage != "") {
                formattedUserMessage = ": " + formattedUserMessage;
            }

            SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.StatusCtl, displayType + formattedUserMessage);
        }

        showAge(trackerHolder);

        // Error could be null, and it's Ok to call SetStatusControlInnerHtml with that:
        SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.ErrorCtl, trackerHolder.NetTrackerData.Error);

        if (_isInfoWindowOpen && _infoWindowOpenMarker == trackerHolder.marker) {
            _infoWindow.setContent(getContent(trackerHolder.marker));
        }
    }

    if (mapHasHiddenTrackers)
        $('#hiddenHint').show();
    else
        $('#hiddenHint').hide();
}

function showAge(trackerHolder) {
    var ageStr;
    var utcTsAndTrack;

    if (isHavingCoordinates(trackerHolder.NetTrackerData)) {
        ageStr = getAgeStr(trackerHolder.UpdateTs, trackerHolder.NetTrackerData.Age);

        if (trackerHolder.marker != null) {
            var newTitle = trackerHolder.Name + ": " + ageStr + " ago";
            if (trackerHolder.marker.getTitle() != newTitle)
                trackerHolder.marker.setTitle(newTitle);
        }

        utcTsAndTrack = dateFormat(trackerHolder.NetTrackerData.Ts, "default", false);

        if (isMarkerDisplayble(trackerHolder.NetTrackerData))
            utcTsAndTrack += "<br />" + getTrackControlStr(trackerHolder);
    }

    SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.AgeCtl, ageStr);
    SetStatusControlInnerHtml(trackerHolder.StatusControlsSet.TimestampCtl, utcTsAndTrack);
}

function copyToClipboard(text) {
    var d = document.createElement("div");
    d.innerHTML = unescape(text);

    window.prompt("Copy that manually:", d.innerText);
}

function getDegrees(lat, lon, breakLine) {
    var res;
    if (_coordFormat == 'deg') {
        res = lat.toString();

        if (breakLine)
            res = res + "<br />";
        else
            res = res + ",&nbsp;";

        res = res + lon.toString();
    } else {
        var latAbs = lat;
        var latPrefix;
        if (latAbs < 0)
            latPrefix = "S";
        else
            latPrefix = "N";
        latAbs = Math.abs(latAbs);

        var lonAbs = lon;
        var lonPrefix;
        if (lonAbs < 0)
            lonPrefix = "W";
        else
            lonPrefix = "E";
        lonAbs = Math.abs(lonAbs);

        res = latPrefix + '&nbsp;' + transformCoords(latAbs, 2);
        if (breakLine)
            res = res + "<br />";
        else
            res = res + "&nbsp;";

        res = res + lonPrefix + transformCoords(lonAbs, 3);
    }

    return res;

    function transformCoords(coord, degLength) {
        var res;

        var deg = Math.floor(coord);

        var min = ((coord - deg) * 60);

        var minFloor = Math.floor(min);

        if (_coordFormat == 'degminsec') {
            var sec = ((min - minFloor) * 60);
            var secFloor = Math.floor(sec);
            var secFraction = Math.round((sec - secFloor) * 10);
            res = padInteger(deg, degLength) + '&deg;' + padInteger(minFloor, 2) + '&prime;' + padInteger(secFloor, 2) + '.' + secFraction.toString() + '&quot;';
        } else {
            var minFraction = Math.round((min - minFloor) * 1000);
            res = padInteger(deg, degLength) + '&deg;' + padInteger(minFloor, 2) + '.' + padInteger(minFraction, 3) + '&prime;';
        }

        return res;
    }
}

function padInteger(n, len) {
    var s = n.toString();
    if (s.length < len) {
        s = ('0000000000' + s).slice(-len);
    }
    return s;
}

function SetStatusControlInnerHtml(ctrl, innerHtml2Set) {
    if (ctrl != null) {
        if (innerHtml2Set == null) {
            ctrl.innerHTML = null;
        } else if (ctrl.innerHTML == null || ctrl.innerHTML != innerHtml2Set) {
            ctrl.innerHTML = innerHtml2Set;
        }
    }
}

function FindTrackerHolder(trackerName) {
    if (_trackerHolders == null) return null;

    for (var iTrackerHolder = 0; iTrackerHolder < _trackerHolders.length; iTrackerHolder++) {
        if (_trackerHolders[iTrackerHolder].Name == trackerName) {
            return _trackerHolders[iTrackerHolder];
        }
    }

    return null;
}

function fitAllTrackers(showAlertForNoCoords) {
    try {
        if (_trackerHolders == null || _trackerHolders.length == 0) {
            if (showAlertForNoCoords) {
                alert("There are no pilots assigned to the group.");
            }
            return;
        }

        var bounds = null;

        for (var iTrackerHolder = 0; iTrackerHolder < _trackerHolders.length; iTrackerHolder++) {
            var trackerHolder = _trackerHolders[iTrackerHolder];
            if (isHavingCoordinates(trackerHolder.NetTrackerData) &&
                trackerHolder.marker != null &&
                trackerHolder.NetTrackerData.Lat > -999 &&
                trackerHolder.NetTrackerData.Lon > -999) {
                if (bounds == null) {
                    bounds = new google.maps.LatLngBounds(trackerHolder.marker.getPosition());
                } else {
                    bounds = bounds.extend(trackerHolder.marker.getPosition());
                }
            }
        }

        if (bounds != null) {
            _map.fitBounds(bounds);
        } else if (showAlertForNoCoords) {
            alert("No coordinates received yet for pilots in the group, see Pilot List for details.");
        }
    }
    catch (exc) {
        alert(exc.message);
    }
}

function togglePanels() {
    if ($('#mapPanel').is(':visible')) {
        $('#mapPanel').hide();
        $('#listPanel').show();
    } else {
        $('#listPanel').hide();
        $('#mapPanel').show();
    }

    positionContent();
}

function addTrackerToTable(trackerHolder) {
    var $tbody = $('#listTable tbody');
    var templateRow = $tbody.find('tr').first()[0];
    var newRow = templateRow.cloneNode(true);

    var statusControlsSet = new Object();
    statusControlsSet.NameCtl = findElementByClass(newRow, "TrackerNameCtl");
    statusControlsSet.StatusCtl = findElementByClass(newRow, "TrackerStatusCtl");
    statusControlsSet.CoordsCtl = findElementByClass(newRow, "TrackerCoordsCtl");
    statusControlsSet.AgeCtl = findElementByClass(newRow, "TrackerAgeCtl");
    statusControlsSet.TimestampCtl = findElementByClass(newRow, "TrackerTimestampCtl");
    statusControlsSet.ErrorCtl = findElementByClass(newRow, "TrackerErrorCtl");
    statusControlsSet.Row = newRow;

    $tbody.append(newRow);
    newRow.style.display = "";
    newRow.trackerHolder = trackerHolder;

    return statusControlsSet;
}


function findElementByClass(node, lookupClass) {
    for (var iChildNode = 0; iChildNode < node.childNodes.length; iChildNode++) {
        var childNode = node.childNodes[iChildNode];

        if (childNode.className == lookupClass) {
            return childNode;
        }

        var result = findElementByClass(childNode, lookupClass);
        if (result != null) {
            return result;
        }
    }

    return null;
}

function sendLog() {
    var logElement = document.getElementById('logDiv');
    FlyTrace.Service.TrackerService.TestCheck(logElement.innerHTML);
}

function positionContent() {
    $header = $('#header');
    var headerHeight = $header.height();
    $('#content').css({
        top: headerHeight
    });

    if ($('#mapPanel').is(':visible')) {
        var mapPanelHeight = $(window).height() - headerHeight;
        mapPanelHeight = Math.max(mapPanelHeight, 200);
        $('#mapPanel').height(mapPanelHeight);
    }
}

var _coordFormat = 'degmin';

function changeCoordsFormat(value) {
    _coordFormat = value;
    setCookie('flytrace_coord_format', value);
    updateTrackersDisplay();
    closeInfoWindow();

    setPreFormatLink();
}

$(document).ready(function () {
});

// Window load event used just in case window height is dependant upon images
$(window).bind("load", function () {
    positionContent();
    $(window)
        .scroll(positionContent)
        .resize(positionContent)
});
