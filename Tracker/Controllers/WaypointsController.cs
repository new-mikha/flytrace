using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FlyTrace.Controllers
{
  public class WaypointsController : ApiController
  {
    public class TaskWaypoint
    {
      // ReSharper disable once InconsistentNaming
      public int id;

      // ReSharper disable once InconsistentNaming
      public int radius;
    }

    public void Put(int id, [FromBody] IEnumerable<TaskWaypoint> task)
    {
      var taskAdapter = new TrackerDataSetTableAdapters.TaskTableAdapter();
      TrackerDataSet.TaskDataTable taskTable = taskAdapter.GetDataByEventId(id);

      foreach (DataRow row in taskTable.Rows)
      {
        row.Delete();
      }

      if (task != null)
      {
        int iWp = 0;
        foreach (TaskWaypoint waypoint in task)
        {
          taskTable.AddTaskRow(waypoint.id, waypoint.radius, iWp++);
        }
      }

      taskAdapter.Update(taskTable);
    }

    [HttpPost]
    [Route("api/Waypoints/HideOldPoints/{eventId:int}/{hoursThreshold:int}")]
    public long HideOldPoints(int eventId, int hoursThreshold)
    {
      DateTime utcThreshold = DateTime.UtcNow.AddHours(-hoursThreshold);

      TrackerDataSetTableAdapters.EventTableAdapter adapter = new TrackerDataSetTableAdapters.EventTableAdapter();
      adapter.UpdateEventStartTs(eventId, utcThreshold);

      Service.ServiceFacade.ResetGroupsDefCache();

      DateTime epochStart = new DateTime(1970, 1, 1);
      TimeSpan ts = new TimeSpan(utcThreshold.Ticks - epochStart.Ticks);

      return (long)ts.TotalMilliseconds;
    }

    [HttpPost]
    [Route("api/Waypoints/RestoreOldPoints/{eventId:int}")]
    public void RestoreOldPoints(int eventId)
    {
      TrackerDataSetTableAdapters.EventTableAdapter adapter = new TrackerDataSetTableAdapters.EventTableAdapter();
      adapter.UpdateEventStartTs(eventId, null);

      Service.ServiceFacade.ResetGroupsDefCache();
    }
  }
}
