using System;
using System.Collections.Generic;
using System.Linq;

using FlyTrace.TrackerDataSetTableAdapters;

namespace FlyTrace.Tools
{
  public class WaypointsProvider
  {
    /// <summary>
    /// Definition of a waypoint in the event. Each event waypoint can be referenced 
    /// by id in <see cref="TaskWaypoint"/>, e.g. many task waypoints
    /// can reference single event waypoint.
    /// </summary>
    public class EventWaypoint
    {
      public int Id;

      public string Name;
    }

    /// <summary>
    /// Reference to the event waypoint in the actual task, i.e. "waypoint instance", 
    /// where id is a _reference_ to the event waypoint definition
    /// in <see cref="EventWaypoint"/>
    /// </summary>
    public class TaskWaypoint
    {
      public int Id;

      public int Radius;
    }


    public class WaypointsBundle
    {
      public TaskWaypoint[] TaskWaypoints;

      public EventWaypoint[] EventWaypoints;

      public long? StartTsMilliseconds;
    }

    public WaypointsBundle GetWaypointsBundle(int eventId)
    {
      var result = new WaypointsBundle
      {
        EventWaypoints = GetEventWaypoints(eventId).ToArray(),
        TaskWaypoints = GetTaskWaypoints(eventId).ToArray()
      };

      var eventTableAdapter = new EventTableAdapter();
      var eventDataTable = eventTableAdapter.GetDataByEventId(eventId);
      if (eventDataTable.Rows.Count > 0 && !eventDataTable[0].IsStartTsNull())
      {
        DateTime utcEpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime utcStartTs = eventDataTable[0].StartTs;

        TimeSpan timeSpan = new TimeSpan(utcStartTs.Ticks - utcEpochStart.Ticks);

        result.StartTsMilliseconds = (long)timeSpan.TotalMilliseconds;
      }

      return result;
    }

    private IEnumerable<EventWaypoint> GetEventWaypoints(int eventId)
    {
      WaypointTableAdapter waypointsAdapter =
        new WaypointTableAdapter();

      TrackerDataSet.WaypointDataTable eventWaypoints =
        waypointsAdapter.GetDataByEventId(eventId);

      return
        eventWaypoints
        .Select(
          row =>
            new EventWaypoint { Id = row.Id, Name = row.Name }
        );
    }

    private IEnumerable<TaskWaypoint> GetTaskWaypoints(int eventId)
    {
      var taskAdapter = new TaskTableAdapter();
      TrackerDataSet.TaskDataTable orderedTaskWaypoints = taskAdapter.GetOrderedWaypointsByEventId(eventId);

      return
        orderedTaskWaypoints
        .Select(
          row =>
            new TaskWaypoint { Id = row.WaypointId, Radius = row.Radius }
        );
    }
  }
}
