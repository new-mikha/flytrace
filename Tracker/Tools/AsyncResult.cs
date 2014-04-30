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

/******************************************************************************
Module:  AsyncResult.cs
Notices: Written by Jeffrey Richter
******************************************************************************/

using System;


///////////////////////////////////////////////////////////////////////////////


internal class AsyncResult<TResult> : AsyncResultNoResult
{
  // Field set when operation completes
  private TResult m_result = default( TResult );

  public AsyncResult( AsyncCallback asyncCallback , Object state )
    : base( asyncCallback , state )
  {
  }

  public void SetAsCompleted( TResult result , Boolean completedSynchronously )
  {
    // Save the asynchronous operation's result
    m_result = result;

    // Tell the base class that the operation completed sucessfully (no exception)
    base.SetAsCompleted( null , completedSynchronously );
  }

  new public TResult EndInvoke( )
  {
    base.EndInvoke( ); // Wait until operation has completed 
    return m_result;  // Return the result (if above didn't throw)
  }
}


//////////////////////////////// End of File //////////////////////////////////