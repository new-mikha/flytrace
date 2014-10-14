using System.Collections.Generic;
using System;
using FlyTrace.LocationLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using FlyTrace.Service;

namespace Service.Test
{
  /// <summary>
  ///This is a test class for SchedulerTest and is intended
  ///to contain all SchedulerTest Unit Tests
  ///</summary>
  [TestClass]
  public class SchedulerTest
  {
    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext { get; set; }

    #region Additional test attributes
    // 
    //You can use the following additional attributes as you write your tests:
    //
    //Use ClassInitialize to run code before running the first test in the class
    //[ClassInitialize()]
    //public static void MyClassInitialize(TestContext testContext)
    //{
    //}
    //
    //Use ClassCleanup to run code after all tests in a class have run
    //[ClassCleanup()]
    //public static void MyClassCleanup()
    //{
    //}
    //
    //Use TestInitialize to run code before running each test
    //[TestInitialize()]
    //public void MyTestInitialize()
    //{
    //}
    //
    //Use TestCleanup to run code after each test has run
    //[TestCleanup()]
    //public void MyTestCleanup()
    //{
    //}
    //
    #endregion


    private Random rand;

    /// <summary>
    ///A test for GetMoreStaleTracker
    ///</summary>
    [TestMethod]
    public void GetMoreStaleTrackerTest( )
    {
      for ( int iSequence = 0; iSequence < 1000; iSequence++ )
      {
        GenerateAndTestStaleSequence( iSequence );
        if ( iSequence % 50 == 0 )
          Console.WriteLine( "Sequence {0} done", iSequence );
      }
    }

    private DateTime GetRandomDateTime( )
    {
      return DateTime.Now;
    }

    private void GenerateAndTestStaleSequence( int iSequence )
    {
      this.rand = new Random( iSequence );

      TrackerStateHolder.GetNow = GetRandomDateTime;

      List<TrackerStateHolder> list = new List<TrackerStateHolder>( );

      for ( int i = 0; i < 100; i++ )
      {
        TrackerStateHolder holder = new TrackerStateHolder( new ForeignId( "test", i.ToString( ) ) );

        if ( rand.NextDouble( ) < 0.2 )
          holder.ScheduledTime = GetRandomDateTime( );

        if ( rand.NextDouble( ) < 0.8 )
          holder.RefreshTime = GetRandomDateTime( );
        list.Add( holder );
      }

      list.Sort( CompareHolders );

      TrackerStateHolder a = list[0];
      TrackerStateHolder b = list[1];
      TrackerStateHolder c = list[2];

      for ( int i = 0; i < list.Count; i++ )
      {
        for ( int j = 0; j < list.Count; j++ )
        {
          string tst = string.Format( "{0}, {1}, {2}", iSequence, i, j );

          if ( i == j )
            Assert.AreEqual( 0, CompareHolders( list[i], list[j] ), tst );
          else if ( i < j )
            Assert.IsTrue( CompareHolders( list[i], list[j] ) < 0, tst );
          else
            Assert.IsTrue( CompareHolders( list[i], list[j] ) > 0, tst );
        }
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
  }
}
