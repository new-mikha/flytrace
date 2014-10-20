using System;
using FlyTrace.LocationLib.ForeignAccess;

namespace Service.Test
{
  public class MockForeignFactory : LocationRequestFactory
  {
    public int MaxCallsInPackCount;
    public int MinTimeFromPrevStartMs;
    public int MinCallsGapMs;
    public double? SameFeedHitIntervalMinutes;
    public double? SameFeedHitIntervalSeconds;

    public override LocationRequest CreateRequest( string foreignId )
    {
      throw new NotImplementedException( );
    }

    public override LocationRequest CreateTestRequest( string foreignId, string testSource )
    {
      throw new NotImplementedException( );
    }

    public override string GetStat( out bool isOk )
    {
      throw new NotImplementedException( );
    }

    public override int MaxCallsInPack
    {
      get { return MaxCallsInPackCount; }
    }

    public override TimeSpan MinTimeFromPrevStart
    {
      get { return TimeSpan.FromMilliseconds( MinTimeFromPrevStartMs ); }
    }

    public override TimeSpan MinCallsGap
    {
      get { return TimeSpan.FromMilliseconds( MinCallsGapMs ); }
    }

    public override TimeSpan SameFeedHitInterval
    {
      get
      {
        if ( SameFeedHitIntervalMinutes.HasValue )
          return TimeSpan.FromMinutes( SameFeedHitIntervalMinutes.Value );

        if ( SameFeedHitIntervalSeconds.HasValue )
          return TimeSpan.FromSeconds( SameFeedHitIntervalSeconds.Value );

        throw new InvalidOperationException( "Neither parameter for SameFeedHitInterval has set." );

      }
    }
  }
}
