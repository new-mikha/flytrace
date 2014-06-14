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
using log4net;

namespace FlyTrace.LocationLib.ForeignAccess.Spot
{
  public class SpotLocationRequestFactory : LocationRequestFactory
  {
    private readonly string appAuxLogFolder;

    public SpotLocationRequestFactory( string appAuxLogFolder )
    {
      this.appAuxLogFolder = appAuxLogFolder;
    }

    public override LocationRequest CreateRequest( ForeignId foreignId )
    {
      LocationRequest locationRequest =
        new SpotLocationRequest( foreignId, this.appAuxLogFolder, GetSanitizedAttemptsOrder( ) );

      locationRequest.ReadLocationFinished += locationRequest_ReadLocationFinished;

      return locationRequest;
    }

    private static ILog Log = LogManager.GetLogger( "SLRF" );

    /// <summary>
    /// Normally this.attemptsOrder should have all values from FeedKind enum except None. But it's very 
    /// important that it's always true, otherwise it could stuck with wrong feed kind(s) or even without 
    /// any at all. So check that these values are really there, and there is no garbage from any kind of,
    /// a bug, and do that synchronzing access to this.attemptsOrder.
    /// </summary>
    /// <returns></returns>
    private FeedKind[] GetSanitizedAttemptsOrder( )
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
          int totalCountOfAvailableFeeds = Enum.GetValues( typeof( FeedKind ) ).Length - 1;
          if ( result.Count != totalCountOfAvailableFeeds )
          {
            Log.ErrorFormat( "Attempts order list is not complete or has garbage inside ({0} values)", result.Count );
          }
          else
          {
            isOk = true;
          }
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

    public override LocationRequest CreateTestRequest( ForeignId foreignId, string testSource )
    {
      return new SpotLocationRequest( foreignId, testSource, FeedKind.Feed_2_0, this.appAuxLogFolder );
    }

    private object statSync = new object( );

    private readonly List<FeedKind> attemptsOrder =
      new List<FeedKind>( SpotLocationRequest.DefaultAttemptsOrder );

    private Dictionary<FeedKind, DateTime> feedsSuccTimes = new Dictionary<FeedKind, DateTime>( );

    private int feedCheckStat;

    private Dictionary<FeedKind, int> feedsSuccStats = new Dictionary<FeedKind, int>( );

    private void locationRequest_ReadLocationFinished( LocationRequest locationRequest, TrackerState result )
    {
      if ( result.Error != null )
        return;

      try
      {
        FeedKind feedKind = ( ( SpotLocationRequest ) locationRequest ).CurrentFeedKind;

        DateTime otherFeedSuccTime;

        lock ( this.statSync )
        {
          if ( !this.feedsSuccTimes.TryGetValue( feedKind, out otherFeedSuccTime ) ||
               otherFeedSuccTime < result.RefreshTime )
          {
            feedsSuccTimes[feedKind] = result.RefreshTime;
          }

          UpdateAttemptsOrder( feedKind );
        }
      }
      catch ( Exception exc )
      { // don't really expect an exception here, but just to be on the safe side:
        log4net.LogManager.GetLogger( GetType( ) ).Error( exc );
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
          Log.Warn( "Log stat for the moment: " + logString );
          this.attemptsOrder.RemoveAll( fk => fk == bestFeed );
          this.attemptsOrder.Insert( 0, bestFeed );
          Log.WarnFormat( "Best feed was {0}, now it's {1}", prevBestFeed, bestFeed );
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
  }
}