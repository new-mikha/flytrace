using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace FlyTrace.Service
{
  /// <summary>Temporary thing, will be removed with TrackerDataManager.</summary>
  public static class MgrService
  {
    private static readonly bool IsNew =
      Properties.Settings.Default.IsNewScheduler;

    public static Subservices.ICoordinatesService GetCoordinatesService( int group, string srcSeed )
    {
      if ( IsNew )
        return new Subservices.CoordinatesService( group, srcSeed );

      return TrackerDataManager.Singleton;
    }

    public static Subservices.ITracksService GetTracksService( int group, TrackRequest trackRequest )
    {
      if ( IsNew )
        return new Subservices.TracksService( group, trackRequest );

      return TrackerDataManager.Singleton;
    }

    public static AdminAlerts AdminAlerts
    {
      get
      {
        if ( IsNew )
          return ForeignRequestsManager.Singleton.AdminAlerts;

        return TrackerDataManager.Singleton.AdminAlerts;
      }
    }

    public static List<TrackerStateHolder> GetTrackers( )
    {
      if ( IsNew )
      {
        ForeignRequestsManager.Singleton.HolderRwLock.AttemptEnterReadLock( );
        try
        {
          return ForeignRequestsManager.Singleton.Trackers.Values.ToList( );
        }
        finally
        {
          ForeignRequestsManager.Singleton.HolderRwLock.ExitReadLock( );
        }
      }

      lock ( TrackerDataManager.Singleton.Trackers )
      {
        return TrackerDataManager.Singleton.Trackers.Values.ToList( );
      }
    }

    public static void ClearCache( )
    {
      if ( IsNew )
        ForeignRequestsManager.Singleton.ClearTrackers( );
      else
        TrackerDataManager.Singleton.ClearCache( );
    }

    internal static void Stop( )
    {
      if ( IsNew )
        ForeignRequestsManager.Singleton.Stop( );
      else
        TrackerDataManager.Singleton.Stop( );
    }

    internal static DataSet GetStatistics( )
    {
      if ( IsNew )
        return ForeignRequestsManager.Singleton.GetStatistics( );

      return null;
    }
  }
}