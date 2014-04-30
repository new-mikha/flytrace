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
Module:  AsyncResultNoResult.cs
Notices: Written by Jeffrey Richter (modified for flytrace.com project)
******************************************************************************/

using System;
using System.Threading;
using System.Diagnostics;


///////////////////////////////////////////////////////////////////////////////


public class AsyncResultNoResult : IAsyncResult
{
  // Fields set at construction which never change while operation is pending
  private readonly AsyncCallback m_AsyncCallback;
  private readonly Object m_AsyncState;

  // Field set at construction which do change after operation completes
  private const Int32 c_StatePending = 0;
  private const Int32 c_StateCompletedSynchronously = 1;
  private const Int32 c_StateCompletedAsynchronously = 2;
  private Int32 m_CompletedState = c_StatePending;

  // Field that may or may not get set depending on usage
  private ManualResetEvent m_AsyncWaitHandle;

  // Fields set when operation completes
  private Exception m_exception;

  public AsyncResultNoResult( AsyncCallback asyncCallback , Object state )
  {
    m_AsyncCallback = asyncCallback;
    m_AsyncState = state;
  }

  public void SetAsCompleted( Exception exception , Boolean completedSynchronously )
  {
    // Passing null for exception means no error occurred; this is the common case
    m_exception = exception;

    // The m_CompletedState field MUST be set prior calling the callback
    Int32 prevState = Interlocked.Exchange( ref m_CompletedState ,
       completedSynchronously ? c_StateCompletedSynchronously : c_StateCompletedAsynchronously );
    if ( prevState != c_StatePending )
      throw new InvalidOperationException( "You can set a result only once" );

    // If the event exists, set it
    if ( m_AsyncWaitHandle != null ) m_AsyncWaitHandle.Set( );

    // If a callback method was set, call it
    if ( m_AsyncCallback != null ) m_AsyncCallback( this );
  }

  public static int DefaultEndWaitTimeout = -1;

  public int EndWaitTimeout = DefaultEndWaitTimeout;

  public void EndInvoke( )
  {
    // This method assumes that only 1 thread calls EndInvoke for this object
    if ( !IsCompleted )
    {
      if ( EndWaitTimeout < 0 )
      {
        AsyncWaitHandle.WaitOne( );
      }
      else
      {
        // If the operation isn't done, wait for it
        if ( !AsyncWaitHandle.WaitOne( EndWaitTimeout , false ) )
        {
          throw new TimeoutException( );
        }
      }
      AsyncWaitHandle.Close( );
      m_AsyncWaitHandle = null;  // Allow early GC
    }

    // Operation is done: if an exception occured, throw it
    if ( m_exception != null ) throw m_exception;
  }

  #region Implementation of IAsyncResult
  public Object AsyncState
  {
    get
    {
      return m_AsyncState;
    }
  }

  public Boolean CompletedSynchronously
  {
    get
    {
      return m_CompletedState == c_StateCompletedSynchronously;
    }
  }

  public WaitHandle AsyncWaitHandle
  {
    get
    {
      if ( m_AsyncWaitHandle == null )
      {
        Boolean done = IsCompleted;
        ManualResetEvent mre = new ManualResetEvent( done );
        if ( Interlocked.CompareExchange( ref m_AsyncWaitHandle , mre , null ) != null )
        {
          // Another thread created this object's event; dispose the event we just created
          mre.Close( );
        }
        else
        {
          if ( !done && IsCompleted )
          {
            // If the operation wasn't done when we created 
            // the event but now it is done, set the event
            m_AsyncWaitHandle.Set( );
          }
        }
      }
      return m_AsyncWaitHandle;
    }
  }

  public Boolean IsCompleted
  {
    get
    {
      // MK: added Thread.VolatileRead
      return Thread.VolatileRead( ref m_CompletedState ) != c_StatePending;
    }
  }
  #endregion
}


//////////////////////////////// End of File //////////////////////////////////