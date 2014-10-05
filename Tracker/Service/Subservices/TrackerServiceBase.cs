using System;
using System.Threading;
using log4net;

namespace FlyTrace.Service.Subservices
{
  public class TrackerServiceBase
  {
    protected readonly int Group;
    protected readonly long CallId;
    protected readonly DateTime CallStartTime = DateTime.UtcNow;

    protected TrackerServiceBase( int group )
    {
      Group = group;
      CallId = Interlocked.Increment( ref CallIdSource );
    }

    protected static readonly ILog IncrTestLog = LogManager.GetLogger( "IncrTest" );
    protected static readonly ILog InfoLog = LogManager.GetLogger( "InfoLog" );
    protected static readonly ILog IncrLog = LogManager.GetLogger( "TDM.IncrUpd" );
    protected static readonly ILog Log = LogManager.GetLogger( "TDM" );

    protected static int SimultaneousCallCount;

    private static long CallIdSource;

    protected readonly GroupFacade GroupFacade = new GroupFacade( );

    protected static void LogCallCount( int callCount )
    {
      if ( !Log.IsDebugEnabled ) return;

      if ( callCount > 100 )
      {
        Log.DebugFormat( "Got callCount > 100 , i.e. {0}", callCount );
      }
      else if ( callCount > 10 )
      {
        Log.DebugFormat( "Got callCount > 10 , i.e. {0}", callCount );
      }
      else if ( callCount > 5 )
      {
        Log.DebugFormat( "Got callCount > 5 , i.e. {0}", callCount );
      }
      else if ( callCount > 3 )
      {
        Log.DebugFormat( "Got callCount > 3 , i.e. {0}", callCount );
      }
    }
  }
}