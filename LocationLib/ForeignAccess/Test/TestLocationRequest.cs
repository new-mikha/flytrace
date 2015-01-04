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

using FlyTrace.LocationLib.ForeignAccess.Spot;

namespace FlyTrace.LocationLib.ForeignAccess.Test
{
  public class TestLocationRequest : LocationRequest
  {
    private readonly SpotFeedRequest spotFeedRequest;

    public TestLocationRequest( RequestParams requestParams, string testXml )
      : base( requestParams )
    {
      this.spotFeedRequest = new SpotFeedRequest( requestParams.Id, testXml, -1 );
    }

    public override string ForeignType
    {
      get { return ForeignId.TEST; }
    }

    public override IAsyncResult BeginReadLocation( AsyncCallback callback, object state )
    {
      return this.spotFeedRequest.BeginRequest( callback, state );
    }

    protected override TrackerState EndReadLocationProtected( IAsyncResult ar )
    {
      return this.spotFeedRequest.EndRequest( ar );
    }

    public override void SafelyAbortRequest( AbortStat abortStat )
    {
      this.spotFeedRequest.SafelyCloseRequest( );
    }
  }
}