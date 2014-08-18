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

    private static ConsequentErrorsCounter spotConsequentErrorsCounter;

    /// <summary>
    /// Initializes auxilliary facilities like logging. Call is optional.
    /// </summary>
    /// <param name="logFolder"></param>
    /// <param name="spotConsequentRequestsErrorCountThresold"></param>
    /// <param name="spotConsequentTimedOutRequestsThresold"></param>
    public static void InitAux( 
      string logFolder, 
      int spotConsequentRequestsErrorCountThresold ,
      int spotConsequentTimedOutRequestsThresold
    )
    {
      ForeignAccessCentral.logFolder = logFolder;
      
      ForeignAccessCentral.spotConsequentErrorsCounter =
        new ConsequentErrorsCounter( 
          spotConsequentRequestsErrorCountThresold, 
          spotConsequentTimedOutRequestsThresold 
        );
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
                spotConsequentErrorsCounter
              )
            );
          }
        }

        return locationRequestFactories;
      }
    }
  }
}
