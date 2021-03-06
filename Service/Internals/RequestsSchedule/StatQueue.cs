﻿/******************************************************************************
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
using FlyTrace.LocationLib;

namespace FlyTrace.Service.Internals.RequestsSchedule
{
  /// <summary>
  /// Records events and provides statistics on them.
  /// Thread safety: NOT THREAD SAFE.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  internal class EventQueue<T>
  {
    // ReSharper disable once StaticFieldInGenericType (it's just a convinient const, 
    // no problem that each specific type has it)
    public static readonly TimeSpan MaxSpanToKeepInQueue = TimeSpan.FromHours( 1 );

    private readonly Func<T, T, T> aggregator;

    public EventQueue( )
    {
    }

    public EventQueue( Func<T, T, T> aggregator )
    {
      this.aggregator = aggregator;
    }

    public bool HasValues(string category)
    {
      return this.categoriesQueues.ContainsKey(category);
    }

    /// <summary>
    /// Records an event for the given category.
    /// <para>Thread safety: NOT THREAD SAFE.</para>
    /// </summary>
    public void AddEvent( string category, T @event, DateTime time = default(DateTime) )
    {
      if ( time == default( DateTime ) )
        time = TimeService.Now;

      Queue<Tuple<DateTime, T>> queue;
      if ( !this.categoriesQueues.TryGetValue( category, out queue ) )
      {
        queue = new Queue<Tuple<DateTime, T>>( );
        this.categoriesQueues.Add( category, queue );
      }

      queue.Enqueue( new Tuple<DateTime, T>( time, @event ) );

      // events are not kept indefinitely in the queue, there is a time threshold to remove:
      while (
        queue.Any( ) &&
        queue.Peek( ).Item1 < TimeService.Now - MaxSpanToKeepInQueue
        )
      {
        queue.Dequeue( );
      }

      // But overall values cover the whole class lifetime:
      {
        T prevOverallMax;
        if ( !this.categoriesOverallMax.TryGetValue( category, out prevOverallMax ) ||
            comparer.Compare( prevOverallMax, @event ) < 0 )
        {
          this.categoriesOverallMax[category] = @event;
        }
      }

      {
        T prevOverallMin;
        if ( !this.categoriesOverallMin.TryGetValue( category, out prevOverallMin ) ||
            comparer.Compare( prevOverallMin, @event ) > 0 )
        {
          this.categoriesOverallMin[category] = @event;
        }
      }

      {
        int count;
        this.categoriesOverallCounts.TryGetValue( category, out count );
        this.categoriesOverallCounts[category] = count + 1;
      }

      if ( this.aggregator != null )
      {
        T agg;
        if ( !this.categoriesOverallAgg.TryGetValue( category, out agg ) )
        {
          agg = @event;
        }
        else
        {
          agg = this.aggregator( agg, @event );
        }

        this.categoriesOverallAgg[category] = agg;
      }
    }

    /// <summary>
    /// Returns the number of events recorded by <see cref="AddEvent"/> for the given category
    /// during time interval before Now.
    /// <para>Thread safety: NOT THREAD SAFE.</para>
    /// </summary>
    public double GetEventsPerMinute( string category, TimeSpan reportSpan )
    {
      if ( reportSpan > MaxSpanToKeepInQueue )
      {
        int overallCount;
        this.categoriesOverallCounts.TryGetValue( category, out overallCount );

        // use overall timespan because there is no data for longer than MaxSpanToKeepInQueue,
        // apart from the overall data:
        return overallCount / ( TimeService.Now - this.startTime ).TotalMinutes;
      }

      Queue<Tuple<DateTime, T>> queue;
      if ( !this.categoriesQueues.TryGetValue( category, out queue ) )
        return 0.0;

      if ( reportSpan == TimeSpan.Zero )
        return 0.0;

      DateTime threshold = TimeService.Now - reportSpan;

      return
        queue.Count( tuple => tuple.Item1 > threshold )
        /
        reportSpan.TotalMinutes;
    }

    /// <summary>
    /// Returns the maximums value from the events recorded by <see cref="AddEvent"/> for the given category
    /// during the time interval before Now.
    /// <para>Thread safety: NOT THREAD SAFE.</para>
    /// </summary>
    public T GetMaximum( string category, TimeSpan reportSpan )
    {
      if ( reportSpan > MaxSpanToKeepInQueue )
      {
        T overallMax;
        this.categoriesOverallMax.TryGetValue( category, out overallMax );

        return overallMax;
      }

      Queue<Tuple<DateTime, T>> queue;
      if ( !this.categoriesQueues.TryGetValue( category, out queue ) )
        return default( T );

      if ( reportSpan == TimeSpan.Zero )
        return default( T );

      DateTime threshold = TimeService.Now - reportSpan;

      T result = default( T );

      foreach ( Tuple<DateTime, T> tuple in
        queue.Where( tuple => tuple.Item1 > threshold ) )
      {
        if ( Equals( result, default( T ) ) ||
             comparer.Compare( result, tuple.Item2 ) < 0 )
          result = tuple.Item2;
      }

      return result;
    }

    /// <summary>
    /// Returns the minumum value from the events recorded by <see cref="AddEvent"/> for the given category
    /// during the time interval before Now.
    /// <para>Thread safety: NOT THREAD SAFE.</para>
    /// </summary>
    public T GetMinimum( string category, TimeSpan reportSpan )
    {
      if ( reportSpan > MaxSpanToKeepInQueue )
      {
        T overallMin;
        this.categoriesOverallMin.TryGetValue( category, out overallMin );

        return overallMin;
      }

      Queue<Tuple<DateTime, T>> queue;
      if ( !this.categoriesQueues.TryGetValue( category, out queue ) )
        return default( T );

      if ( reportSpan == TimeSpan.Zero )
        return default( T );

      DateTime threshold = TimeService.Now - reportSpan;

      T result = default( T );

      foreach ( Tuple<DateTime, T> tuple in
        queue.Where( tuple => tuple.Item1 > threshold ) )
      {
        if ( Equals( result, default( T ) ) ||
             comparer.Compare( result, tuple.Item2 ) > 0 )
          result = tuple.Item2;
      }

      return result;
    }

    public int GetCount( string category, TimeSpan reportSpan )
    {
      if ( reportSpan > MaxSpanToKeepInQueue )
      {
        int overallCount;
        this.categoriesOverallCounts.TryGetValue( category, out overallCount );

        return overallCount;
      }

      Queue<Tuple<DateTime, T>> queue;
      if ( !this.categoriesQueues.TryGetValue( category, out queue ) )
        return 0;

      if ( reportSpan == TimeSpan.Zero )
        return 0;

      DateTime threshold = TimeService.Now - reportSpan;

      return queue.Count( tuple => tuple.Item1 > threshold );
    }


    public T Aggregate( string category, TimeSpan reportSpan )
    {
      if ( this.aggregator == null )
        throw new InvalidOperationException( "No aggregator function specified." );

      if ( reportSpan > MaxSpanToKeepInQueue )
      {
        T agg;
        this.categoriesOverallAgg.TryGetValue( category, out agg );

        return agg;
      }

      Queue<Tuple<DateTime, T>> queue;
      if ( !this.categoriesQueues.TryGetValue( category, out queue ) )
        return default( T );

      if ( reportSpan == TimeSpan.Zero )
        return default( T );

      DateTime threshold = TimeService.Now - reportSpan;

      T result = default( T );

      foreach ( Tuple<DateTime, T> tuple in
        queue.Where( tuple => tuple.Item1 > threshold ) )
      {
        if ( Equals( result, default( T ) ) )
          result = tuple.Item2;
        else
          result = this.aggregator( result, tuple.Item2 );
      }

      return result;
    }

    private readonly Comparer<T> comparer = Comparer<T>.Default;

    private readonly Dictionary<string, Queue<Tuple<DateTime, T>>> categoriesQueues =
      new Dictionary<string, Queue<Tuple<DateTime, T>>>( );

    private readonly Dictionary<string, T> categoriesOverallMax =
      new Dictionary<string, T>( );

    private readonly Dictionary<string, T> categoriesOverallMin =
      new Dictionary<string, T>( );

    private readonly Dictionary<string, int> categoriesOverallCounts =
      new Dictionary<string, int>( );

    private readonly Dictionary<string, T> categoriesOverallAgg =
      new Dictionary<string, T>( );

    private readonly DateTime startTime = TimeService.Now;
  }
}