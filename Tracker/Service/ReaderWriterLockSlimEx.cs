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
using System.Threading;

namespace FlyTrace.Service
{
  /// <summary>
  /// Inherited from ReaderWriterLockSlim, just adds few helper methods
  /// </summary>
  public class ReaderWriterLockSlimEx : ReaderWriterLockSlim
  {
    private readonly int waitTimeout;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="waitTimeout">Timeout in milliseconds to wait for *WithFiniteWaiting methods</param>
    public ReaderWriterLockSlimEx( int waitTimeout )
    {
      if ( waitTimeout <= 0 )
        throw new ArgumentException( "Value should be positive", "waitTimeout" );

      this.waitTimeout = waitTimeout;
    }

    /// <summary>Do not throw a TimeoutException, works as base EnterReadLock but use std timeout</summary>
    public bool TryEnterWriteLock( )
    {
      return TryEnterWriteLock( waitTimeout );
    }

    /// <summary>Tries to enter, throws a TimeoutException if std timeout expires</summary>
    public void AttemptEnterReadLock( )
    {
      if ( !TryEnterReadLock( waitTimeout ) )
        throw new TimeoutException( "Can't enter read mode: timeout has expired" );
    }

    /// <summary>Tries to enter, throws a TimeoutException if std timeout expires</summary>
    public void AttemptEnterWriteLock( )
    {
      if ( !TryEnterWriteLock( waitTimeout ) )
        throw new TimeoutException( "Can't enter write mode: timeout has expired" );
    }

    /// <summary>Tries to enter, throws a TimeoutException if std timeout expires</summary>
    public void AttemptEnterUpgradeableReadLock( )
    {
      if ( !TryEnterUpgradeableReadLock( waitTimeout ) )
        throw new TimeoutException( "Can't enter upgradeable mode: timeout has expired" );
    }
  }
}