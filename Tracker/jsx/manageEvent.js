var WaypointsTable = React.createClass({
    render: function() {
        return (
          <div>
            hello react
        </div>
    );
}
});

$( document ).ready(function() {
    ReactDOM.render(
        <WaypointsTable />,
        document.getElementById('waypoints123')
    );
});
