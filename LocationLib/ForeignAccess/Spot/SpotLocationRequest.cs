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
  // ReSharper disable InconsistentNaming (names are OK)
  public enum FeedKind
  {
    None,
    Feed_1_0,
    Feed_1_0_undoc,
    Feed_2_0
  };
  // ReSharper restore InconsistentNaming

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

    /// <summary>Requests for different destinations are executed in this order until any one succeeds or there 
    /// are no more unrequested destinations in this array.</summary>
    private readonly FeedKind[] attemptsOrder;

    public static readonly FeedKind[] DefaultAttemptsOrder =
      { 
        FeedKind.Feed_2_0

        // Discontinue using old feeds, so commented out:
        //,FeedKind.Feed_1_0_undoc
        //,FeedKind.Feed_1_0
      };

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
    /// <param name="attemptsOrder"></param>
    public SpotLocationRequest(
      string id,
      string appAuxLogFolder,
      ConsequentErrorsCounter consequentErrorsCounter,
      IEnumerable<FeedKind> attemptsOrder
    )
      : base( id )
    {
      this.appAuxLogFolder = appAuxLogFolder;
      this.consequentErrorsCounter = consequentErrorsCounter;

      FeedKind[] feedKindsArr = attemptsOrder as FeedKind[] ?? attemptsOrder.ToArray( );

      if ( attemptsOrder != null &&
           feedKindsArr.Any( fk => fk != FeedKind.None ) )
      {
        this.attemptsOrder = feedKindsArr;
      }
      else
      {
        this.attemptsOrder = DefaultAttemptsOrder;
      }
    }

    public override string ForeignType
    {
      get { return ForeignId.SPOT; }
    }

    /// <summary>Number of attempt for this request (where 0 is first attempt), corresponds to an element 
    /// in <see cref="attemptsOrder"/>.</summary>
    private volatile int iAttempt;

    private volatile FeedKind currentFeedKind;

    internal FeedKind CurrentFeedKind { get { return this.currentFeedKind; } }

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

      this.currentFeedKind = this.attemptsOrder[this.iAttempt];

      this.currentRequest = new SpotFeedRequest( this.currentFeedKind, this.Id, asyncChainedState.Id );
      Log.InfoFormat( "Created request for {0}, {1} lrid {2}", this.Id, this.currentRequest.FeedKind, asyncChainedState.Id );

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
          this.currentRequest.SafelyCloseRequest( abortStat, TimedOutRequestsLog );
        }
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "SafelyAbortRequest error for lrid {0}: {1}", Lrid, exc );
      }
    }

    private static bool IsWarnedAboutSuccRetry;

    private void SpotFeedRequestCallback( IAsyncResult ar )
    {
      var asyncChainedState = ( AsyncChainedState<TrackerState> ) ar.AsyncState;

      try
      {
        TrackerState result = this.currentRequest.EndRequest( ar );

        if ( result.Error == null )
        {
          string tsFileName = string.Format( "{0}.succ.timestamp", this.currentRequest.FeedKind );
          if ( Log.IsDebugEnabled )
            Log.DebugFormat( "Call succeeded for {0}, {1}, lrid {2}", this.Id, this.currentRequest.FeedKind, asyncChainedState.Id );

          if ( this.iAttempt != 0 )
          {
            string msg =
              string.Format(
                "Retry succeeded for {0}, {1}, lrid {2}",
                this.Id, this.currentRequest.FeedKind, asyncChainedState.Id );

            // Prevent many Warns because Warnings supposed to be emailed.
            if ( IsWarnedAboutSuccRetry )
            {
              Log.Info( msg );
            }
            else
            {
              Log.Warn( msg );
              IsWarnedAboutSuccRetry = true; // multiple threads can do that, but it's ok. Deliberately do not sync it.
            }
          }

          if ( this.appAuxLogFolder != null )
          {
            try
            { // we need an idea of how diff.feeds work without keeping log on. 
              // Creation of a timestamp file is not synced, because it's no problem when it fails - the next one will eventually succeed.
              string path = Path.Combine( this.appAuxLogFolder, tsFileName );
              File.WriteAllText( path, "" );
            }
            catch ( Exception exc )
            {
              Log.ErrorFormat( "SpotFeedRequestCallback for {0}, {1}, lrid {2}: {3}", this.Id, this.currentRequest.FeedKind, asyncChainedState.Id, exc );
            }
          }

          asyncChainedState.SetAsCompleted( result );

          if ( this.consequentErrorsCounter != null )
            this.consequentErrorsCounter.RequestsErrorsCounter.Reset( );
        }
        else
        {
          bool havingMoreAttempts =
            this.iAttempt < this.attemptsOrder.Length - 1 &&
              result.Error.Type != Data.ErrorType.BadTrackerId &&
              result.Error.Type != Data.ErrorType.ResponseHasNoData;

          if ( !havingMoreAttempts )
          {
            LogRequestError( asyncChainedState, result );

            asyncChainedState.SetAsCompleted( result );
          }
          else
          {
            this.iAttempt++;

            this.currentFeedKind = this.attemptsOrder[this.iAttempt];

            this.currentRequest = new SpotFeedRequest( this.currentFeedKind, this.Id, asyncChainedState.Id );

            Thread.MemoryBarrier( );
            // If isAborted but we're here then "bad" request was finished and closed.
            if ( !this.isAborted )
            {
              Log.WarnFormat( "Retrying using {0} for {1}, lrid {2} after an error: {3}", this.currentFeedKind, this.Id, asyncChainedState.Id, result.Error );

              this.currentRequest.BeginRequest( SpotFeedRequestCallback, asyncChainedState );
            }
          }
        }
      }
      catch ( Exception exc )
      {
        asyncChainedState.SetAsCompleted( exc );
      }
    }

    private void LogRequestError( AsyncChainedState<TrackerState> asyncChainedState, TrackerState result )
    {
      ILog log = LogManager.GetLogger( "TDM.LocReq.ErrorHandling" );
      if ( result.Error.Type == Data.ErrorType.ResponseHasNoData ||
           result.Error.Type == Data.ErrorType.BadTrackerId )
      {
        log.InfoFormat(
          "Request for {0}, lrid {1} failed: {2}",
          this.Id,
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
            "Request for {0}, lrid {1} failed: {2}. That's a consequent request error #{3}",
            this.Id,
            asyncChainedState.Id,
            result.Error,
            consequentErrorsCount
          );

        if ( shouldReportProblem )
          log.Error( message );
        else
          log.Info( message );
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

        Log.InfoFormat( "Got some result for {0} ({1}): {2}", this.Id, this.currentRequest.FeedKind, result );

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