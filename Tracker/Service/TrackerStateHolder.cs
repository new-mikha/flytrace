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
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
  internal class TrackerStateHolder
  {
    public TrackerStateHolder( LocationLib.ForeignId foreignId )
    {
      ForeignId = foreignId;
    }

    public readonly LocationLib.ForeignId ForeignId;

    /// <summary>The value of this field (i.e.reference to <see cref="Tracker"/> class) could be changed at 
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

    /// <summary>UTC time of the latest refresh from the foreign server.
    /// NOT volatile. Accessed from multiple threads.
    /// </summary>
    public DateTime RefreshTime;

    /// <summary>UTC time of the latest access</summary>
    public long AccessTimestamp = DateTime.UtcNow.ToFileTime( );

    public override string ToString( )
    {
      return ForeignId.ToString( );
    }
  }
}