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

    private readonly EventQueue<int> callPackSizeEvents =
      new EventQueue<int>( ( i1, i2 ) => i1 + i2 );

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
        {
          if ( this.callDurationEvents.HasValues( foreignType ) )
            result.Tables.Add( CreateReportTable( foreignType ) );
        }
      }

      return result;
    }

    private readonly static TimeSpan span1Min = TimeSpan.FromMinutes( 1 );
    private readonly static TimeSpan span10Min = TimeSpan.FromMinutes( 10 );
    private readonly static TimeSpan span1Hr = TimeSpan.FromHours( 1 );
    private readonly static TimeSpan spanOverall = TimeSpan.MaxValue;

    private static void AddStatRow(
      DataTable table,
      string statName,
      Func<TimeSpan, int> statFunc
      )
    {
      table.Rows.Add(
        statName,
        statFunc( span1Min ).ToString( ),
        statFunc( span10Min ).ToString( ),
        statFunc( span1Hr ).ToString( ),
        statFunc( spanOverall ).ToString( )
      );
    }


    private static void AddStatRow(
      DataTable table,
      string statName,
      Func<TimeSpan, TimeSpan> statFunc
      )
    {
      AddStatRow( table, statName, span => statFunc( span ).TotalMilliseconds, "{0:000} ms" );
    }

    private static void AddStatRow(
      DataTable table,
      string statName,
      Func<TimeSpan, double> statFunc,
      string formatString
      )
    {
      table.Rows.Add(
        statName,
        string.Format( formatString, statFunc( span1Min ) ),
        string.Format( formatString, statFunc( span10Min ) ),
        string.Format( formatString, statFunc( span1Hr ) ),
        string.Format( formatString, statFunc( spanOverall ) )
      );
    }


    private DataTable CreateReportTable( string foreignType )
    {
      DataTable result = new DataTable( foreignType );

      result.Columns.Add( "Counter" );
      result.Columns.Add( "1min" ).Caption = "1 min";
      result.Columns.Add( "10min" ).Caption = "10 min";
      result.Columns.Add( "1hr" ).Caption = "1 hour";
      result.Columns.Add( "Overall" ).Caption = "Overall";

      AddStatRow(
        result,
        "Max Calls In Pack Count",
        span => this.callPackSizeEvents.GetMaximum( foreignType, span )
      );

      AddStatRow(
        result,
        "Avg Calls In Pack Count",
        span => GetAverageCount( this.callPackSizeEvents, foreignType, span ),
        "{0:F1}"
      );

      AddStatRow(
        result,
        "Min Time From Prev Start",
        span => this.timeFromPrevStartEvents.GetMinimum( foreignType, span )
      );

      AddStatRow(
        result,
        "Max Time From Prev Start",
        span => this.timeFromPrevStartEvents.GetMaximum( foreignType, span )
      );

      AddStatRow(
        result,
        "Min Calls Gap",
        span => this.callGapEvents.GetMinimum( foreignType, span )
      );

      AddStatRow(
        result,
        "Same Feed Hit Interval",
        span => this.sameFeedHitEvents.GetMinimum( foreignType, span ).TotalMinutes,
        "{0:F1} min"
      );

      AddStatRow(
        result,
        "Min call duration",
        span => this.callDurationEvents.GetMinimum( foreignType, span )
      );

      AddStatRow(
        result,
        "Avg call duration",
        span => GetAverageSpan( this.callDurationEvents, foreignType, span )
      );

      AddStatRow(
        result,
        "Max call duration",
        span => this.callDurationEvents.GetMaximum( foreignType, span )
      );

      AddStatRow(
        result,
        "Requests p/minute",
        span => this.timeFromPrevStartEvents.GetEventsPerMinute( foreignType, span ),
        "{0:F1}"
      );

      AddStatRow(
        result,
        "Timed-out Count",
        span => this.timedOutEvents.GetCount( foreignType, span )
      );

      AddStatRow(
        result,
        "Timed-out p/minute",
        span => this.timedOutEvents.GetEventsPerMinute( foreignType, span ),
        "{0:F1}"
      );

      return result;
    }

    private static double GetAverageCount( EventQueue<int> eventQueue, string foreignType, TimeSpan reportSpan )
    {
      int count = eventQueue.GetCount( foreignType, reportSpan );
      if ( count == 0 )
        return 0.0;

      int totalValue = eventQueue.Aggregate( foreignType, reportSpan );

      return ( ( double ) totalValue ) / count;
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