using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using FlyTrace.LocationLib;
using FlyTrace.LocationLib.ForeignAccess;
using FlyTrace.Service;
using NUnit.Framework;
using System.Reflection;

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
    }

    private DateTime RandTime( Random rand, int minutesRandomInterval )
    {
      return DateTime.UtcNow.AddMilliseconds( minutesRandomInterval * rand.Next( 60 * 1000 ) );
    }

    private static TrackerStateHolder GetHolder( string id, DateTime addedTime = default(DateTime) )
    {
      var originalReplacement = TimeService.DebugReplacement;
      try
      {
        if ( addedTime != default( DateTime ) )
          TimeService.DebugReplacement = ( ) => addedTime;

        return new TrackerStateHolder( new ForeignId( "test", id ) );
      }
      finally
      {
        TimeService.DebugReplacement = originalReplacement;
      }
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
      return now.AddSeconds( addSeconds );
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
       *    A: @.....cl...
       *    B: ...@O......
       *
       * Legend:
       * 'c' call start
       * 'l' succ call end
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

      TimeService.DebugReplacement = ( ) => this.now;

      int maxSchedulerSleep = ( int ) ( Scheduler.MaxSleepTimeSpan.TotalSeconds );

      int realTimeToWait = Math.Max( 0, sameFeedHitIntervalSeconds - 6 );

      int expectedSecondsWait = Math.Min( maxSchedulerSleep, realTimeToWait );

      var mockWaitHandle = new MockWaitHandle( expectedSecondsWait * 1000, false );

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
  }
}
