﻿/******************************************************************************
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

    public override LocationRequest CreateRequest( string foreignId )
    {
      LocationRequest locationRequest =
        new SpotLocationRequest( foreignId, this.appAuxLogFolder, this.consequentErrorsCounter, GetSanitizedAttemptsOrder( ) );

      locationRequest.ReadLocationFinished += locationRequest_ReadLocationFinished;

      return locationRequest;
    }

    private static readonly ILog Log = LogManager.GetLogger( "SLRF" );

    /// <summary>
    /// Normally this.attemptsOrder should have all values from FeedKind enum except None. But it's very 
    /// important that it's always true, otherwise it could stuck with wrong feed kind(s) or even without 
    /// any at all. So check that these values are really there, and there is no garbage from any kind of,
    /// a problem, and do that synchronzing access to this.attemptsOrder.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<FeedKind> GetSanitizedAttemptsOrder( )
    {
      List<FeedKind> result;
      lock ( this.statSync )
      {
        result = new List<FeedKind>( this.attemptsOrder );
      }

      bool isOk = false;
      if ( result.Count == 0 )
      {
        Log.Error( "Attempts order list is empty" );
      }
      else if ( result.Contains( FeedKind.None ) )
      {
        Log.Error( "Attempts order list contains None" );
      }
      else
      {
        int distinctCount = result.Distinct( ).Count( );
        if ( result.Count != distinctCount )
        {
          Log.ErrorFormat(
            "Attempts order list contains non-unique values ({0} total and {1} unique)",
            result.Count,
            distinctCount
          );
        }
        else
        {
          // decomissioned all old feed kinds, so this is not needed anymore
          //int totalCountOfAvailableFeeds = Enum.GetValues( typeof( FeedKind ) ).Length - 1;
          //if ( result.Count != totalCountOfAvailableFeeds )
          //{
          //  Log.ErrorFormat( "Attempts order list is not complete or has garbage inside ({0} values)", result.Count );
          //}
          //else
          //{
            isOk = true;
          //}
        }
      }

      if ( !isOk )
      {
        result = new List<FeedKind>( SpotLocationRequest.DefaultAttemptsOrder );
        lock ( this.statSync )
        {
          this.attemptsOrder.Clear( );
          this.attemptsOrder.AddRange( SpotLocationRequest.DefaultAttemptsOrder );
        }
      }

      return result.ToArray( );
    }

    private readonly object statSync = new object( );

    private readonly List<FeedKind> attemptsOrder =
      new List<FeedKind>( SpotLocationRequest.DefaultAttemptsOrder );

    private readonly Dictionary<FeedKind, DateTime> feedsSuccTimes = new Dictionary<FeedKind, DateTime>( );

    private int feedCheckStat;

    private readonly Dictionary<FeedKind, int> feedsSuccStats = new Dictionary<FeedKind, int>( );

    private void locationRequest_ReadLocationFinished( LocationRequest locationRequest, TrackerState result )
    {
      if ( result.Error != null )
        return;

      try
      {
        FeedKind feedKind = ( ( SpotLocationRequest ) locationRequest ).CurrentFeedKind;

        lock ( this.statSync )
        {
          DateTime otherFeedSuccTime;
          if ( !this.feedsSuccTimes.TryGetValue( feedKind, out otherFeedSuccTime ) ||
               otherFeedSuccTime < result.CreateTime )
          {
            feedsSuccTimes[feedKind] = result.CreateTime;
          }

          UpdateAttemptsOrder( feedKind );
        }
      }
      catch ( Exception exc )
      { // don't really expect an exception here, but just to be on the safe side:
        LogManager.GetLogger( GetType( ) ).Error( exc );
      }
    }

    private void UpdateAttemptsOrder( FeedKind feedKind )
    {
      {
        int feedStat;
        this.feedsSuccStats.TryGetValue( feedKind, out feedStat );
        this.feedsSuccStats[feedKind] = feedStat + 1;

        this.feedCheckStat++;
      }

      if ( this.feedCheckStat < 100 )
        return;

      string logString = "";
      FeedKind defaultFeed = SpotLocationRequest.DefaultAttemptsOrder[0];
      int bestStat = 0;
      FeedKind bestFeed = FeedKind.None;
      foreach ( KeyValuePair<FeedKind, int> kvpFeedStat in this.feedsSuccStats )
      {
        if ( logString != "" )
          logString += ", ";
        logString += string.Format( "{0}/{1}", kvpFeedStat.Key, kvpFeedStat.Value );

        if ( kvpFeedStat.Value > bestStat )
        {
          bestStat = kvpFeedStat.Value;
          bestFeed = kvpFeedStat.Key;
        }
        else if ( kvpFeedStat.Value == bestStat &&
                  kvpFeedStat.Key == defaultFeed )
        {
          bestFeed = defaultFeed;
        }
      }

      if ( bestFeed == FeedKind.None )
      { // Should not happen, but let's check for that too. No need to uodate this.attemptsOrder because
        // GetSanitizedAttemptsOrder will return correct sequence anyway.
        Log.ErrorFormat(
          "NONE obtained as \"best feed\", total number value in feedsSuccStats is {0}",
          this.feedsSuccStats.Count
        );
      }
      else
      {
        FeedKind prevBestFeed = this.attemptsOrder.FirstOrDefault( );
        if ( bestFeed != prevBestFeed )
        {
          Log.Info( "Log stat for the moment: " + logString );
          this.attemptsOrder.RemoveAll( fk => fk == bestFeed );
          this.attemptsOrder.Insert( 0, bestFeed );
          Log.InfoFormat( "Best feed was {0}, now it's {1}", prevBestFeed, bestFeed );
        }
      }

      this.feedsSuccStats.Clear( );
      this.feedCheckStat = 0;
    }

    public override string GetStat( out bool isOk )
    {
      StringBuilder sb = new StringBuilder( );

      lock ( this.statSync )
      {
        for ( int i = 0; i < this.attemptsOrder.Count; i++ )
        {
          if ( i > 0 )
            sb.AppendLine( );

          FeedKind feedKind = this.attemptsOrder[i];
          sb.AppendFormat( "{0}:\t", feedKind.ToString( ) );

          DateTime dtSucc;
          if ( this.feedsSuccTimes.TryGetValue( feedKind, out dtSucc ) )
            sb.Append( Tools.GetAgeStr( dtSucc, true ) );
          else
            sb.Append( "None" );
        }

        isOk =
          this.attemptsOrder.Count > 0 &&
          this.attemptsOrder[0] == SpotLocationRequest.DefaultAttemptsOrder[0];
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
  }
}