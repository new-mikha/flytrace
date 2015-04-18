using System;
using System.Collections.Generic;

namespace FlyTrace.LocationLib.ForeignAccess
{
  public static class ForeignAccessCentral
  {
    private static Dictionary<string, LocationRequestFactory> LocationRequestFactoriesPrivate;

    private static readonly object Sync = new object( );

    private static string LogFolder;

    private static ConsequentErrorsCounter SpotConsequentErrorsCounter;

    /// <summary>
    /// Initializes auxilliary facilities like logging. Call is optional.
    /// </summary>
    /// <param name="logFolder"></param>
    /// <param name="spotConsequentRequestsErrorCountThresold"></param>
    /// <param name="spotConsequentTimedOutRequestsThresold"></param>
    public static void InitAux(
      string logFolder,
      int spotConsequentRequestsErrorCountThresold,
      int spotConsequentTimedOutRequestsThresold,
      int spotUnexpectedForeignErrorsThresold
    )
    {
      LogFolder = logFolder;

      SpotConsequentErrorsCounter =
        new ConsequentErrorsCounter(
          spotConsequentRequestsErrorCountThresold,
          spotConsequentTimedOutRequestsThresold,
          spotUnexpectedForeignErrorsThresold
        );
    }

    public static Dictionary<string, LocationRequestFactory> LocationRequestFactories
    {
      get
      {
        lock ( Sync )
        {
          if ( LocationRequestFactoriesPrivate == null )
          {
            LocationRequestFactoriesPrivate =
              new Dictionary<string, LocationRequestFactory>( StringComparer.InvariantCultureIgnoreCase );

            LocationRequestFactoriesPrivate.Add(
              ForeignId.SPOT,
              new Spot.SpotLocationRequestFactory(
                LogFolder,
                SpotConsequentErrorsCounter
              )
            );

            LocationRequestFactoriesPrivate.Add(
              ForeignId.TEST,
              new Test.TestLocationRequestFactory( )
            );
          }
        }

        return LocationRequestFactoriesPrivate;
      }
    }
  }
}
