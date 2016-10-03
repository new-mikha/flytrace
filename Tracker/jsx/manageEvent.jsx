function getParameterByName(name) {
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(window.location.href);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function showAjaxError(message, err, xhr) {
    if (err)
        message += '\r\n' + err.toString();

    if (xhr &&
        xhr.responseJSON &&
        xhr.responseJSON.ExceptionMessage)
        message += '\r\n' + xhr.responseJSON.ExceptionMessage;

    alert(message);
}


var React = require('react');
var ReactDOM = require('react-dom');
var ReactCSSTransitionGroup = require('react-addons-css-transition-group');

//noinspection JSUnresolvedVariable
var ErrorLines = React.createClass({
    render: function () {
        var divs = [(<div key={0}>Error occurred, try to reload page</div>)];

        var nextLineStyle = {marginLeft: '1em'};

        if (this.props.err)
            divs.push(<div key={divs.length} style={nextLineStyle}>{this.props.err.toString()}</div>);

        var xhr = this.props.xhr;
        if (xhr &&
            xhr.responseJSON &&
            xhr.responseJSON.ExceptionMessage)
            divs.push(<div key={divs.length} style={nextLineStyle}>{xhr.responseJSON.ExceptionMessage}</div>);

        return (<div>{divs}</div>);
    }
});

var RadiusBox = React.createClass({
    getInitialState: function () {
        return {radius: this.props.radius};
    },

    handleChange: function (e) {
        let value = e.target.value;
        const re = /^\d*$/;
        if (!re.test(value))
            return;

        this.setState({radius: value});

        if (this.props.onChange)
            this.props.onChange(value);
    },

    render: function () {
        if (this.props.hidden)
            return null;

        return (
            <input
                style={{width: this.props.width}}
                type="text"
                placeholder="Enter number"
                value={this.state.radius}
                ref="radiusInput"
                onChange={this.handleChange}
            />
        );
    }
});

var WaypointRow = React.createClass({
    render: function () {
        let taskWpt = this.props.taskWpt;
        let isNew = taskWpt.id === undefined;

        // only New row has an empty line in the options:
        let selectOptions;
        let selectedValue;
        let controlsStyle = {display: isNew ? 'none' : ''};
        if (isNew) {
            selectOptions = [
                (<option key={-1} value={-1}/>),
                ...this.props.selectOptions];
            selectedValue = -1;
        } else {
            selectOptions = this.props.selectOptions;
            selectedValue = taskWpt.id;
        }

        return (
            <tr>
                <td>{this.props.label}</td>
                <td>
                    <select defaultValue={selectedValue}
                            ref="waypointBox"
                            onChange={this.handleWaypointChange}>
                        {selectOptions}
                    </select>
                </td>
                <td>
                    <RadiusBox hidden={isNew}
                               radius={taskWpt.radius}
                               onChange={this.handleRadiusChange}
                    />
                    <input style={controlsStyle} type="button"
                           value="Remove"
                           onClick={this.handleRemoveClick}/>
                </td>
            </tr>
        );
    },

    handleRemoveClick: function () {
        this.props.onRemove(this.props.index);
    },

    handleWaypointChange: function () {
        this.props.taskWpt.id = this.refs.waypointBox.value;

        if (this.props.onChange)
            this.props.onChange(this.props.index);
    },

    handleRadiusChange: function (newRadius) {
        this.props.taskWpt.radius = newRadius;

        if (this.props.onChange)
            this.props.onChange(this.props.index);
    },

    checkNumeric: function (e) {
        //console.log(e.target.value);
        e.preventDefault();
    },
});

var SaveButton = React.createClass({
    getInitialState: function () {
        return {isSaving: false};
    },

    clearSaveFlags: function () {
        this.setState({isSaving: false, isSaved: false, isError: false});
    },

    render: function () {
        if (this.state.isSaving) {
            return (<div><input type="button" disabled={true} value="Saving, please wait..."/></div>);
        }

        let saveInfo = '';
        if (this.state.isSaved)
            saveInfo = 'saved OK';
        else if (this.state.isError)
            saveInfo = 'error occurred on save';

        return (
            <div>
                <input type="button" value='Save' onClick={this.saveTask}/>
                &nbsp;&nbsp;{saveInfo}
            </div>
        );
    },

    saveTask: function () {
        let result = [];
        $.each(this.props.task, function (index, taskWpt) {
            if (taskWpt.id) {
                result.push({
                    id: taskWpt.id,
                    radius: taskWpt.radius
                });
            }
        }.bind(this));

        let eventId = getParameterByName('event');

        this.setState({isSaving: true});

        $.ajax({
            dataType: 'json',
            url: _baseUrl + 'api/Waypoints/' + eventId,
            method: 'PUT',
            data: JSON.stringify(result),
            contentType: 'application/json',
            success: function () {
                this.setState({isSaving: false, isSaved: true, isError: false});
            }.bind(this),
            error: function (xhr, status, err) {
                this.setState({isSaving: false, isSaved: false, isError: true});
                showAjaxError('Error occurred while saving', err, xhr);
            }.bind(this)
        });
    },


});

var AutoFillControls = React.createClass({
    componentWillMount: function () {
        this.defRadius = 400;
    },

    getInitialState: function () {
        return {collapsed: true};
    },

    render: function () {
        var items = [];

        if (!this.state.collapsed) {
            items.push(
                <div key={'fillAllButtons'}>
                    <div style={{marginTop: '0.5em', whiteSpace: 'nowrap'}}>
                        <input type="button"
                               onClick={this.addAllSortedByName}
                               value="Use all waypoints, sorted by name"/>
                        &nbsp;&nbsp;Def radius:&nbsp;
                        <RadiusBox width={'5em'}
                                   radius={this.defRadius}
                                   onChange={this.handleDefRadiusChange}
                        />

                    </div>
                    <div style={{marginTop: '0.5em'}}>
                        <input type="button"
                               onClick={this.addAllSortedById}
                               value="Use all waypoints, sorted by order they've been added"/>
                    </div>
                </div>
            );
        }

        return (
            <div>
                <input type="button" onClick={this.clearTask} value="Clear task"/>
                <a style={{marginLeft: '1em'}} href='#' onClick={this.toggleCollapsibleArea}>
                    {this.state.collapsed ? 'Show bulk update controls' : 'Hide bulk update controls'}</a>
                <ReactCSSTransitionGroup
                    transitionName="vert-collapsable"
                    transitionEnterTimeout={300}
                    transitionLeaveTimeout={200}
                >
                    {items}
                </ReactCSSTransitionGroup>
            </div>
        );
    },

    handleDefRadiusChange: function (value) {
        this.defRadius = value;
    },

    toggleCollapsibleArea: function (e) {
        e.preventDefault();
        this.setState({collapsed: !this.state.collapsed});
    },

    clearTask: function () {
        let len = this.props.task.length;
        if (len > 2) {
            this.props.task.splice(1, len - 2);
            this.props.onUpdate();
        }
    },

    addAllSorted: function (primer) {
        { // remove old waypoints first, apart from "new" placeholder:
            let len = this.props.task.length;
            if (len > 1)
                this.props.task.splice(0, len - 1);
        }

        let defRadius = parseInt(this.defRadius);
        if (isNaN(defRadius) || defRadius <= 0) {
            defRadius = 400;
        }

        let allWaypointsCopy = this.props.allWaypoints.concat();
        allWaypointsCopy.sort(primer);

        $.each(allWaypointsCopy, function (index, wpt) {
            this.props.task.splice(index, 0, {
                key: this.props.wptGen.gen++,
                id: wpt.id,
                radius: defRadius
            });
        }.bind(this));

        this.props.onUpdate();
    },

    addAllSortedByName: function () {
        this.addAllSorted(function (x, y) {
            return x.name.localeCompare(y.name);
        });
    },

    addAllSortedById: function () {
        this.addAllSorted(function (x, y) {
            return x.id - y.id;
        });
    },
});

var WaypointsTable = React.createClass({
    componentWillMount: function () {
        this.selectOptions = [];
        $.each(this.props.allWaypoints, function (index, wpt) {
            this.selectOptions.push(
                <option key={wpt.id}
                        value={wpt.id}>
                    {wpt.name}
                </option>);
        }.bind(this));

        this.wptGen = {gen: 0};

        this.keyedTaskWaypoints = [];
        $.each(this.props.taskWaypoints, function (index, taskWpt) {
            this.keyedTaskWaypoints.push({
                key: this.wptGen.gen++,
                id: taskWpt.id,
                radius: taskWpt.radius
            });
        }.bind(this));

        this.addNewWaypointPlaceholder();
    },

    addNewWaypointPlaceholder: function () {
        this.keyedTaskWaypoints.push({key: this.wptGen.gen++, radius: 400});
    },

    render: function () {
        var rows = [];
        $.each(this.keyedTaskWaypoints, function (index, taskWpt) {
            let label;
            if (index === (this.keyedTaskWaypoints.length - 1)) {
                label = index === 0 ? 'Set start first' : 'Add new ->';
            } else if (index === 0) {
                label = 'Start';
            } else {
                label = index;
            }

            rows.push(
                <WaypointRow key={taskWpt.key}
                             label={label}
                             taskWpt={taskWpt}
                             selectOptions={this.selectOptions}
                             index={index}
                             onRemove={this.removeByIndex}
                             onChange={this.checkChangedWaypoint}
                />
            );
        }.bind(this));

        return (
            <div>
                <AutoFillControls
                    task={this.keyedTaskWaypoints}
                    allWaypoints={this.props.allWaypoints}
                    wptGen={this.wptGen}
                    onUpdate={this.updatedByAutoFill}/>
                <table>
                    <thead>
                    <tr>
                        <th>Point #</th>
                        <th>Turn Point</th>
                        <th>Radius (meters)</th>
                    </tr>
                    </thead>
                    <tbody>
                    {rows}
                    </tbody>
                </table>
                <SaveButton ref="saveButton" task={this.keyedTaskWaypoints}/>
            </div>
        );
    },

    removeByIndex: function (rowIndex) {
        this.keyedTaskWaypoints.splice(rowIndex, 1);
        this.forceUpdate();
        this.refs.saveButton.clearSaveFlags();
    },

    checkChangedWaypoint: function (rowIndex) {
        if (rowIndex === (this.keyedTaskWaypoints.length - 1)) {
            this.addNewWaypointPlaceholder();
            this.forceUpdate();
        }
        this.refs.saveButton.clearSaveFlags();
    },

    updatedByAutoFill: function () {
        this.forceUpdate();
        this.refs.saveButton.clearSaveFlags();
    }
});

var RemoveOldPointsButton = React.createClass({
    render: function () {
        let buttonText;
        if (!this.props.hours) {
            buttonText = 'Hide points older than now';
            $('#react-remove-old-points-button-text').html(buttonText.toUpperCase());
        } else if (this.props.hours === 1) {
            buttonText = "Hide points older than NOW minus 1 hour";
        } else {
            buttonText = "Hide points older than NOW minus " + this.props.hours + " hours";
        }

        return (
            <input type="button"
                   value={buttonText}
                   onClick={this.hideOldPoints}/>
        );
    },

    hideOldPoints: function (e) {
        let confirmationMsg;
        if (!this.props.hours)
            confirmationMsg = 'Are you sure you want to hide all points older than NOW?';
        else if (this.props.hours === 1)
            confirmationMsg = 'Are you sure you want to hide all points older than NOW minus 1 hour?';
        else
            confirmationMsg = 'Are you sure you want to hide all points older than NOW minus ' +
                this.props.hours + ' hours?';

        if (!confirm(confirmationMsg))
            return;

        let eventId = getParameterByName('event');

        this.props.onClicked();

        $.ajax({
            dataType: 'json',
            url: _baseUrl + 'api/Waypoints/HideOldPoints/' + eventId + '/' + (this.props.hours ? this.props.hours : 0),
            method: 'POST',
            success: function (data) {
                this.props.onDone(data);
            }.bind(this),
            error: function (xhr, status, err) {
                this.props.onError();
                showAjaxError('Error occurred while trying to hide old points', err, xhr);
            }.bind(this)
        });
    }
});

var OldTaskStatus = React.createClass({
    componentWillMount: function () {
        this.timer =
            setInterval(function () {
                    this.forceUpdate(); // to update rendered "... ago" string
                }.bind(this),
                1000);
    },

    componentWillUnmount: function () {
        clearInterval(this.timer);
    },

    render: function () {
        let date = this.props.startTs ? new Date(this.props.startTs) : null;

        if (!date && !this.props.isUpdating) {
            return null;
        }

        let contents;
        if (!date && this.props.isUpdating) {
            contents = "Updating old points threshold...";
        } else {
            let thresholdAgoString = this.getAgeStr(date) + ' ago'; //  '5 hr 18 min ago';
            let thresholdLocalTimeString = dateFormat(date, 'ddd d mmm H:MM', false) + ' local time'; // 'Sat 1 Oct 17:04 local time';
            let thresholdUtcTimeString = dateFormat(date, 'ddd d mmm H:MM', true) + ' UTC'; // 'Sat 1 Oct 7:04 UTC';

            contents = (
                <span >
                    <b>Assigned maps are hiding the old points now.</b><br />
                    Threshold for "old" is the moment <b>{thresholdAgoString}</b>, i.e.<br />
                    only those points are displayed that are appeared<br />
                    after <b>{thresholdLocalTimeString}</b>,<br />
                    which is <b>{thresholdUtcTimeString}</b>. {this.props.isUpdating ? 'Updating...' : ''}
                </span>
            );
        }

        return (
            <div className="InfoMessage" style={{marginTop: '0.6em', marginBottom: '0.6em'}}>
                {contents}
            </div>
        );
    },

    getAgeStr: function (startTime) {
        let d = new Date();

        let totalSeconds = Math.round((d.getTime() - startTime.getTime()) / 1000);
        let ageTotalMinutes = Math.floor((totalSeconds) / 60);

        let days = Math.floor(ageTotalMinutes / 60 / 24);
        let hours = Math.floor(ageTotalMinutes / 60) % 24;
        let minutes = Math.floor(ageTotalMinutes) % 60;

        let ageStr = minutes + " min";
        if (hours != 0) ageStr = hours + " hr " + ageStr;
        if (days != 0) ageStr = days + " d " + ageStr;

        return ageStr;
    }
});

var RestoreAllPointsButton = React.createClass({
    render: function () {
        return (
            <input type="button"
                   style={{marginTop: '1em'}}
                   value='Restore old points'
                   onClick={this.restoreOldPoints}/>
        );
    },

    restoreOldPoints: function () {
        let okToRestore =
            confirm('Are you sure you want to remove the threshold for old points and show all of them back?');

        if (!okToRestore)
            return;

        this.props.onClicked();

        $.ajax({
            dataType: 'json',
            url: _baseUrl + 'api/Waypoints/RestoreOldPoints/' + getParameterByName('event'),
            method: 'POST',
            success: function () {
                this.props.onRemoved();
            }.bind(this),
            error: function (xhr, status, err) {
                this.props.onError();
                showAjaxError('Error occurred while trying to restore old points', err, xhr);
            }.bind(this)
        });
    }
});

var OldTaskCleanUpControls = React.createClass({
    getInitialState: function () {
        return {collapsed: true, startTs: this.props.initialStartTs, isUpdating: false};
    },

    render: function () {
        let collapsibleItems = [];

        if (!this.state.collapsed) {
            let hourButtons = []
            $.each([1, 2, 3, 5, 24], function (iHour, hour) {
                hourButtons.push(
                    <div key={hour} style={{marginTop: '1em'}}>
                        <RemoveOldPointsButton
                            key={hour}
                            hours={hour}
                            onClicked={this.startUpdating}
                            onDone={this.setStartTs}
                            onError={this.haltUpdating}/>
                    </div>
                );
            }.bind(this));

            collapsibleItems.push(
                <div key={'allButtons'}>
                    {hourButtons}
                </div>
            );
        }

        return (
            <div style={{paddingTop: '0.3em'}}>
                <OldTaskStatus startTs={this.state.startTs} isUpdating={this.state.isUpdating}/>
                <RemoveOldPointsButton
                    onClicked={this.startUpdating}
                    onDone={this.setStartTs}
                    onError={this.haltUpdating}/><br />
                Clicking on this button hides all trackers from the event's assigned maps until new positions are
                received. That can be useful when only new positions make sense for the moment. E.g. it can be clicked
                at the beginning of the competition day - without such clean-up a map can contain points and tracks from
                previous days for a while until the trackers are switched on which could be annoying.
                <br/><br/>
                <a href='#' onClick={this.toggleCollapsibleArea}>
                    {this.state.collapsed ? 'Show additional clean-up controls...' : 'Hide additional clean-up controls'}</a>
                <ReactCSSTransitionGroup
                    transitionName="vert-collapsable"
                    transitionEnterTimeout={300}
                    transitionLeaveTimeout={200}
                >
                    {collapsibleItems}
                </ReactCSSTransitionGroup>
                <br />
                <RestoreAllPointsButton
                    onClicked={this.startUpdating}
                    onRemoved={this.clearStartTs}
                    onError={this.haltUpdating}/><br/>
                Restores all trackers on the map no matter how old their positions are - if a tracker is on its original
                SPOT Shared Page then it will be shown on the event's assigned maps after pressing this button. In other
                words, it cancels earlier click to the "HIDE POINTS OLDER THAN NOW" button above.

            </div>
        );
    },

    startUpdating: function () {
        this.setState({isUpdating: true});
    },

    haltUpdating: function () {
        this.setState({isUpdating: false});
    },

    setStartTs: function (newStartTs) {
        this.setState({startTs: newStartTs, isUpdating: false});
    },

    clearStartTs: function () {
        this.setState({startTs: null, isUpdating: false});
    },


    toggleCollapsibleArea: function (e) {
        e.preventDefault();
        this.setState({collapsed: !this.state.collapsed});
    },
});

var _baseUrl;

if(typeof _ie8_or_less === 'undefined' || !_ie8_or_less) {
    $(document).ready(function () {
        _baseUrl = new RegExp(/^.*\//).exec(window.location.href);

        {
            let waypoints = document.getElementById('react-waypoints');
            if (waypoints) {
                ReactDOM.render(
                    <div className="task">
                        <WaypointsTable allWaypoints={_allWaypoints}
                                        taskWaypoints={_taskWaypoints}
                        />
                    </div>,
                    waypoints
                );
            }
        }

        {
            let old_task_clean_up_controls = document.getElementById('react-old-task-clean-up-controls');
            if (old_task_clean_up_controls) {
                ReactDOM.render(
                    <OldTaskCleanUpControls initialStartTs={_initialStartTs}/>,
                    old_task_clean_up_controls
                );
            }
        }
    });
}