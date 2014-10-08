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

using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.IO;
using FlyTrace.LocationLib;

namespace FlyTrace.Service.Test
{
  internal class TestSource
  {
    private TestSource( )
    {
    }

    public static TestSource Singleton = new TestSource( );

    public bool IsAutoUpdate = true;

    public volatile int PositionNumber;

    public List<TrackerName> GetTestGroup( out int groupVersion, out bool showUserMessages )
    {
      lock ( this.sync )
      {
        EnsureDataLoaded( );

        groupVersion = 1;

        List<TrackerName> result =
          (
            from name in this.sourceData.Keys
            select new TrackerName
            {
              ForeignId = new ForeignId( ForeignId.SPOT, TestIdPrefix + name ),
              Name = name
            }
          ).ToList( );

        showUserMessages = true;

        return result;
      }
    }

    public static readonly string TestIdPrefix = "FlyTraceTestId_";

    private readonly object sync = new object( );

    private SortedList<string, XDocument> sourceData;

    private void EnsureDataLoaded( )
    {
      if ( this.sourceData != null )
        return;

      string dataFolder = HttpContext.Current.Server.MapPath( "~/App_Data/test/" );

      string[] filesPaths = Directory.GetFiles( dataFolder, "*.xml" );

      this.sourceData = new SortedList<string, XDocument>( );

      foreach ( string filePath in filesPaths )
      {
        string fileName = Path.GetFileNameWithoutExtension( filePath );
        if ( fileName == null || fileName.StartsWith( "_" ) )
          continue;

        XDocument doc = XDocument.Load( filePath );
        this.sourceData.Add( fileName, doc );

        //DateTime[] timestamps =
        //  (
        //    from msg in doc.Root
        //      .Elements( "feedMessageResponse" )
        //      .Elements( "messages" )
        //      .Elements( "message" )
        //    select SpotFeedRequest.ParsePosixTime( ( double ) msg.Element( "unixTime" ) )
        //  ).ToArray( );
      }

      string emptyFeedFilePath = Path.Combine( dataFolder, "_empty.xml" );
      this.emptySource = XDocument.Load( emptyFeedFilePath );
    }

    private XDocument emptySource;

    internal string GetFeed( string trackerForeignId )
    {
      lock ( this.sync )
      {
        int positionNumber = PositionNumber;

        if ( positionNumber <= 0 )
          return GetEmtpyFeed( trackerForeignId );

        string name = trackerForeignId.Substring( TestIdPrefix.Length );

        /*  Take from each source one after another.
         *  Say PositionNumber=9 and source is S1, what would be a max number of
         *  elements (N) to take from the source?
         * 
         *  N   S1  S2  S3  S4
         *  1   .   .   .   .
         *  2   .   .   .   .
         *  3   .  
         *    
         *  For S1 it will be 3. Calc as: at least 9/4=2. And if 17%4 > (index of the 
         *  source, for S1 it's 0) then add 1. 17%4=1 > 0, so result is 2+1=3
         *  
         */

        XDocument source = this.sourceData[name];

        int nMaxFromThisSource =
          positionNumber / this.sourceData.Count;

        int nameIndex = this.sourceData.Keys.IndexOf( name );

        if ( positionNumber % this.sourceData.Count > nameIndex )
          nMaxFromThisSource += 1;

        if ( nMaxFromThisSource == 0 )
          return GetEmtpyFeed( trackerForeignId );

        XDocument doc = new XDocument( source.Declaration );

        doc.AddFirst( source.Root );

        IEnumerable<XElement> messages =
          doc.Root
            .Element( "feedMessageResponse" )
            .Element( "messages" )
            .Elements( "message" );

        var messagesArray = messages as XElement[] ?? messages.ToArray();
        int sourceMessagesCount = messagesArray.Count( );

        if ( nMaxFromThisSource < sourceMessagesCount )
        {
          XElement[] elementsToRemove =
            messagesArray
            .Take( sourceMessagesCount - nMaxFromThisSource )
            .ToArray( );

          elementsToRemove.Remove( );
        }

        return doc.ToString( );
      }
    }

    private string GetEmtpyFeed( string trackerForeignId )
    {
      XDocument doc = new XDocument( this.emptySource.Declaration );

      doc.AddFirst( this.emptySource.Root );

      doc
        .Element( "response" )
        .Element( "errors" )
        .Element( "error" )
        .Element( "description" )
        .Value += trackerForeignId;

      return doc.ToString( );
    }
  }
}