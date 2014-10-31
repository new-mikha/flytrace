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
using System.Xml.Linq;
using System.IO;

namespace FlyTrace.LocationLib
{
  public class TestSource
  {
    public static TestSource Singleton { get; private set; }

    private static readonly object Sync = new object( );

    public static void Initialize( string testDataPath )
    {
      // System.Web.Hosting.HostingEnvironment.MapPath( "~/App_Data/test/" );
      lock ( Sync )
      {
        if ( Singleton != null )
          throw new InvalidOperationException( "TestSource is already initialized." );

        Singleton = new TestSource( testDataPath );
      }
    }

    private TestSource( string testDataPath )
    {
      string[] filesPaths = Directory.GetFiles( testDataPath, "*.xml" );

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

      string emptyFeedFilePath = Path.Combine( testDataPath, "_empty.xml" );
      this.emptySource = XDocument.Load( emptyFeedFilePath );
    }

    public bool IsAutoUpdate = true;

    public volatile int PositionNumber;

    public string[] GetTestNames( )
    {
      lock ( Sync )
      {
        return this.sourceData.Keys.ToArray( );
      }
    }

    public static readonly string TestIdPrefix = "FlyTraceTestId_";

    private readonly SortedList<string, XDocument> sourceData;

    private readonly XDocument emptySource;

    internal string GetFeed( string trackerForeignId )
    {
      lock ( Sync )
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

        // ReSharper disable PossibleNullReferenceException
        IEnumerable<XElement> messages =
          doc.Root
            .Element( "feedMessageResponse" )
            .Element( "messages" )
            .Elements( "message" );
        // ReSharper restore PossibleNullReferenceException

        var messagesArray = messages as XElement[] ?? messages.ToArray( );
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

      // ReSharper disable PossibleNullReferenceException
      doc
        .Element( "response" )
        .Element( "errors" )
        .Element( "error" )
        .Element( "description" )
        .Value += trackerForeignId;
      // ReSharper restore PossibleNullReferenceException

      return doc.ToString( );
    }
  }
}