using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FlyTrace.LocationLib.ForeignAccess
{
  public static class ForeignAccessCentral
  {
    private static Dictionary<string, LocationRequestFactory> locationRequestFactories;

    private static object sync = new object( );

    private static string logFolder = null;

    private static ThresholdCounter spotConsequentRequestsErrorCounter;

    public static void Init( string logFolder, int spotConsequentRequestsErrorCountThresold )
    {
      ForeignAccessCentral.logFolder = logFolder;
      ForeignAccessCentral.spotConsequentRequestsErrorCounter = 
        new ThresholdCounter( spotConsequentRequestsErrorCountThresold );
    }

    public static Dictionary<string, LocationRequestFactory> LocationRequestFactories
    {
      get
      {
        lock ( sync )
        {
          if ( locationRequestFactories == null )
          {
            locationRequestFactories =
              new Dictionary<string, LocationRequestFactory>( StringComparer.InvariantCultureIgnoreCase );

            locationRequestFactories.Add(
              ForeignId.SPOT,
              new Spot.SpotLocationRequestFactory( 
                logFolder ,
                spotConsequentRequestsErrorCounter
              )
            );
          }
        }

        return locationRequestFactories;
      }
    }
  }
}
