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

using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

using FlyTrace.LocationLib;
using FlyTrace.LocationLib.ForeignAccess;
using FlyTrace.Service;
using FlyTrace.Service.RequestsSchedule;
using NUnit.Framework;

namespace Service.Test
{
  /// <summary>
  ///This is a test class for SchedulerTest and is intended
  ///to contain all SchedulerTest Unit Tests
  ///</summary>
  [TestFixture]
  public class SchedulerTest
  {
    [TearDown]
    public void TearDown( )
    {
      TimeService.DebugReplacement = null;
      this.timeoutSpanInSeconds = -1;
    }

    private DateTime RandTime( Random rand, int minutesRandomInterval )
    {
      return DateTime.UtcNow.AddMilliseconds( minutesRandomInterval * rand.Next( 60 * 1000 ) );
    }

    private static TrackerStateHolder GetHolder( string id, DateTime addedTime = default(DateTime) )
    {
      return GetHolder( GetForeignId( id ), addedTime );
    }

    private static TrackerStateHolder GetHolder( ForeignId id, DateTime addedTime = default(DateTime) )
    {
      var originalReplacement = TimeService.DebugReplacement;
      try
      {
        if ( addedTime != default( DateTime ) )
          TimeService.DebugReplacement = ( ) => addedTime;

        return new TrackerStateHolder( id );
      }
      finally
      {
        TimeService.DebugReplacement = originalReplacement;
      }
    }

    private static ForeignId GetForeignId( string id )
    {
      return new ForeignId( "test", id );
    }

    /// <summary>
    ///A test for GetMoreStaleTracker
    ///</summary>
    [Test]
    [Combinatorial]
    public void GetMoreStaleTrackerTest(
      // ReSharper disable once ParameterHidesMember
      [Values( 0, -15, 15 )]int minutesRandomInterval
    )
    {
      for ( int iSequence = 0; iSequence < 200; iSequence++ )
      {
        var rand = new Random( iSequence );

        Func<DateTime> randTimeFunc =
          ( ) => RandTime( rand, minutesRandomInterval );

        TimeService.DebugReplacement = randTimeFunc;

        List<TrackerStateHolder> list = new List<TrackerStateHolder>( );

        for ( int i = 0; i < 100; i++ )
        {
          TrackerStateHolder holder = GetHolder( i.ToString( ) );

          if ( rand.NextDouble( ) < 0.2 )
            holder.ScheduledTime = randTimeFunc( );

          if ( rand.NextDouble( ) < 0.8 )
            holder.RefreshTime = randTimeFunc( );
          list.Add( holder );
        }

        list.Sort( CompareHolders );

        for ( int i = 0; i < list.Count; i++ )
        {
          for ( int j = 0; j < list.Count; j++ )
          {
            string tst = string.Format( "{0}, {1}, {2}, {3}", iSequence, i, j, minutesRandomInterval );

            if ( i == j )
              Assert.AreEqual( 0, CompareHolders( list[i], list[j] ), tst );
            else if ( i < j )
              Assert.IsTrue( CompareHolders( list[i], list[j] ) < 0, tst );
            else
              Assert.IsTrue( CompareHolders( list[i], list[j] ) > 0, tst );
          }
        }

        if ( iSequence % 50 == 0 )
          Debug.WriteLine( "Sequence {0} done", iSequence );
      }
    }

    private static int CompareHolders( TrackerStateHolder x, TrackerStateHolder y )
    {
      bool areEqual;
      TrackerStateHolder moreStale = Scheduler.GetMoreStaleTracker( x, y, out areEqual );

      if ( areEqual )
        return string.Compare( x.ForeignId.Id, y.ForeignId.Id, StringComparison.InvariantCultureIgnoreCase );

      if ( ReferenceEquals( moreStale, x ) )
        return -1;

      return 1;
    }

    private readonly DateTime now = DateTime.UtcNow;

    private DateTime Now( double addSeconds = 0 )
    {
      return this.now.AddSeconds( addSeconds );
    }

    [Test]
    [Combinatorial]
    public void BasicSchedulerTest(
      [Values( 0, 1, 3, 5, 6, 7, 8, 9, 10, 150 )] int sameFeedHitIntervalSeconds
      )
    {
      ForeignAccessCentral.LocationRequestFactories["test"] =
        new MockForeignFactory( )
        {
          MaxCallsInPackCount = 2,
          MinTimeFromPrevStartMs = 2000,
          MinCallsGapMs = 0,
          SameFeedHitIntervalSeconds = sameFeedHitIntervalSeconds
        };

      Scheduler scheduler = new Scheduler( );

      /*
       * time: 09876543210
       *    A: @.....[]...
       *    B: ...@O......
       *
       * Legend:
       * '[' call start
       * ']' succ call end
       * 'O' call started and succ.finished on the same second
       */

      {
        TrackerStateHolder holder = GetHolder( "A", Now( -10 ) );
        holder.RequestStartTime = Now( -4 );
        holder.RefreshTime = Now( -3 );

        scheduler.Trackers.Add( holder.ForeignId, holder );
      }

      {
        TrackerStateHolder holder = GetHolder( "B", Now( -7 ) );
        holder.RequestStartTime = Now( -6 );
        holder.RefreshTime = Now( -6 );

        scheduler.Trackers.Add( holder.ForeignId, holder );
      }

      int maxSchedulerSleep = ( int ) ( Scheduler.MaxSleepTimeSpan.TotalSeconds );

      int realTimeToWait = Math.Max( 0, sameFeedHitIntervalSeconds - 6 );

      int expectedSecondsWait = Math.Min( maxSchedulerSleep, realTimeToWait );

      var mockWaitHandle = new MockWaitHandle( expectedSecondsWait * 1000, false );

      TimeService.DebugReplacement = ( ) => this.now;

      IEnumerable<TrackerStateHolder> trackersToRequest = scheduler.ScheduleCleanupWait( mockWaitHandle );

      Assert.IsTrue( mockWaitHandle.IsWaitSucceeded );

      if ( realTimeToWait <= maxSchedulerSleep )
      {
        Assert.AreEqual( 1, trackersToRequest.Count( ) );
        Assert.AreEqual( "B", trackersToRequest.First( ).ForeignId.Id );
      }
      else
      {
        Assert.AreEqual( 0, trackersToRequest.Count( ) );
      }
    }

    /// <summary>Same as BasicSchedulerTest, but uses <see cref="ParseAndTest"/> method.</summary>
    [Test]
    [Combinatorial]
    public void BasicSchedulerTestWithAutoTimeLine(
      [Values( 0, 1, 3, 5, 6, 7, 8, 9, 10, 150 )] int sameFeedHitIntervalSeconds
    )
    {
      ForeignAccessCentral.LocationRequestFactories["test"] =
        new MockForeignFactory( )
        {
          MaxCallsInPackCount = 2,
          MinTimeFromPrevStartMs = 2000,
          MinCallsGapMs = 0,
          SameFeedHitIntervalSeconds = sameFeedHitIntervalSeconds
        };

      int realTimeToWait = Math.Max( 0, sameFeedHitIntervalSeconds - 6 );

      ParseAndTest(
        realTimeToWait,
        "  time: 09876543210"
        , "   A: @.....[]..."
        , "  *B: ...@[......"
        , "   B: ....]......"
      );
    }

    [Test]
    [Combinatorial]
    public void StartedCallTest(
      [Values( 0, 1, 3, 5, 6, 7, 8, 9, 10, 150 )] int sameFeedHitIntervalSeconds
    )
    {
      ForeignAccessCentral.LocationRequestFactories["test"] =
        new MockForeignFactory( )
        {
          MaxCallsInPackCount = 2,
          MinTimeFromPrevStartMs = 2000,
          MinCallsGapMs = 0,
          SameFeedHitIntervalSeconds = sameFeedHitIntervalSeconds
        };

      int realTimeToWait = Math.Max( 0, sameFeedHitIntervalSeconds - 4 );

      ParseAndTest(
        realTimeToWait,
        "  time: 09876543210"
        , "*  A: @.....[]..."
        , "   B: ...@......."
        , "   B: .....[....."
      );
    }

    [Test]
    [Combinatorial]
    public void ArbitraryPrevStartInterval(
      [Values( 1000, 2000, 3000, 4000 )] int minTimeFromPrevStartMs,
      [Values( 0, 1, 3, 5, 6, 7, 8, 9, 10, 150 )] int sameFeedHitIntervalSeconds
    )
    {
      ForeignAccessCentral.LocationRequestFactories["test"] =
        new MockForeignFactory( )
        {
          MaxCallsInPackCount = 5,
          MinTimeFromPrevStartMs = minTimeFromPrevStartMs,
          MinCallsGapMs = 0,
          SameFeedHitIntervalSeconds = sameFeedHitIntervalSeconds
        };

      int realTimeToWait =
        Math.Max(
          minTimeFromPrevStartMs / 1000 - 3,
          Math.Max( 0, sameFeedHitIntervalSeconds - 4 )
        );

      ParseAndTest(
        realTimeToWait,
        "  time: 76543210"
        , "   A: .@S....."
        , "   B: @..[.].."
        , "  *C: ..@[]..."
        , "   D: @...[].."
      );
    }

    [Test]
    [Combinatorial]
    public void Zeros(
      [Values( 1, 2, 3, 4, 5, 10, 20 )] int maxCallsInPackCount
      )
    {
      ForeignAccessCentral.LocationRequestFactories["test"] =
        new MockForeignFactory( )
        {
          MaxCallsInPackCount = maxCallsInPackCount,
          MinTimeFromPrevStartMs = 0,
          MinCallsGapMs = 0,
          SameFeedHitIntervalSeconds = 0
        };

      int realTimeToWait =
        maxCallsInPackCount > 3
        ? 0
        : 1 + ( int ) Scheduler.MaxSleepTimeSpan.TotalSeconds;

      this.timeoutSpanInSeconds = 1;

      ParseAndTest(
        realTimeToWait,
        "  time: 210",
        "     A: @[.",
        "     B: @[.",
        "     C: @.[",
        "    *D: @.."
      );
    }

    private int timeoutSpanInSeconds = -1;

    [Test]
    [Combinatorial]
    public void OneCallInPack(
      [Values( 0, 1, 2, 3 )] int minTimeFromPrevStart,
      [Values( 0, 1, 2 )] int minCallsGap,
      [Values( 0, 1, 3, 5, 6, 7, 8, 9, 10, 150 )] int sameFeedHitInterval,
      [Values(
        "     A: @....[.]",
        "     A: @....S..",
        "     A: @....[.."
        )] string prevTimeline
    )
    {
      ForeignAccessCentral.LocationRequestFactories["test"] =
        new MockForeignFactory( )
        {
          MaxCallsInPackCount = 1,
          MinTimeFromPrevStartMs = minTimeFromPrevStart * 1000,
          MinCallsGapMs = minCallsGap * 1000,
          SameFeedHitIntervalSeconds = sameFeedHitInterval
        };

      int realTimeToWait;
      if ( !prevTimeline.Contains( "]" ) )
        realTimeToWait = 0;
      else
      {
        realTimeToWait = Math.Max( 0,
          Math.Max(
            minTimeFromPrevStart - 2,
            minCallsGap
            ) );
      }

      realTimeToWait = Math.Max( realTimeToWait, sameFeedHitInterval - 6 );

      this.timeoutSpanInSeconds = 1;

      ParseAndTest(
        realTimeToWait,
        prevTimeline,
        "  time: 76543210",
        "    *B: @[.]...."
      );
    }


    [Test]
    [Combinatorial]
    public void FullPack(
      [Values( 1000, 2000, 3000, 4000 )] int minTimeFromPrevStartMs,
      [Values( 0, 1, 3, 5, 6, 7, 8, 9, 10, 150 )] int sameFeedHitIntervalSeconds
    )
    {
      ForeignAccessCentral.LocationRequestFactories["test"] =
        new MockForeignFactory( )
        {
          MaxCallsInPackCount = 4,
          MinTimeFromPrevStartMs = minTimeFromPrevStartMs,
          MinCallsGapMs = 0,
          SameFeedHitIntervalSeconds = sameFeedHitIntervalSeconds
        };

      int realTimeToWait = 1 + ( int ) Scheduler.MaxSleepTimeSpan.TotalSeconds;

      ParseAndTest(
        realTimeToWait,
        "  time: 76543210"
        , "   A: .@...[.."
        , "   B: @..][..."
        , "   C: ..@][..."
        , "   D: @...[..."
        , "   E: @.[]...."
      );
    }

    [Test]
    [Combinatorial]
    public void BadFactory(
      [Values( -1, 0, 1, 10, 20, 21, 2000 )] int maxCallsInPackCount,
      [Values( -1000, 0, 2000, 60000, 60001, int.MaxValue )] int minTimeFromPrevStartMs,
      [Values( -1000, 0, 2000, 60000, 60001, int.MaxValue )] int minCallsGapMs,
      [Values( -1, 0, 1, 1800, 1801, int.MaxValue )] int sameFeedHitIntervalSeconds
    )
    {
      ForeignAccessCentral.LocationRequestFactories["test"] =
        new MockForeignFactory( )
        {
          MaxCallsInPackCount = maxCallsInPackCount,
          MinTimeFromPrevStartMs = minTimeFromPrevStartMs,
          MinCallsGapMs = minCallsGapMs,
          SameFeedHitIntervalSeconds = sameFeedHitIntervalSeconds
        };

      bool isGoodFactory;
      if ( maxCallsInPackCount < 1 || maxCallsInPackCount > 20 )
        isGoodFactory = false;
      else
        isGoodFactory =
          minTimeFromPrevStartMs >= 0 && minTimeFromPrevStartMs <= 60000 &&
          minCallsGapMs >= 0 && minCallsGapMs <= 60000 &&
          sameFeedHitIntervalSeconds >= 0 && sameFeedHitIntervalSeconds <= 1800;

      try
      {
        //int realTimeToWait = 1 + ( int ) Scheduler.MaxSleepTimeSpan.TotalSeconds;

        ParseAndTest(
          0,
          "  time: 0"
          , "  *A: @"
        );

        Assert.IsTrue( isGoodFactory, "Should be bad, but didn't throw an exception" );

      }
      catch ( ApplicationException exc )
      {
        if ( isGoodFactory )
          throw;

        if ( !exc.Message.StartsWith( "test factory config error:" ) )
          throw;
      }
    }


    [Test]
    [Combinatorial]
    public void NothingToRequestTest(
      [Values( 1, 2, 5, 10 )] int maxCallsInPackCount,
      [Values( 0, 500, 10000 )] int minTimeFromPrevStartMs,
      [Values( 0, 2000, 10000 )] int minCallsGapMs,
      [Values( 0, 3, 150 )] int sameFeedHitIntervalSeconds,
      [Values( 0, 1, 2, 5, 10 )] int callCount
    )
    {
      ForeignAccessCentral.LocationRequestFactories["test"] =
        new MockForeignFactory( )
        {
          MaxCallsInPackCount = maxCallsInPackCount,
          MinTimeFromPrevStartMs = minTimeFromPrevStartMs,
          MinCallsGapMs = minCallsGapMs,
          SameFeedHitIntervalSeconds = sameFeedHitIntervalSeconds
        };

      const int timelineLen = 10;
      List<string> lines = new List<string>( );

      var rand = new Random( 0 );

      for ( int iTracker = 0; iTracker < callCount; iTracker++ )
      {
        StringBuilder sb = new StringBuilder( timelineLen + 10 );
        sb.Append( new string( ' ', timelineLen ) );

        int iMin = 0;
        int iMax = timelineLen - 1;

        int i;

        i = rand.Next( iMin, iMax );
        sb[i] = '@';
        iMin = i + 1;
        iMax = timelineLen;

        if ( rand.NextDouble( ) > 0.2 )
        {
          i = rand.Next( iMin, iMax );
          sb[i] = '[';
          iMax = i;

          if ( iMin < iMax )
          {
            i = rand.Next( iMin, iMax );
            sb[i] = ']';
          }
        }
        else
        {
          i = rand.Next( iMin, iMax );
          sb[i] = 'S';
          iMax = i - 1;

          if (
            rand.NextDouble( ) < 0.5 &&
            iMin < iMax )
          {
            i = rand.Next( iMin, iMax );
            sb[i] = '[';
            sb[i + 1] = ']';
          }
        }

        sb.Insert( 0, string.Format( "Tracker #{0}:", iTracker ) );

        lines.Add( sb.ToString( ) );
      }

      int maxSchedulerSleep = ( int ) ( Scheduler.MaxSleepTimeSpan.TotalSeconds );
      ParseAndTest(
        maxSchedulerSleep + 1,
        lines.ToArray( )
      );
    }


    /// <summary>
    /// <para>@ add</para>
    /// <para>[ start</para>
    /// <para>] end</para>
    /// <para># add + start</para>
    /// <para>$ add + start + end</para>
    /// <para>% start + end</para>
    /// </summary>
    private void ParseAndTest(
      int realTimeToWait,
      params string[] lines )
    {
      int maxSchedulerSleep = ( int ) ( Scheduler.MaxSleepTimeSpan.TotalSeconds );

      TimeService.DebugReplacement = ( ) => this.now;

      Scheduler scheduler = new Scheduler( );

      if ( this.timeoutSpanInSeconds > 0 )
        scheduler.TimeoutSpanInSeconds = this.timeoutSpanInSeconds;

      int? iTimeLineLength = null;

      // for a parameter i, returns time corresponding to the point in the timeline
      // e.g. '@' in "...@." corresponds to (this.now - 1 sec).
      Func<int, DateTime> getTimeFunc =
        i =>
          // ReSharper disable once AccessToModifiedClosure
          // ReSharper disable once PossibleInvalidOperationException
          Now( i - iTimeLineLength.Value + 1 );

      string winningName = null;

      foreach ( string line in lines )
      {
        if ( line.Trim( ).ToLower( ).StartsWith( "time:" ) )
          continue; // line started from "time:" is kind of a comment, ignoring that

        int colIndex = line.IndexOf( ':' );
        if ( colIndex <= 0 )
          throw new Exception( "Cannot find name for: " + line );

        string name =
          line
          .Remove( colIndex )
          .Replace( '*', ' ' )
          .Trim( );

        string timeLine = line.Substring( colIndex + 1 ).ToLower( );

        ForeignId id = GetForeignId( name );

        TrackerStateHolder holder;

        // might be tracker was added by the prev.line:
        scheduler.Trackers.TryGetValue( id, out holder );

        if ( iTimeLineLength == null )
          iTimeLineLength = timeLine.Length;
        else if ( iTimeLineLength != timeLine.Length )
          throw new Exception( "Timelines have different lengths" );

        for ( int i = 0; i < timeLine.Length; i++ )
        {
          char c = timeLine[i];
          // @ add
          // [ call start
          // ] call end
          // s scheduled start

          if ( c == '@' )
          {
            if ( holder != null )
              throw new Exception(
                "Only one '@' per tracker can be found, and it should be first for the tracker: " + id.Id );

            holder = GetHolder( id, getTimeFunc( i ) );
            scheduler.Trackers.Add( id, holder );
          }

          if ( c == '[' ) // holder should be created by now by '@' encountered earlier, otherwise it's NullRefException
            holder.RequestStartTime = getTimeFunc( i );

          if ( c == ']' )
          {
            holder.RefreshTime = getTimeFunc( i );
            holder.Snapshot = CreateMockupSnapshot( holder.RefreshTime.Value );
          }

          if ( c == 's' )
            holder.ScheduledTime = getTimeFunc( i );
        }

        if ( line.Contains( "*" ) )
        {
          if ( winningName != null )
            throw new Exception( "'*' sign (for a winning tracker) specified more than once" );

          winningName = name;
        }

        if ( holder.CurrentRequest == null &&
             holder.RequestStartTime.HasValue &&
             (
                holder.RefreshTime == null ||
                holder.RequestStartTime.Value > holder.RefreshTime.Value
             )
           )
        {
          // just any request to make the field not-null:

          var originalReplacement = TimeService.DebugReplacement;
          try
          {
            TimeService.DebugReplacement = ( ) => holder.RequestStartTime.Value;

            holder.CurrentRequest = new FakeLocationRequest( id.Id );
          }
          finally
          {
            TimeService.DebugReplacement = originalReplacement;
          }
        }
        else
        { // need else because it could be set earlier by one of the prev. lines.
          holder.CurrentRequest = null;
        }

        Assert.AreEqual( null, holder.CheckTimesConsistency( ) );
      }

      int expectedSecondsWait = Math.Min( maxSchedulerSleep, realTimeToWait );
      var mockWaitHandle = new MockWaitHandle( expectedSecondsWait * 1000, false );

      IEnumerable<TrackerStateHolder> trackersToRequest = scheduler.ScheduleCleanupWait( mockWaitHandle );

      Assert.IsTrue( mockWaitHandle.IsWaitSucceeded );

      if ( realTimeToWait <= maxSchedulerSleep )
      {
        Assert.AreEqual( 1, trackersToRequest.Count( ) );
        Assert.AreEqual( winningName, trackersToRequest.First( ).ForeignId.Id );
      }
      else
      {
        Assert.AreEqual( 0, trackersToRequest.Count( ) );
      }
    }

    private class FakeLocationRequest : LocationRequest
    {
      public FakeLocationRequest( string id )
        : base( id )
      {
      }

      public override string ForeignType
      {
        get { throw new NotImplementedException( ); }
      }

      public override IAsyncResult BeginReadLocation( AsyncCallback callback, object state )
      {
        throw new NotImplementedException( );
      }

      protected override TrackerState EndReadLocationProtected( IAsyncResult ar )
      {
        throw new NotImplementedException( );
      }

      public override void SafelyAbortRequest( AbortStat abortStat )
      {
        throw new NotImplementedException( );
      }
    }

    private RevisedTrackerState CreateMockupSnapshot( DateTime timeToUseAsNow )
    {
      var originalReplacement = TimeService.DebugReplacement;
      try
      {
        TimeService.DebugReplacement = ( ) => timeToUseAsNow;

        TrackerState trackerState = new TrackerState( "foo", "foo" );
        return RevisedTrackerState.Merge( null, trackerState );
      }
      finally
      {
        TimeService.DebugReplacement = originalReplacement;
      }
    }
  }
}
