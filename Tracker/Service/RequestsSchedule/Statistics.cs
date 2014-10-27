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

      TimeSpan span1Min = TimeSpan.FromMinutes( 1 );
      TimeSpan span10Min = TimeSpan.FromMinutes( 10 );
      TimeSpan span1Hr = TimeSpan.FromHours( 1 );
      TimeSpan spanOverall = TimeSpan.MaxValue;

      result.Rows.Add(
        "Max Calls In Pack Count",
        ( double ) this.callPackSizeEvents.GetMaximum( foreignType, span1Min ),
        ( double ) this.callPackSizeEvents.GetMaximum( foreignType, span10Min ),
        ( double ) this.callPackSizeEvents.GetMaximum( foreignType, span1Hr ),
        ( double ) this.callPackSizeEvents.GetMaximum( foreignType, spanOverall )
      );

      result.Rows.Add(
        "Min Time From Prev Start",
        this.timeFromPrevStartEvents.GetMinimum( foreignType, span1Min ).TotalMilliseconds,
        this.timeFromPrevStartEvents.GetMinimum( foreignType, span10Min ).TotalMilliseconds,
        this.timeFromPrevStartEvents.GetMinimum( foreignType, span1Hr ).TotalMilliseconds,
        this.timeFromPrevStartEvents.GetMinimum( foreignType, spanOverall ).TotalMilliseconds
      );

      result.Rows.Add(
        "Min Calls Gap",
        this.callGapEvents.GetMinimum( foreignType, span1Min ).TotalMilliseconds,
        this.callGapEvents.GetMinimum( foreignType, span10Min ).TotalMilliseconds,
        this.callGapEvents.GetMinimum( foreignType, span1Hr ).TotalMilliseconds,
        this.callGapEvents.GetMinimum( foreignType, spanOverall ).TotalMilliseconds
      );

      result.Rows.Add(
        "Same Feed Hit Interval",
        this.sameFeedHitEvents.GetMinimum( foreignType, span1Min ).TotalSeconds,
        this.sameFeedHitEvents.GetMinimum( foreignType, span10Min ).TotalSeconds,
        this.sameFeedHitEvents.GetMinimum( foreignType, span1Hr ).TotalSeconds,
        this.sameFeedHitEvents.GetMinimum( foreignType, spanOverall ).TotalSeconds
      );

      result.Rows.Add(
        "Min call duration",
        this.callDurationEvents.GetMinimum( foreignType, span1Min ).TotalSeconds,
        this.callDurationEvents.GetMinimum( foreignType, span10Min ).TotalSeconds,
        this.callDurationEvents.GetMinimum( foreignType, span1Hr ).TotalSeconds,
        this.callDurationEvents.GetMinimum( foreignType, spanOverall ).TotalSeconds
      );

      result.Rows.Add(
        "Max call duration",
        this.callDurationEvents.GetMaximum( foreignType, span1Min ).TotalSeconds,
        this.callDurationEvents.GetMaximum( foreignType, span10Min ).TotalSeconds,
        this.callDurationEvents.GetMaximum( foreignType, span1Hr ).TotalSeconds,
        this.callDurationEvents.GetMaximum( foreignType, spanOverall ).TotalSeconds
      );

      result.Rows.Add(
        "Avg call duration",
        GetAverageSpan( this.callDurationEvents, foreignType, span1Min ).TotalSeconds,
        GetAverageSpan( this.callDurationEvents, foreignType, span10Min ).TotalSeconds,
        GetAverageSpan( this.callDurationEvents, foreignType, span1Hr ).TotalSeconds,
        GetAverageSpan( this.callDurationEvents, foreignType, spanOverall ).TotalSeconds
      );

      // ReSharper disable RedundantCast (cast to double to make sure that double goes in there,
      // as expected by DataTable, even if return type of GetEventsPerMinute changes which is possible)
      result.Rows.Add(
        "Requests p/minute",
        ( double ) this.timeFromPrevStartEvents.GetEventsPerMinute( foreignType, span1Min ),
        ( double ) this.timeFromPrevStartEvents.GetEventsPerMinute( foreignType, span10Min ),
        ( double ) this.timeFromPrevStartEvents.GetEventsPerMinute( foreignType, span1Hr ),
        ( double ) this.timeFromPrevStartEvents.GetEventsPerMinute( foreignType, spanOverall )
      );
      // ReSharper restore RedundantCast

      result.Rows.Add(
        "Timed-out Count",
        ( double ) this.timedOutEvents.GetCount( foreignType, span1Min ),
        ( double ) this.timedOutEvents.GetCount( foreignType, span10Min ),
        ( double ) this.timedOutEvents.GetCount( foreignType, span1Hr ),
        ( double ) this.timedOutEvents.GetCount( foreignType, spanOverall )
      );

      // ReSharper disable RedundantCast (cast to double to make sure that double goes in there,
      // as expected by DataTable, even if return type of GetEventsPerMinute changes which is possible)
      result.Rows.Add(
        "Timed-out p/minute",
        ( double ) this.timedOutEvents.GetEventsPerMinute( foreignType, span1Min ),
        ( double ) this.timedOutEvents.GetEventsPerMinute( foreignType, span10Min ),
        ( double ) this.timedOutEvents.GetEventsPerMinute( foreignType, span1Hr ),
        ( double ) this.timedOutEvents.GetEventsPerMinute( foreignType, spanOverall )
      );
      // ReSharper restore RedundantCast

      return result;
    }

    private static TimeSpan GetAverageSpan( EventQueue<TimeSpan> eventQueue, string foreignType, TimeSpan reportSpan )
    {
      int count = eventQueue.GetCount( foreignType, reportSpan );
      if ( count == 0 )
        return TimeSpan.Zero;

      TimeSpan totalSpan = eventQueue.Aggregate( foreignType, reportSpan );

      return TimeSpan.FromSeconds( totalSpan.TotalSeconds / count );
    }
  }
}