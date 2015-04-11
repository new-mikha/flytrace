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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using log4net;

namespace FlyTrace.LocationLib.ForeignAccess.Spot
{
  /// <summary>
  /// Used to ask a foreign server for coordinates. Returns Tracker, that contains either Error or Location with FullTrack. 
  /// If requested in the constructor, it also fills Tracker.FullTrack field (if there is no Error). See Remarks 
  /// section for details.
  /// </summary>
  /// <remarks>
  /// There are two point time thresholds that define if a point would be included into the FullTrack or not.
  /// First one is passed into the constructor (see its description), another one is uncoditional 12 hours (defined by 
  /// FullTrackPointAgeToIgnoreUnconditionally const). The point has to be younger than BOTH thresholds to be included
  /// into the full track and Location.PrevPoint. At the same time, Location.CurrPoint is always filled if the newset 
  /// point returned by the foreign server, no matter what age it has.
  /// </remarks>
  public class SpotLocationRequest : LocationRequest
  {
    private readonly static ILog Log = LogManager.GetLogger( "TDM.LocReq" );

    private readonly string appAuxLogFolder;

    private readonly ConsequentErrorsCounter consequentErrorsCounter;

    /// <summary>
    /// </summary>
    /// <param name="id"></param>
    /// <param name="appAuxLogFolder">It's where this class puts "*.succ.timestamp" files.
    /// That's needed for logging purposes. It could be null  (no flag files then), or e.g. a value of 
    /// Path.Combine( HttpRuntime.AppDomainAppPath , "logs" ).
    /// </param>
    /// <param name="consequentErrorsCounter">
    /// A counter to check if request error should be reported as error or as warning. If the number of 
    /// consequent request errors is not reached parameter's Threshold value, it logged as Warning. Otherwise
    /// it's logged as Error. Can be null, in this case always logged as Error. Note that: 
    /// - a successful request sets this counter to zero.
    /// - ResponseHasNoData or BadTrackerId are ignored and not counted as neither fail nor success.
    /// </param>
    public SpotLocationRequest(
      RequestParams requestParams,
      string appAuxLogFolder,
      ConsequentErrorsCounter consequentErrorsCounter
    )
      : base( requestParams )
    {
      this.appAuxLogFolder = appAuxLogFolder;
      this.consequentErrorsCounter = consequentErrorsCounter;
    }

    public override string ForeignType
    {
      get { return ForeignId.SPOT; }
    }

    private readonly object sync = new object( );

    private bool inCall;

    // Can be written from different threads (in case of retry) so making it volatile. Probably there would be 
    // a bit too many volatile reads after that, but it shouldn't be a problem - not thousands of them per seconds anyway.
    private volatile SpotFeedRequest currentRequest;

    public override IAsyncResult BeginReadLocation( AsyncCallback callback, object state )
    {
      lock ( this.sync )
      {
        if ( this.inCall )
          throw new InvalidOperationException( "BeginReadLocation has already been called for this instance" );

        this.inCall = true;
      }

      var asyncChainedState = new AsyncChainedState<TrackerState>( callback, state );

      Lrid = asyncChainedState.Id;

      this.currentRequest = new SpotFeedRequest( this.Id, 0, asyncChainedState.Id );
      Log.InfoFormat( "Created request for {0}, lrid {1}", this.Id, asyncChainedState.Id );

      this.currentRequest.BeginRequest( SpotFeedRequestCallback, asyncChainedState );

      return asyncChainedState.FinalAsyncResult;
    }

    private bool isAborted;

    /// <summary>
    /// Normally this method should not be called. This only happens in cases like if SpotFeedRequest.ResponseStreamReadCallback 
    /// is not called and SpotFeedRequest fails to finish normally (such cases were observed)
    /// </summary>
    public override void SafelyAbortRequest( AbortStat abortStat )
    {
      // Once it's quite emergency mode, do everything to close it, taking that any kind of exception could happen here,
      // including those from race conditions.

      try
      {
        this.isAborted = true;
        Thread.MemoryBarrier( );

        if ( this.currentRequest != null )
        {
          this.currentRequest.SafelyCloseRequest( abortStat, ErrorHandlingLog );
        }
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "SafelyAbortRequest error for lrid {0}: {1}", Lrid, exc );
      }
    }

    private void SpotFeedRequestCallback( IAsyncResult ar )
    {
      Tools.ConfigureThreadCulture( );

      Thread.MemoryBarrier( );

      var asyncChainedState = ( AsyncChainedState<TrackerState> ) ar.AsyncState;

      try
      {
        TrackerState result = this.currentRequest.EndRequest( ar );

        if ( result.Error != null )
          LogFailedRequestEnd( asyncChainedState, result );

        bool needContinue = false;

        if ( result.Error == null ||
             result.Error.Type == Data.ErrorType.ResponseHasNoData )
        { // Might be there is a result from the prev.pages, so try to merge it:
          result = MergeWithExistingData( result, out needContinue );
          // If it was an error with ResponseHasNoData, but there were data from the prev.pages,
          // then now result has Error == null.
        }

        bool isSubsequentRequestStarted = false;

        if ( result.Error == null )
        {
          if ( !needContinue )
          {
            LogSuccRequestEnd( asyncChainedState );
          }
          else
          {
            this.currentRequest =
              new SpotFeedRequest(
                this.Id,
                this.currentRequest.Page + 1,
                asyncChainedState.Id
              );

            // If isAborted but we're here then "bad" (timed out?) request was finished and closed.
            if ( !this.isAborted )
            {
              this.currentRequest.BeginRequest( SpotFeedRequestCallback, asyncChainedState );
              isSubsequentRequestStarted = true;
            }
          }
        }

        if ( !isSubsequentRequestStarted )
          asyncChainedState.SetAsCompleted( result );
      }
      catch ( Exception exc )
      {
        asyncChainedState.SetAsCompleted( exc );
      }
    }

    private void LogSuccRequestEnd( AsyncChainedState<TrackerState> asyncChainedState )
    {
      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Call succeeded for {0}, lrid {1}", this.Id, asyncChainedState.Id );

      if ( this.appAuxLogFolder != null )
      {
        try
        { // we need an idea of how diff.feeds work without keeping log on. 
          // Creation of a timestamp file is not synced, because it's no problem when it fails - the next one will eventually succeed.
          string path = Path.Combine( this.appAuxLogFolder, "succ.timestamp" );
          File.WriteAllText( path, "" );
        }
        catch ( Exception exc )
        {
          Log.ErrorFormat( "SpotFeedRequestCallback for {0}, lrid {1}: {2}", this.Id, asyncChainedState.Id, exc );
        }
      }

      if ( this.consequentErrorsCounter != null )
        this.consequentErrorsCounter.RequestsErrorsCounter.Reset( );
    }

    private Data.TrackPointData[] prevPagesTrack;

    /// <summary>Max number of messages in the SPOT feed page</summary>
    private const int MaxFeedLength = 50;

    private TrackerState MergeWithExistingData( TrackerState result, out bool needContinue )
    {
      bool needUpdate = false;

      bool prevTrackExisted = this.prevPagesTrack != null;

      Data.TrackPointData[] mergedTrack;
      if ( this.prevPagesTrack == null )
      {
        if ( result.Position == null )
        { // this should be ResponseHasNoData - ok, it's really no data then.
          needContinue = false;
          return result;
        }

        mergedTrack = result.Position.FullTrack;
      }
      else
      {
        needUpdate = true;

        if ( result.Position == null ) // i.e. it's ResponseHasNoData for N'th page.
          mergedTrack = prevPagesTrack;
        else
          mergedTrack =
            this.prevPagesTrack
            .Union( result.Position.FullTrack )
            .OrderByDescending( point => point.ForeignTime )
            .Distinct( ) // e.g. page 2 can start with a message that is ending for page 1 (e.g. if a new point passed through to the SPOT server after page 1 retrieved)
            .ToArray( );
      }

      if ( mergedTrack.Length == 0 )
      { // should never happen because this method called for succ.request end only, but let's check anyway
        Log.ErrorFormat( "mergedResult.Length == 0" );
        needContinue = false;
        return result;
      }

      Data.TrackPointData oldestRequestedPoint = mergedTrack.Last( );

      Data.TrackPointData newestExistingTrackPoint = null;
      if ( RequestParams.ExistingTrack != null )
        newestExistingTrackPoint = RequestParams.ExistingTrack.FirstOrDefault( );

      if (
        // no pre-loaded track:
            newestExistingTrackPoint == null ||

            // there is a gap between just loaded and pre-loaded track - this hardly would happen but let's check:
            oldestRequestedPoint.ForeignTime > newestExistingTrackPoint.ForeignTime
        )
      {
        double mergedTrackTotalHours =
          ( mergedTrack.First( ).ForeignTime - mergedTrack.Last( ).ForeignTime ).TotalHours;

        needContinue =
          result.Error == null // check that it's not 50 messages on one page and "no data" on the next one.
          && result.Position.FullTrack.Length == MaxFeedLength  // check that page just loaded by the request is full - hence "result", not "mergedTrack"
          && mergedTrackTotalHours < LocationRequest.FullTrackPointAgeToIgnore
          && this.currentRequest.Page < 5; // we need to stop somewhere

        this.prevPagesTrack = mergedTrack;
      }
      else
      {
        needContinue = false;

        Data.TrackPointData[] mergedWithExisting =
          mergedTrack
          .Union( RequestParams.ExistingTrack )
          .OrderByDescending( point => point.ForeignTime )
          .Distinct( ) // tracks would mostly overlap, normally except the newest point (if it's there at all)
          .ToArray( );

        if ( mergedWithExisting.Length != mergedTrack.Length )
        {
          mergedTrack = mergedWithExisting;
          needUpdate = true;
        }
      }

      if ( needUpdate )
      {
        #region Logging only
        if ( result.Error != null
             &&
             (
              result.Error.Type != Data.ErrorType.ResponseHasNoData ||
              this.currentRequest.Page == 0 ||
              mergedTrack == null ||
              mergedTrack.Length != MaxFeedLength 
             )
           ) // should never happen because here TrackerState is after succ.request, but let's check
        {
          string trackStat;
          if ( mergedTrack == null )
            trackStat = "null";
          else if ( mergedTrack.Length == 0 )
            trackStat = "none";
          else
            trackStat = string.Format( "{0} ## {1} ## {2}", mergedTrack.Length, mergedTrack.First( ), mergedTrack.Last( ) );

          Log.ErrorFormat(
            "result.Error != null: {0} \\ {1}\r\nPage={2}\r\nprevTrackExisted={3}\r\nId2={4}\r\ntrack={5}",
            Id,
            result,
            this.currentRequest.Page,
            prevTrackExisted,
            this.currentRequest.TrackerForeignId,
            trackStat
          );
        }
        #endregion

        DateTime thresholdDateTime = mergedTrack.First( ).ForeignTime.AddHours( -FullTrackPointAgeToIgnore );

        result =
          new TrackerState(
            mergedTrack
              .Where( point => point.ForeignTime > thresholdDateTime ),
            result.Tag
          );
      }

      return result;
    }

    private void LogFailedRequestEnd( AsyncChainedState<TrackerState> asyncChainedState, TrackerState result )
    {
      if ( result.Error.Type == Data.ErrorType.ResponseHasNoData ||
           result.Error.Type == Data.ErrorType.BadTrackerId )
      {
        ErrorHandlingLog.DebugFormat(
          "Request for {0}, page {1}, lrid {2} failed: {3}",
          this.Id,
          this.currentRequest.Page,
          asyncChainedState.Id,
          result.Error );
      }
      else
      {
        bool shouldReportProblem = true;
        int consequentErrorsCount = 1;

        if ( this.consequentErrorsCounter != null )
        {
          consequentErrorsCount =
            this.consequentErrorsCounter.RequestsErrorsCounter.Increment( out shouldReportProblem );
        }

        string message =
          string.Format
          (
            "Request for {0}, page {1}, lrid {2} failed: {3}. That's a consequent request error #{4}",
            this.Id,
            this.currentRequest.Page,
            asyncChainedState.Id,
            result.Error,
            consequentErrorsCount
          );

        if ( shouldReportProblem )
          ErrorHandlingLog.Error( message );
        else
          ErrorHandlingLog.Info( message );
      }
    }

    protected override TrackerState EndReadLocationProtected( IAsyncResult ar )
    {
      lock ( this.sync )
      { // safe to do it before actual work of EndReadLocation
        if ( !this.inCall )
          throw new InvalidOperationException( "Corresponding BeginReadLocation hasn't been called, or EndReadLocation already called." );

        this.inCall = false;
      }

      try
      {
        AsyncResult<TrackerState> finalAsyncResult = ( AsyncResult<TrackerState> ) ar;

        TrackerState result = finalAsyncResult.EndInvoke( );

        Log.InfoFormat( "Got some result for {0}: {1}", this.Id, result );

        return result;
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Got error for {0}: {1}", this.Id, exc.Message );
        throw;
      }
    }
  }
}