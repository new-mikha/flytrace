using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using FlyTrace.Service;
using FlyTrace.Tools;
using FlyTrace.TrackerDataSetTableAdapters;
using log4net;

namespace FlyTrace.Controllers
{
  public class WaypointsController : ApiController
  {
    private readonly ILog _infoLog = LogManager.GetLogger("InfoLog");


    /// <summary>
    /// If a user might change a page's underlying data after it's loaded (as it happens on this page), then 
    /// when the page is loaded from the cache it should re-request the data through ajax calls - because the
    /// cached data is not the one that the user has entered and then saved (through ajax put/post/etc calls)
    /// using the page controls. See also Remarks section for details about non-cached page alternatives.
    /// </summary>
    /// <remarks>
    /// As an alternative, a page might be returned with "no cache" headers (diff.browsers have different 
    /// headers for that). But it doesn't work well with PostBack's. In this scenario a browser (most
    /// typically IE although Chrome too sometimes) shows an error instead of the page content:
    /// - open the page 
    /// - do a PostBack, e.g.press a button on the page. Now it's a post-backed page, with hidden POST values.
    /// - navigate to another page by clicking a link.
    /// - navigate back - error pops up "the page is out of date", and manual page reload is required then.
    /// If the page is cacheable, then browser restores the page is it was. And it's now JS job to get the right
    /// data from the server.
    /// </remarks>
    /// <param name="id">event id</param>
    /// <returns>All data required to refresh the page changeable state</returns>
    public WaypointsProvider.WaypointsBundle Get(int id) // GET api/<controller>/5
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

      _infoLog.Info($"Hiding old points for event {eventId} with {hoursThreshold} hrs threshold, i.e. from {utcThreshold} UTC == {utcThreshold.ToLocalTime()} local time == {Utils.ToAdminTimezone(utcThreshold)} admin time");

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
      _infoLog.Info($"Restoring old points for event {eventId}");

      TrackerDataSetTableAdapters.EventTableAdapter adapter = new TrackerDataSetTableAdapters.EventTableAdapter();
      adapter.UpdateEventStartTs(eventId, null);

      Service.ServiceFacade.ResetGroupsDefCache();
    }
  }
}
