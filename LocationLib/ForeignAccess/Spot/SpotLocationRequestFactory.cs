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
using System.Linq;
using System.Text;
using FlyTrace.LocationLib.Properties;
using log4net;

namespace FlyTrace.LocationLib.ForeignAccess.Spot
{
  public class SpotLocationRequestFactory : LocationRequestFactory
  {
    private readonly string appAuxLogFolder;

    private readonly ConsequentErrorsCounter consequentErrorsCounter;

    /// <summary>
    /// Spot requests factory constructor.
    /// </summary>
    /// <param name="appAuxLogFolder">It's where this class puts "*.succ.timestamp" files.
    /// That's needed for logging purposes. It could be null (no flag files then), or e.g. a value of 
    /// Path.Combine( HttpRuntime.AppDomainAppPath , "logs" ).</param>
    /// <param name="consequentErrorsCounter">
    /// A counter to check if request error should be reported as error or as warning. If the number of 
    /// consequent request errors is not reached parameter's Threshold value, it logged as Warning. Otherwise
    /// it's logged as Error. Can be null, in this case always logged as Error. Note that: 
    /// - a successful request sets this counter to zero.
    /// - ResponseHasNoData or BadTrackerId are ignored and not counted as neither fail nor success.
    /// </param>
    public SpotLocationRequestFactory( string appAuxLogFolder, ConsequentErrorsCounter consequentErrorsCounter )
    {
      this.appAuxLogFolder = appAuxLogFolder;
      this.consequentErrorsCounter = consequentErrorsCounter;
    }

    public override LocationRequest CreateRequest( RequestParams requestParams )
    {
      LocationRequest locationRequest =
        new SpotLocationRequest(
          requestParams,
          this.appAuxLogFolder,
          this.consequentErrorsCounter
        );

      locationRequest.ReadLocationFinished += locationRequest_ReadLocationFinished;

      return locationRequest;
    }

    private static readonly ILog Log = LogManager.GetLogger( "SLRF" );

    private readonly object statSync = new object( );

    private DateTime? succTime;

    private int feedCheckStat;

    private void locationRequest_ReadLocationFinished( LocationRequest locationRequest, TrackerState result )
    {
      if ( result.Error != null )
        return;

      try
      {
        lock ( this.statSync )
        {
          if ( this.succTime == null ||
               this.succTime.Value < result.CreateTime )
          {
            this.succTime = result.CreateTime;
          }
        }
      }
      catch ( Exception exc )
      { // don't really expect an exception here, but just to be on the safe side:
        LogManager.GetLogger( GetType( ) ).Error( exc );
      }
    }

    public override string GetStat( out bool isOk )
    {
      StringBuilder sb = new StringBuilder( );

      isOk = true;

      lock ( this.statSync )
      {
        sb.Append( "Succ.request:\t" );

        if ( this.succTime != null )
          sb.Append( Tools.GetAgeStr( succTime.Value, true ) );
        else
          sb.Append( "None" );
      }

      return sb.ToString( );
    }

    public override int MaxCallsInPack
    {
      get { return Settings.Default.SPOT_MaxCallsInPack; }
    }

    public override TimeSpan MinTimeFromPrevStart
    {
      get { return TimeSpan.FromMilliseconds( Settings.Default.SPOT_MinTimeFromPrevStart_Ms ); }
    }

    public override TimeSpan MinCallsGap
    {
      get { return TimeSpan.FromMilliseconds( Settings.Default.SPOT_MinCallsGap_Ms ); }
    }

    public override TimeSpan SameFeedHitInterval
    {
      get { return TimeSpan.FromSeconds( Settings.Default.SPOT_SameFeedHitInterval_Seconds ); }
    }

    public override void RequestFinished( LocationRequest locReq, bool isTimedOut )
    {
      if ( this.consequentErrorsCounter == null )
        return;

      if ( !isTimedOut )
      {
        this.consequentErrorsCounter.TimedOutRequestsCounter.Reset( );
      }
      else
      {
        bool shouldReportProblem;
        int consequentErrorsCount =
          this.consequentErrorsCounter.TimedOutRequestsCounter.Increment( out shouldReportProblem );

        string message =
          string.Format(
            "Location request hasn't finished in time for lrid {0}, tracker id {1}. That's a consequent time out #{2}",
            locReq.Lrid,
            locReq.Id,
            consequentErrorsCount );

        if ( shouldReportProblem )
          LocationRequest.ErrorHandlingLog.Error( message );
        else
          LocationRequest.ErrorHandlingLog.Info( message );
      }
    }
  }
}