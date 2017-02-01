using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using FlyTrace.Tools;
using FlyTrace.TrackerDataSetTableAdapters;

namespace FlyTrace.Controllers
{
  public class WaypointsController : ApiController
  {


   

    // GET api/<controller>/5
    public WaypointsProvider.WaypointsBundle Get(int id)
    {
      var waypointsProvider = new WaypointsProvider();

      return waypointsProvider.GetWaypointsBundle(id);
    }

    // PUT api/<controller>/5
    public void Put(int id, [FromBody] IEnumerable<WaypointsProvider.TaskWaypoint> task)
    {
      var taskAdapter = new TrackerDataSetTableAdapters.TaskTableAdapter();
      TrackerDataSet.TaskDataTable taskTable = taskAdapter.GetOrderedWaypointsByEventId(id);

      foreach (DataRow row in taskTable.Rows)
      {
        row.Delete();
      }

      if (task != null)
      {
        int iWp = 0;
        foreach (WaypointsProvider.TaskWaypoint waypoint in task)
        {
          taskTable.AddTaskRow(waypoint.Id, waypoint.Radius, iWp++);
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
