using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using log4net;

namespace FlyTrace.Service
{
  // Making CallData a type parameter for AsyncChainedState<> seems to be the easiest way 
  // to pass CallData to End* methods (note there are more than one End method returning 
  // different types). Alternative would be passing a separate class having both Names and 
  // TrackerStateHolder collections, which means additional "new" operation etc.
  internal class CallData2 : AsyncChainedState<GroupData>
  {
    private static long idSource = 0;

    public readonly long CallId;

    /// <summary>
    /// Used for getting current coordinates of trackers
    /// </summary>
    /// <param name="group"></param>
    /// <param name="clientSeed"></param>
    /// <param name="outerCallback"></param>
    /// <param name="outerAsyncState"></param>
    public CallData2( int group, string clientSeed, AsyncCallback outerCallback, Object outerAsyncState )
      : base( outerCallback, outerAsyncState )
    {
      Group = group;
      TrackRequests = null;
      SourceSeed = clientSeed;
      CallId = Interlocked.Increment( ref idSource );
    }

    /// <summary>
    /// Used to get tracks
    /// </summary>
    /// <param name="group"></param>
    /// <param name="trackRequests"></param>
    /// <param name="outerCallback"></param>
    /// <param name="outerAsyncState"></param>
    public CallData2( int group, TrackRequestItem[] trackRequests, AsyncCallback outerCallback, Object outerAsyncState )
      : base( outerCallback, outerAsyncState )
    {
      Group = group;
      TrackRequests = trackRequests;
      CallId = Interlocked.Increment( ref idSource );
    }

    public readonly DateTime CallStartTime = DateTime.UtcNow;

    public readonly int Group;

    /// <summary>Seed string from the request, see  "Incremental update algorithm explanation" 
    /// comment in <see cref="TrackerDataManager2"/></summary>
    public readonly string SourceSeed;

    public readonly GroupFacade GroupFacade = new GroupFacade( );

    /// <summary>Used only for GetFullTrack</summary>
    public readonly TrackRequestItem[] TrackRequests;

    public TrackerStateHolder[] TrackerStateHolders;

    /// <summary>Group version, see "Incremental update algorithm explanation" comment 
    /// in <see cref="TrackerDataManager2"/>. Could be different from value that is a 
    /// part of <see cref="SourceSeed"/></summary>
    public int ActualGroupVersion;

    /// <summary>True if the group allows to show used-defined message from foreign system</summary>
    public bool ShowUserMessages;

    /// <summary>If the group has "hide points older than..." turned on, keep the starting timestamp.</summary>
    public DateTime? StartTs;

    private readonly static ILog IncrLog = LogManager.GetLogger( "TDM.IncrUpd" );

    /// <summary>
    /// A valid client seed looks like that: "25;54215" where 1st value is the group version,
    /// and 2nd value is the maximum client tracker revision, both received by the client earlier by
    /// a similar call to this web service. Result is not null only if the group version from client seed 
    /// is equal to the current actual group version kept in Seed.ActualGroupVersion. There are also some other
    /// checks to ensure that the extracted revision is healthy. If anything's wrong then null returned,
    /// and as a result the client receives a full actual group info.
    /// </summary>
    /// <returns></returns>
    public int? TryParseThresholdRevision( )
    {
      if ( SourceSeed == null )
        return null;

      if ( TrackerStateHolders.Length == 0 ) // if group is empty it should be full update
        return null;

      string[] elements = SourceSeed.Split( ';' );
      if ( elements == null || elements.Length != 2 )
        return null;

      {
        int clientGroupVersion;

        if ( !int.TryParse( elements[0], out clientGroupVersion ) )
          return null;

        if ( clientGroupVersion < 0 )
          return null;

        if ( clientGroupVersion != ActualGroupVersion )
        {
          if ( IncrLog.IsInfoEnabled )
          {
            IncrLog.InfoFormat
            (
              "Call id {0}, actual group version ({1}) is different from one came from the client ({2}), so this call is not incremental",
              CallId,
              ActualGroupVersion,
              clientGroupVersion
            );
          }

          return null;
        }
      }

      {
        int thresholdRevision;

        if ( !int.TryParse( elements[1], out thresholdRevision ) )
          return null;

        if ( thresholdRevision < 0 )
          return null;

        return thresholdRevision;
      }
    }
  }
}