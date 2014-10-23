/******************************************************************************
 * Flytrace, online viewer for GPS trackers.
 * Copyright (C) 2011-2014 Mikhail Karmazin
 * 
 * This file is part of Flytrace.
 * 
 * Flytrace is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * Flytrace is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
 *****************************************************************************/

using System;

using FlyTrace.LocationLib;
using log4net;

namespace FlyTrace.Service
{
  /// <summary>
  /// See Remarks section for details.
  /// </summary>
  /// <remarks>
  /// This class solves thread-safety issue for collections like Dictionary that require locking of the collection if there is
  /// a chance of being updated during the read (see MSDN for Dictionary or List, thread-safety section). Such locks could be 
  /// time-consuming for Tracker instances which should be read as fast as possible in client requests. If a collection holds 
  /// just instances of this class, it still requires locking when it's being updated and thus when it's being read. But once a 
  /// holder is read from the collection, it doesn't require a lock to get a value of the Snapshot field.
  /// 
  /// As a result, less time needs to be spent on locking parent collection. It should be locked for time-consuming changes 
  /// (adding a new value) only once per tracker, not once per a change (which would require also locks for reads)
  /// 
  /// And that's the reaspo why it's class, not struct (note that structs are value types which wouldn't show to a reader 
  /// of Snapshot updates to it from another thread. Read MSDN if you don't see why)
  /// </remarks>
  public class TrackerStateHolder
  {
    public TrackerStateHolder( ForeignId foreignId )
    {
      ForeignId = foreignId;
    }

    public readonly ForeignId ForeignId;

    /// <summary>The value of this field (i.e. a reference to <see cref="RevisedTrackerState"/> class) could be changed at 
    /// any time by different threads. So to access members of the referenced instance, either put it under the same lock 
    /// as the one that writes to that property, or first read it into a local variable then work with the local variable. 
    /// Note that it's NOT volatile, so make volatile reads to prevent reordering.
    /// </summary>
    public RevisedTrackerState Snapshot;

    /// <summary>
    /// Not null when the request is in progress for the tracker, null otherwise. 
    /// NOT volatile. Accessed from multiple threads.
    /// </summary>
    public LocationLib.ForeignAccess.LocationRequest CurrentRequest;

    /// <summary>Time when the tracker was requested for the 1st time</summary>
    public readonly DateTime AddedTime = TimeService.Now;

    public DateTime? ScheduledTime;

    /// <summary>Keeps time of the start of the latest LocationRequest used to request data for this
    /// tracker, even for just refresh. Also might be not-null while Snapshot is null yet.
    /// Notice that <see cref="RefreshTime"/> keeps time of the _end_ of the same request, while this
    /// one keeps the _start_ time.</summary>
    public DateTime? RequestStartTime;

    /// <summary>UTC time of the latest refresh from the foreign server.
    /// NOT volatile. Accessed from multiple threads. Null if CurrentRequest is null.
    /// </summary>
    public DateTime? RefreshTime;

    private static readonly ILog Log = LogManager.GetLogger( typeof( TrackerStateHolder ) );

    /// <summary>
    /// Checks that values of fields in the holder are consistent with each other. 
    /// Logs error without throwing (see details in the comment inside the method)
    /// </summary>
    /// <returns>Returns error message for debug purposes or null if all is good.</returns>
    public string CheckTimesConsistency( )
    {
      try
      {
        // return just one message, but log all encountered - see if's sequence
        string message = null;

        // 1. Inconsistency should just never happen.
        // 2. If yet it happens and an error is thrown, then there is a chance that same tracker
        //    will be attempted to schedule over and over again, disturbing other trackers in the 
        //    queue.
        // So in the highly unlikely event of inconsistency, just log error, which should go into
        // SMTP appender, and hope that it will recover.
        if ( RequestStartTime.HasValue &&
             RequestStartTime.Value < AddedTime )
        {
          message = "Start before add for " + ForeignId;
          Log.ErrorFormat( message );
        }

        if ( RefreshTime.HasValue )
        {
          if ( RefreshTime.Value < AddedTime )
          {
            message = "End before add for " + ForeignId;
            Log.ErrorFormat( message );
          }

          if ( Snapshot == null )
          {
            message = "Refresh time without snapshot for " + ForeignId;
            Log.ErrorFormat( message );
          }
        }

        if ( ScheduledTime.HasValue )
        {
          if ( ScheduledTime.Value < AddedTime )
          {
            message = "Scheduled before add " + ForeignId;
            Log.Error( message );
          }

          if ( RequestStartTime.HasValue )
          {
            message = "Scheduled and RequestStartTime both have value for " + ForeignId;
            Log.Error( message );
          }
        }

        if ( RefreshTime.HasValue &&
            !RequestStartTime.HasValue )
        {
          message = "End without start for " + ForeignId;
          Log.Error( message );
        }

        if ( CurrentRequest == null &&
             RequestStartTime.HasValue &&
             (
                RefreshTime == null ||
                RequestStartTime.Value > RefreshTime.Value
             )
          )
        {
          message = "Request start without actual request for " + ForeignId;
          Log.Error( message );
        }

        return message;
      }
      catch ( Exception exc )
      {
        Log.Error( "CheckTimesConsistency", exc );
        return exc.Message;
      }
    }

    /// <summary>UTC time of the latest access. Used for diag only. 
    /// Writing to that can be done OUT OF LOCK, see its usage.</summary>
    public long ThreadDesynchronizedAccessTimestamp = TimeService.Now.ToFileTime( );

    public override string ToString( )
    {
      return ForeignId.ToString( );
    }
  }
}