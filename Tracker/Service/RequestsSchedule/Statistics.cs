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
using System.Data;
using FlyTrace.LocationLib;
using log4net;
using FlyTrace.LocationLib.ForeignAccess;

namespace FlyTrace.Service.RequestsSchedule
{
  /// <summary>
  /// Gathers requests statistics. See Remarks for details.
  /// Thread safety: all public methods are thread safe, so consider calling it out of
  /// critical sections of code to improve performance.
  /// </summary>
  /// <remarks>
  /// Intended to provide about that structure of statistics data:
  ///     --- SPOT ---
  ///     [Counter name\interval] | 1 min  | 10 min |  1 hr  | Overall
  ///     ------------------------|--------|--------|--------|--------
  ///     MaxCallsInPack          |   3    |   5    |   7    |   10
  ///     MinTimeFromPrevStart    | 3400ms | 3000ms | 2700ms | 2000ms
  ///     MinCallsGap             | etc... | etc... | etc... | etc...
  ///     SameFeedHitInterval     |  ...   |  ...   |  ...   |  ...
  ///     Max call duration       |  ...   |  ...   |  ...   |  ...
  ///     Min call duration       |  ...   |  ...   |  ...   |  ...
  ///     Avg call duration       |  ...   |  ...   |  ...   |  ...
  ///     Requests/min            |  ...   |  ...   |  ...   |  ...
  ///     Timed out               |  ...   |  ...   |  ...   |  ...
  ///     Timed out/min           |  ...   |  ...   |  ...   |  ...
  ///     ------------------------------------------------------------
  /// 
  ///     --- DeLorme ---
  ///     ....
  /// 
  ///     --- etc ---
  ///     ....
  /// </remarks>
  public class Statistics
  {
    private readonly object sync = new object( );

    private readonly EventQueue<int> callPackSizeEvents = new EventQueue<int>( );

    private static readonly ILog Log = LogManager.GetLogger( "Statistics" );

    internal void AddCallPackEvent( string foreignType, int callPackSize )
    {
      try
      {
        lock ( this.sync )
        {
          this.callPackSizeEvents.AddEvent( foreignType, callPackSize );
        }
      }
      catch ( Exception exc )
      {
        Log.Error( exc );
      }
    }

    private readonly StatTimer<string> callStartsByForeignTypeTimer = new StatTimer<string>( );

    private readonly StatTimer<string> callGapsByForeignTypeTimer = new StatTimer<string>( );

    private readonly StatTimer<ForeignId> sameFeedHitTimer = new StatTimer<ForeignId>( );

    private readonly StatTimer<ForeignId> callDurationTimer = new StatTimer<ForeignId>( );

    private readonly EventQueue<TimeSpan> timeFromPrevStartEvents = new EventQueue<TimeSpan>( );

    private readonly EventQueue<TimeSpan> callGapEvents = new EventQueue<TimeSpan>( );

    private readonly EventQueue<TimeSpan> sameFeedHitEvents = new EventQueue<TimeSpan>( );

    private readonly EventQueue<TimeSpan> callDurationEvents =
      new EventQueue<TimeSpan>(
        ( span1, span2 ) => span1 + span2  // aggregate func to calc average durations
      );

    internal void AddRequestStartEvent( ForeignId foreignId, DateTime requestStart )
    {
      try
      {
        lock ( this.sync )
        {
          {
            TimeSpan? spanFromPrevStart = this.callStartsByForeignTypeTimer.GetSpan( foreignId.Type, requestStart );

            if ( spanFromPrevStart != null )
              this.timeFromPrevStartEvents.AddEvent( foreignId.Type, spanFromPrevStart.Value, requestStart );
          }

          {
            TimeSpan? callGap = this.callGapsByForeignTypeTimer.GetSpan( foreignId.Type, requestStart );

            if ( callGap != null )
              this.callGapEvents.AddEvent( foreignId.Type, callGap.Value, requestStart );
          }

          { // for statistics, we don't care about specific tracker's hit interval, but rather interested in 
            // hit intervals for a foreign type. 
            // So despite sameFeedHitSpan is for foreignId, event is saved for foreignId.Type:
            TimeSpan? sameFeedHitSpan = this.sameFeedHitTimer.GetSpan( foreignId, requestStart );
            if ( sameFeedHitSpan != null )
              this.sameFeedHitEvents.AddEvent( foreignId.Type, sameFeedHitSpan.Value );
          }

          this.callDurationTimer.Reset( foreignId, requestStart );
        }
      }
      catch ( Exception exc )
      {
        Log.Error( exc );
      }
    }

    internal void AddRequestEndEvent( ForeignId foreignId )
    {
      try
      {
        lock ( this.sync )
        {
          this.callGapsByForeignTypeTimer.Reset( foreignId.Type );

          {
            TimeSpan? callDuration = this.callDurationTimer.GetSpan( foreignId );

            // A span obtained for foreignId is being saved for foreignId.Type, 
            // see adding event for sameFeedHitEvents for reasons why:
            if ( callDuration != null )
              this.callDurationEvents.AddEvent( foreignId.Type, callDuration.Value );
          }
        }
      }
      catch ( Exception exc )
      {
        Log.Error( exc );
      }
    }

    private readonly EventQueue<int> timedOutEvents = new EventQueue<int>( );

    internal void AddTimedOutEvent( string foreignType )
    {
      try
      {
        lock ( this.sync )
        {
          // recording just the fact if the event here, hence 0 as parameter:
          this.timedOutEvents.AddEvent( foreignType, 0 );
        }
      }
      catch ( Exception exc )
      {
        Log.Error( exc );
      }
    }

    /// <summary>
    /// Returns dataset where each table corresponds to a foreign type (and has the same name 
    /// as the foreign type). First column is counter name, other for diff aggregation 
    /// intervals (1 min, 10 mins, 1 hr, overall). See class Remarks section for details.
    /// </summary>
    internal DataSet GetReport( )
    {
      DataSet result = new DataSet( "Statistics" );

      lock ( this.sync )
      {
        foreach ( string foreignType in ForeignAccessCentral.LocationRequestFactories.Keys )
          result.Tables.Add( CreateReportTable( foreignType ) );
      }

      return result;
    }

    private DataTable CreateReportTable( string foreignType )
    {
      DataTable result = new DataTable( foreignType );

      result.Columns.Add( "Counter" );
      result.Columns.Add( "1min", typeof( double ) ).Caption = "1 min";
      result.Columns.Add( "10min", typeof( double ) ).Caption = "10 min";
      result.Columns.Add( "1hr", typeof( double ) ).Caption = "1 hour";
      result.Columns.Add( "Overall", typeof( double ) ).Caption = "Overall";

      result.Rows.Add(
        "Max Calls In Pack Count",
        ( double ) this.callPackSizeEvents.GetMaximum( foreignType, TimeSpan.FromMinutes( 1 ) ),
        ( double ) this.callPackSizeEvents.GetMaximum( foreignType, TimeSpan.FromMinutes( 10 ) ),
        ( double ) this.callPackSizeEvents.GetMaximum( foreignType, TimeSpan.FromHours( 1 ) ),
        ( double ) this.callPackSizeEvents.GetMaximum( foreignType, TimeSpan.MaxValue )
      );

      result.Rows.Add(
        "Min Time From Prev Start",
        this.timeFromPrevStartEvents.GetMinimum( foreignType, TimeSpan.FromMinutes( 1 ) ).TotalMilliseconds,
        this.timeFromPrevStartEvents.GetMinimum( foreignType, TimeSpan.FromMinutes( 10 ) ).TotalMilliseconds,
        this.timeFromPrevStartEvents.GetMinimum( foreignType, TimeSpan.FromHours( 1 ) ).TotalMilliseconds,
        this.timeFromPrevStartEvents.GetMinimum( foreignType, TimeSpan.MaxValue ).TotalMilliseconds
      );

      result.Rows.Add(
        "Min Calls Gap",
        this.callGapEvents.GetMinimum( foreignType, TimeSpan.FromMinutes( 1 ) ).TotalMilliseconds,
        this.callGapEvents.GetMinimum( foreignType, TimeSpan.FromMinutes( 10 ) ).TotalMilliseconds,
        this.callGapEvents.GetMinimum( foreignType, TimeSpan.FromHours( 1 ) ).TotalMilliseconds,
        this.callGapEvents.GetMinimum( foreignType, TimeSpan.MaxValue ).TotalMilliseconds
      );

      result.Rows.Add(
        "Same Feed Hit Interval",
        this.sameFeedHitEvents.GetMinimum( foreignType, TimeSpan.FromMinutes( 1 ) ).TotalSeconds,
        this.sameFeedHitEvents.GetMinimum( foreignType, TimeSpan.FromMinutes( 10 ) ).TotalSeconds,
        this.sameFeedHitEvents.GetMinimum( foreignType, TimeSpan.FromHours( 1 ) ).TotalSeconds,
        this.sameFeedHitEvents.GetMinimum( foreignType, TimeSpan.MaxValue ).TotalSeconds
      );

      result.Rows.Add(
        "Min call duration",
        this.callDurationEvents.GetMinimum( foreignType, TimeSpan.FromMinutes( 1 ) ).TotalSeconds,
        this.callDurationEvents.GetMinimum( foreignType, TimeSpan.FromMinutes( 10 ) ).TotalSeconds,
        this.callDurationEvents.GetMinimum( foreignType, TimeSpan.FromHours( 1 ) ).TotalSeconds,
        this.callDurationEvents.GetMinimum( foreignType, TimeSpan.MaxValue ).TotalSeconds
      );

      result.Rows.Add(
        "Max call duration",
        this.callDurationEvents.GetMaximum( foreignType, TimeSpan.FromMinutes( 1 ) ).TotalSeconds,
        this.callDurationEvents.GetMaximum( foreignType, TimeSpan.FromMinutes( 10 ) ).TotalSeconds,
        this.callDurationEvents.GetMaximum( foreignType, TimeSpan.FromHours( 1 ) ).TotalSeconds,
        this.callDurationEvents.GetMaximum( foreignType, TimeSpan.MaxValue ).TotalSeconds
      );

      result.Rows.Add(
        "Avg call duration",
        GetAverageSpan( this.callDurationEvents, foreignType, TimeSpan.FromMinutes( 1 ) ).TotalSeconds,
        GetAverageSpan( this.callDurationEvents, foreignType, TimeSpan.FromMinutes( 10 ) ).TotalSeconds,
        GetAverageSpan( this.callDurationEvents, foreignType, TimeSpan.FromHours( 1 ) ).TotalSeconds,
        GetAverageSpan( this.callDurationEvents, foreignType, TimeSpan.MaxValue ).TotalSeconds
      );

      // ReSharper disable RedundantCast (cast to double to make sure that double goes in there,
      // as expected by DataTable, even if return type of GetEventsPerMinute changes which is possible)
      result.Rows.Add(
        "Requests p/minute",
        ( double ) this.timeFromPrevStartEvents.GetEventsPerMinute( foreignType, TimeSpan.FromMinutes( 1 ) ),
        ( double ) this.timeFromPrevStartEvents.GetEventsPerMinute( foreignType, TimeSpan.FromMinutes( 10 ) ),
        ( double ) this.timeFromPrevStartEvents.GetEventsPerMinute( foreignType, TimeSpan.FromHours( 1 ) ),
        ( double ) this.timeFromPrevStartEvents.GetEventsPerMinute( foreignType, TimeSpan.MaxValue )
      );
      // ReSharper restore RedundantCast

      result.Rows.Add(
        "Timed-out Count",
        ( double ) this.timedOutEvents.GetCount( foreignType, TimeSpan.FromMinutes( 1 ) ),
        ( double ) this.timedOutEvents.GetCount( foreignType, TimeSpan.FromMinutes( 10 ) ),
        ( double ) this.timedOutEvents.GetCount( foreignType, TimeSpan.FromHours( 1 ) ),
        ( double ) this.timedOutEvents.GetCount( foreignType, TimeSpan.MaxValue )
      );

      // ReSharper disable RedundantCast (cast to double to make sure that double goes in there,
      // as expected by DataTable, even if return type of GetEventsPerMinute changes which is possible)
      result.Rows.Add(
        "Timed-out p/minute",
        ( double ) this.timedOutEvents.GetEventsPerMinute( foreignType, TimeSpan.FromMinutes( 1 ) ),
        ( double ) this.timedOutEvents.GetEventsPerMinute( foreignType, TimeSpan.FromMinutes( 10 ) ),
        ( double ) this.timedOutEvents.GetEventsPerMinute( foreignType, TimeSpan.FromHours( 1 ) ),
        ( double ) this.timedOutEvents.GetEventsPerMinute( foreignType, TimeSpan.MaxValue )
      );
      // ReSharper restore RedundantCast

      return result;
    }

    private TimeSpan GetAverageSpan( EventQueue<TimeSpan> eventQueue, string foreignType, TimeSpan reportSpan )
    {
      int count = eventQueue.GetCount( foreignType, reportSpan );
      if ( count == 0 )
        return TimeSpan.Zero;

      TimeSpan totalSpan = eventQueue.Aggregate( foreignType, reportSpan );

      return TimeSpan.FromSeconds( totalSpan.TotalSeconds / count );
    }
  }
}