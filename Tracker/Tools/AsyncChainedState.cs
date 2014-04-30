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
using System.Web;
using System.Threading;
using System.Diagnostics;

internal class AsyncChainedState<TResult>
{
  public readonly AsyncResult<TResult> FinalAsyncResult;

  public AsyncChainedState( AsyncCallback callback , Object state )
  {
    FinalAsyncResult = new AsyncResult<TResult>( callback , state );
  }

  /// <summary>Keeps number of operations completed asynchrounously. If it's greater than 1 then the final result 
  /// has CompletedSynchronously=false</summary>
  private int asyncCompletedCount;

  public bool CompletedSynchronously
  {
    get
    {
      return Thread.VolatileRead( ref asyncCompletedCount ) == 0;
    }
  }

  internal void CheckSynchronousFlag( bool somethingCompletedSynchronously )
  {
    Interlocked.Add( ref asyncCompletedCount , somethingCompletedSynchronously ? 0 : 1 );
  }

  public void SetAsCompleted( TResult result )
  {
    FinalAsyncResult.SetAsCompleted( result , CompletedSynchronously );
  }

  public void SetAsCompleted( Exception exception )
  {
    FinalAsyncResult.SetAsCompleted( exception , CompletedSynchronously );
  }
}