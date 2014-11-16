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
using System.Net;
using System.IO;
using log4net;
using System.Xml;
using System.Threading;

namespace FlyTrace.LocationLib.ForeignAccess.Spot
{
  /// <summary>
  /// TODO: change comment, it's wrong at the moment.
  /// AsyncWebRequestState instanse can change during a single call to LocationRequest, each instance correponds to a single attempt.
  /// But its AsyncChainedState field reference the same instance, that doesn't change throuhout a single call to LocationRequest.
  /// In other words:
  /// "Call to LocationRequest" is one-to-many with "AsyncChainedState"
  /// "Call to LocationRequest" is one-to-many with "call to call to WebRequest"
  /// "Call to WebRequest" is one-to-one with "AsyncWebRequestState"
  /// </summary>
  public class SpotFeedRequest
  {
    private const int ChunkSize = 0x200; // Should be enough to read the first <message> element. If not, it will try to read more.

    /// <summary>If point is older than number of hours defined by this const, it's ignored, unless it's the newest point.</summary>
    private const int FullTrackPointAgeToIgnore = 12;

    private readonly string trackerForeignId;

    public readonly FeedKind FeedKind;

    private readonly long callId;

    public SpotFeedRequest( FeedKind feedKind, string trackerForeignId, long callId )
    {
      this.FeedKind = feedKind;
      this.trackerForeignId = trackerForeignId;
      this.callId = callId;
    }

    private readonly string testXml;

    public SpotFeedRequest( FeedKind feedKind, string trackerForeignId, string testXml, long callId )
    {
      this.FeedKind = feedKind;
      this.trackerForeignId = trackerForeignId;
      this.callId = callId;
      this.testXml = testXml;
    }

    private WebRequest webRequest;

    private readonly MemoryStream bufferStream = new MemoryStream( ChunkSize );

    private int bufferedDataLength;

    private static readonly ILog Log = LogManager.GetLogger( "TDM.FeedReq" );

    public IAsyncResult BeginRequest( AsyncCallback callback, object state )
    {
      AsyncChainedState<TrackerState> asyncChainedState = new AsyncChainedState<TrackerState>( callback, state );

      if ( this.testXml != null )
      { // it's a debug call
        byte[] bytes = Encoding.UTF8.GetBytes( this.testXml );
        this.bufferStream.Write( bytes, 0, bytes.Length );

        TrackerState parseResult = AnalyzeCurrentBuffer( );
        asyncChainedState.SetAsCompleted( parseResult );
        return asyncChainedState.FinalAsyncResult;
      }

      if ( this.webRequest != null )
      {
        throw new InvalidOperationException( "Cannot call BeginRequest twice on the same instance" );
      }

      string url;
      switch ( FeedKind )
      {
        case FeedKind.Feed_1_0:
          url =
            string.Format(
              @"http://share.findmespot.com/messageService/guestlinkservlet?glId={0}",
              this.trackerForeignId
            );
          break;

        case FeedKind.Feed_1_0_undoc:
          url =
            string.Format(
              @"http://share.findmespot.com/spot-adventures/rest-api/1.0/public/feed/{0}/message",
              this.trackerForeignId
            );
          break;

        case FeedKind.Feed_2_0:
          url =
            string.Format(
              @"https://api.findmespot.com/spot-main-web/consumer/rest-api/2.0/public/feed/{0}/message.xml",
              this.trackerForeignId
            );
          break;

        default:
          throw new ApplicationException( "Unknown request destination " + FeedKind );
      }

      this.webRequest = WebRequest.Create( url );

      if ( Log.IsDebugEnabled )
        Log.Debug( string.Format( "Request created for {0}, lrid {1}", this.trackerForeignId, this.callId ) );

      // needed for undocumented url, otherwise returns JSON:
      ( ( HttpWebRequest ) this.webRequest ).Accept = "text/html,application/xhtml+xml,application/xml";

      if ( AsyncResultNoResult.DefaultEndWaitTimeout > 0 )
      {
        this.webRequest.Timeout = Math.Max( 20000, AsyncResultNoResult.DefaultEndWaitTimeout - 5000 );
      }

      try
      {
        //if ( this.FeedKind == LocationLib.FeedKind.Feed_2_0 )
        //  throw new ApplicationException( "4444" );

        if ( Log.IsDebugEnabled )
          Log.Debug( string.Format( "Initiating BeginRequest for {0}, {1}, lrid {2}...", this.trackerForeignId, this.FeedKind, this.callId ) );
        this.webRequest.BeginGetResponse( GetResponseCallback, asyncChainedState );
      }
      catch ( Exception exc )
      {
        SetAsCompletedAndCloseRequest( asyncChainedState, new TrackerState( exc.Message, FeedKind.ToString( ) ) );
        Log.ErrorFormat( "BeginRequest throwed an error for {0}, {1}, lrid {2}: {3}", this.trackerForeignId, this.FeedKind, this.callId, exc.Message );
      }

      return asyncChainedState.FinalAsyncResult;
    }

    private void SetAsCompletedAndCloseRequest( AsyncChainedState<TrackerState> asyncChainedState, TrackerState result )
    {
      if ( asyncChainedState == null )
      {
        Log.ErrorFormat(
          "asyncChainedState is null for {0}, {1}, lrid {2} and result {3}",
          this.trackerForeignId,
          this.FeedKind,
          this.callId,
          result
        );
      }
      else
      {
        try
        {
          if ( asyncChainedState.FinalAsyncResult.IsCompleted )
          {
            Log.WarnFormat( "Discarding result for {0}, {1}, lrid {2} because the result is already completed. The discarding result is: {3}",
              this.trackerForeignId,
              this.FeedKind,
              this.callId,
              result
            );
          }
          else
          {
            asyncChainedState.SetAsCompleted( result );
          }
        }
        catch ( Exception exc )
        { // Catch to prevent throwing "you can set result only once" error - we don't need it out.
          Log.Error( "SetAsCompletedAndCloseRequest", exc );
        }
      }

      // MemoryBarrier so calls in this method can't be reordered. First it's completed, then webRequests etc closed and set to null.
      // As result, if exception occurs in any async handler here because requests was closed, it means that the result has been already set.
      Thread.MemoryBarrier( );

      SafelyCloseRequest( );
    }

    public TrackerState EndRequest( IAsyncResult ar )
    {
      AsyncResult<TrackerState> asyncResultImpl = ( AsyncResult<TrackerState> ) ar;

      return asyncResultImpl.EndInvoke( );
    }

    private WebResponse webResponse;

    private Stream responseStream;

    private void GetResponseCallback( IAsyncResult ar )
    {
      Tools.ConfigureThreadCulture( );

      var asyncChainedState = ( AsyncChainedState<TrackerState> ) ar.AsyncState;

      try
      {
        //if ( this.FeedKind == LocationLib.FeedKind.Feed_1_0_undoc )
        //  throw new ApplicationException( "5555" );

        if ( Log.IsDebugEnabled )
          Log.Debug( string.Format( "In GetResponseCallback for {0}, {1}, lrid {2}...", trackerForeignId, this.FeedKind, this.callId ) );

        asyncChainedState.CheckSynchronousFlag( ar.CompletedSynchronously );

        WebRequest localWebRequest = this.webRequest;
        if ( localWebRequest == null )
          throw new OperationCanceledException( );

        this.webResponse = localWebRequest.EndGetResponse( ar );

        if ( Log.IsDebugEnabled )
          Log.Debug( string.Format( "Got WebResponse for {0}, {1}, lrid {2}...", this.trackerForeignId, this.FeedKind, this.callId ) );

        this.responseStream = this.webResponse.GetResponseStream( );

        if ( Log.IsDebugEnabled )
          Log.Debug( string.Format( "Got Stream for {0}, {1}, lrid {2}...", this.trackerForeignId, this.FeedKind, this.callId ) );

        ReadNextResponseChunk( asyncChainedState );
      }
      catch ( Exception exc )
      {
        Log.InfoFormat( "GetResponseCallback fail for {0}, {1}, lrid {2}: {3}", this.trackerForeignId, this.FeedKind, this.callId,
          Log.IsDebugEnabled ? exc.ToString( ) : exc.Message );

        SetAsCompletedAndCloseRequest( asyncChainedState, new TrackerState( exc.Message, FeedKind.ToString( ) ) );
      }
    }

    public void SafelyCloseRequest( AbortStat abortStat = null, ILog specialLog = null )
    {
      ILog logToUse = specialLog ?? Log;

      // don't want to use locks anywhere in this class to minimize a risk of deadlock

      if ( abortStat != null )
      {
        abortStat.Stage = 1;
        Thread.MemoryBarrier( );  // to make sure it's not moved lower than next.instruction
      }

      try
      {
        // this.webRequest can be set to null from another thread (this happened before).
        // So get it into the local var first:
        WebRequest localWebRequest = this.webRequest;
        Thread.MemoryBarrier( );

        if ( localWebRequest != null )
        {
          logToUse.DebugFormat( "webRequest.Abort starting for lrid {0}...", callId );
          localWebRequest.Abort( );
          logToUse.DebugFormat( "webRequest.Abort done for lrid {0}", callId );
        }
        this.webRequest = null;
      }
      catch ( Exception exc )
      {
        logToUse.ErrorFormat( "webRequest.Abort error for lrid {0}: {1}", callId, exc );
      }

      if ( abortStat != null )
      {
        Thread.MemoryBarrier( ); // to make sure it's not moved higher than prev.instruction
        abortStat.Stage = 2;
        Thread.MemoryBarrier( );  // to make sure it's not moved lower than next.instruction
      }

      try
      {
        // this.localResponseStream can be set to null from another thread (this happened before).
        // So get it into the local var first:
        Stream localResponseStream = this.responseStream;
        Thread.MemoryBarrier( );

        if ( localResponseStream != null )
        {
          logToUse.DebugFormat( "responseStream.Close starting for lrid {0}...", callId );
          localResponseStream.Close( );
          logToUse.DebugFormat( "responseStream.Close done for lrid {0}", callId );
        }
        this.responseStream = null;
      }
      catch ( Exception exc )
      {
        logToUse.ErrorFormat( "responseStream.Close error for lrid {0}: {1}", callId, exc );
      }

      if ( abortStat != null )
      {
        Thread.MemoryBarrier( ); // to make sure it's not moved higher than prev.instruction
        abortStat.Stage = 3;
        Thread.MemoryBarrier( );  // to make sure it's not moved lower than next.instruction
      }

      try
      {
        // this.webResponse can be set to null from another thread (this happened before).
        // So get it into the local var first:
        WebResponse localWebResponse = this.webResponse;
        Thread.MemoryBarrier( );

        if ( localWebResponse != null )
        {
          logToUse.DebugFormat( "webResponse.Close starting for lrid {0}...", callId );
          localWebResponse.Close( );
          logToUse.DebugFormat( "webResponse.Close done for lrid {0}", callId );
        }
        this.webResponse = null;
      }
      catch ( Exception exc )
      {
        logToUse.ErrorFormat( "webResponse.Close error for lrid {0}: {1}", callId, exc );
      }

      if ( abortStat != null )
      {
        Thread.MemoryBarrier( ); // to make sure it's not moved higher than prev.instruction
        abortStat.Stage = 4;
      }
    }

    private void ReadNextResponseChunk( AsyncChainedState<TrackerState> asyncChainedState )
    {
      if ( this.bufferStream.Capacity - bufferedDataLength < ChunkSize )
      {
        this.bufferStream.Capacity += ChunkSize;
      }

      // When the length is increased, the contents of the stream between the old and the new length are initialized to 
      // zeros (see MSDN). So we should increase that now up to capacity, 
      // otherwise the data that's going to be written there will be discarded later:
      this.bufferStream.SetLength( this.bufferStream.Capacity );

      Stream localResponseStream = this.responseStream;
      if ( localResponseStream == null )
        throw new OperationCanceledException( );

      if ( Log.IsDebugEnabled )
        Log.DebugFormat( "Starting reading the next chunk for {0}, {1}, lrid {2}...", this.trackerForeignId, this.FeedKind, this.callId );

      localResponseStream.BeginRead(
        this.bufferStream.GetBuffer( ),
        bufferedDataLength,
        ( int ) this.bufferStream.Length - this.bufferedDataLength,
        ResponseStreamReadCallback,
        asyncChainedState );
    }

    private void ResponseStreamReadCallback( IAsyncResult ar )
    {
      Tools.ConfigureThreadCulture( );

      try
      {
        AsyncChainedState<TrackerState> asyncChainedState = null;

        try
        {
          asyncChainedState = ( AsyncChainedState<TrackerState> ) ar.AsyncState;
          asyncChainedState.CheckSynchronousFlag( ar.CompletedSynchronously );

          Stream localResponseStream = this.responseStream;
          if ( localResponseStream == null )
            throw new OperationCanceledException( );

          int bytesRead = localResponseStream.EndRead( ar );
          if ( bytesRead != 0 )
          {
            this.bufferedDataLength += bytesRead;
            this.bufferStream.SetLength( bufferedDataLength );

            if ( Log.IsDebugEnabled )
              Log.DebugFormat(
                "Read {0} bytes just now for {1}, {2}, lrid {3}, read {4} bytes in total, reading next chunk...",
                bytesRead,
                this.trackerForeignId,
                this.FeedKind,
                this.callId,
                this.bufferedDataLength );

            ReadNextResponseChunk( asyncChainedState ); // read more data & try parse again.
          }
          else
          {
            this.bufferStream.SetLength( bufferedDataLength );
            TrackerState parseResult = AnalyzeCurrentBuffer( );

            if ( Log.IsDebugEnabled )
              Log.DebugFormat( "Result for {0}, {1}, lrid {2}: {3}", this.trackerForeignId, this.FeedKind, this.callId, parseResult );

            // Note that parseResult can actually be just an error message (see Tracker.Error)
            SetAsCompletedAndCloseRequest( asyncChainedState, parseResult );
          }
        }
        catch ( Exception exc )
        {
          Log.ErrorFormat(
            "ResponseStreamReadCallback for {0}, {1}, lrid {2} processing: {3}",
            this.trackerForeignId,
            this.FeedKind,
            this.callId,
            exc.Message
          );

          SetAsCompletedAndCloseRequest( asyncChainedState, new TrackerState( exc.Message, FeedKind.ToString( ) ) );
        }
      }
      catch ( Exception outerExc )
      {
        Log.Error( "Something really bad happened", outerExc );
      }
    }

    private class TrackPointDataTimeEqualityComparer : IEqualityComparer<Data.TrackPointData>
    {
      bool IEqualityComparer<Data.TrackPointData>.Equals( Data.TrackPointData x, Data.TrackPointData y )
      {
        return x.ForeignTime == y.ForeignTime;
      }

      int IEqualityComparer<Data.TrackPointData>.GetHashCode( Data.TrackPointData trackPointData )
      {
        return trackPointData.ForeignTime.GetHashCode( );
      }
    }

    private static readonly TrackPointDataTimeEqualityComparer TimeEqualityComparer =
      new TrackPointDataTimeEqualityComparer( );

    private TrackerState AnalyzeCurrentBuffer( )
    {
      // RandomError( );

      Log.DebugFormat( "Analyzing buffer for {0}, {1}, lrid {2}", this.trackerForeignId, this.FeedKind, this.callId );

      TrackerState result;

      List<Data.TrackPointData> messages = new List<Data.TrackPointData>( );

      bool isBadTrackerId = false;
      bool messageTagFound = false;

      string parseErrorMessage = null;
      try
      {
        this.bufferStream.Seek( 0, SeekOrigin.Begin );
        XmlReader xmlReader = XmlReader.Create( this.bufferStream );

        while ( true )
        { // Look for messages until they run out or until it founds that XML doc is not well formed and throws 
          // an exception.
          Data.TrackPointData message = ParseNextMessage( xmlReader, ref isBadTrackerId, ref messageTagFound );

          if ( isBadTrackerId )
            break;

          if ( message == null )
            break;

          bool shouldBreak;

          if ( messages.Count == 0 )
          {
            // If it's the only message, use it (we always return the newest point no matter what age it has). 
            shouldBreak = false;
          }
          else
          {
            // If there are messages already in the list, take N hours back from the latest one:
            DateTime threshold = messages[0].ForeignTime.AddHours( -FullTrackPointAgeToIgnore );

            shouldBreak = message.ForeignTime <= threshold;

            Log.Debug( "Point ForeignTime: " + message.ForeignTime.ToString( ) + ", shouldBreak: " + shouldBreak.ToString( ) );
          }

          if ( shouldBreak ) break;

          messages.Add( message );
        }
      }
      catch ( XmlException exc )
      {
        // This could happen only in case of a bad response from the server. Normally should not happen.
        Log.ErrorFormat(
          "Parsing server response: found {0} message(s) for {1}, {2}, lrid {3}, and encountered parsing error: {4}",
          messages.Count,
          this.trackerForeignId,
          this.FeedKind,
          this.callId,
          exc.Message
        );
        parseErrorMessage = exc.Message;
      }

      if ( isBadTrackerId )
      {
        result = new TrackerState( Data.ErrorType.BadTrackerId, FeedKind.ToString( ) );
      }
      else if ( messages.Count == 0 )
      {
        if ( parseErrorMessage != null )
          result = new TrackerState( parseErrorMessage, FeedKind.ToString( ) );
        else if ( messageTagFound )
          result = new TrackerState( Data.ErrorType.ResponseHasBadSchema, FeedKind.ToString( ) );
        else
          result = new TrackerState( Data.ErrorType.ResponseHasNoData, FeedKind.ToString( ) );
      }
      else
      { // we have some data

        // It should come ordered & distinct from the source, but order and unique values are vital for
        // later processing including JavaScript, so make it ABSOLUTELY sure it's in order & unique:

        var fullTrack =
          messages
          .Distinct( TimeEqualityComparer )
          .OrderByDescending( msg => msg.ForeignTime );

        result = new TrackerState( fullTrack, FeedKind.ToString( ) );
      }

      return result;
    }

    private Data.TrackPointData ParseNextMessage(
      XmlReader xmlReader,
      ref bool isBadTrackerId,
      ref bool messageTagFound
    )
    {
      Data.TrackPointData result = null;

      string messageTypeElementName;
      string dateTimeElementName;
      string posixTimeElementName;
      string messageContentElementName;

      switch ( this.FeedKind )
      {
        case FeedKind.Feed_1_0:
          messageTypeElementName = "messageType";
          posixTimeElementName = "timeInGMTSecond";
          dateTimeElementName = "timestamp";
          messageContentElementName = "messageDetail";
          break;

        case FeedKind.Feed_1_0_undoc:
          messageTypeElementName = "type";
          posixTimeElementName = "timeInSec";
          dateTimeElementName = "dateTime";
          messageContentElementName = "messageDetail";
          break;

        case FeedKind.Feed_2_0:
          messageTypeElementName = "messageType";
          posixTimeElementName = "unixTime";
          dateTimeElementName = "dateTime";
          messageContentElementName = "messageContent";
          break;

        default:
          throw new ApplicationException( "Unknown request destination " + this.FeedKind );
      }

      string locationType = null;
      double? lat = null;
      double? lon = null;
      DateTime? ts = null;
      string userMessage = null;


      while ( xmlReader.Read( ) )
      {
        if ( xmlReader.Name == "message" )
        {
          locationType = null;
          lat = null;
          lon = null;
          ts = null;
          userMessage = null;

          messageTagFound = true;
        }

        if ( xmlReader.Name == "error" && xmlReader.NodeType == XmlNodeType.Element )
        {
          isBadTrackerId = ProcessErrorTag( xmlReader );
          if ( isBadTrackerId )
            break;
        }

        if ( xmlReader.Name == "latitude" && xmlReader.NodeType == XmlNodeType.Element )
        {
          xmlReader.ReadStartElement( );
          lat = xmlReader.ReadContentAsDouble( );
        }

        if ( xmlReader.Name == "longitude" && xmlReader.NodeType == XmlNodeType.Element )
        {
          xmlReader.ReadStartElement( );
          lon = xmlReader.ReadContentAsDouble( );
        }

        if ( xmlReader.Name == messageTypeElementName && xmlReader.NodeType == XmlNodeType.Element )
        {
          xmlReader.ReadStartElement( );
          locationType = xmlReader.ReadContentAsString( );

          // The js script shows as SOS anything that is not an empty string, "TRACK", "OK", "TEST", or "CUSTOM".
          // SPOT service might returns many statuses, so convert them to either of the above:

          if ( locationType == "TEST" ||
               locationType == "STOP" ||
               locationType == "HELP-CANCEL" )
            locationType = "OK";

          if ( locationType == "EXTREME-TRACK" ||
               locationType == "UNLIMITED-TRACK" ||
               locationType == "NEWMOVEMENT" ||
               locationType == "POI" )
            locationType = "TRACK";
        }

        // There are two date/time fields avaialble that seems to be the same. There is no certainty that both always 
        // will be there, so try parse both and ignore another if one is successful.
        if ( !ts.HasValue &&
             xmlReader.Name == dateTimeElementName &&
             xmlReader.NodeType == XmlNodeType.Element )
        {
          try
          {
            xmlReader.ReadStartElement( );
            string timeValue = xmlReader.ReadContentAsString( );

            if ( Log.IsDebugEnabled )
              Log.DebugFormat( "Parsing XML date/time: {0} for {1}, {2}, lrid {3}", timeValue, this.trackerForeignId, this.FeedKind, this.callId );

            ts = ParseXmlDateTime( timeValue );

            if ( Log.IsDebugEnabled )
              Log.DebugFormat( "XML date/time parsed: {0} for {1}, {2}, lrid {3}", ts, this.trackerForeignId, this.FeedKind, this.callId );
          }
          catch ( Exception exc )
          {
            Log.ErrorFormat( "Can't parse date/time: {0} for {1}, {2}, lrid {3}", exc.Message, this.trackerForeignId, this.FeedKind, this.callId );
          }
        }

        // See comment for dateTimeElementName
        if ( !ts.HasValue &&
             xmlReader.Name == posixTimeElementName &&
             xmlReader.NodeType == XmlNodeType.Element )
        {
          try
          {
            xmlReader.ReadStartElement( );
            double posixTime = xmlReader.ReadContentAsDouble( );

            if ( Log.IsDebugEnabled )
              Log.DebugFormat
              (
                "Parsing POSIX date/time: {0} for {1}, {2}, lrid {3}",
                posixTime,
                this.trackerForeignId,
                this.FeedKind,
                this.callId
              );

            ts = ParsePosixTime( posixTime );

            if ( Log.IsDebugEnabled )
              Log.DebugFormat( "POSIX date/time parsed: {0} for {1}, {2}, lrid {3}", ts, this.trackerForeignId, this.FeedKind, this.callId );
          }
          catch ( Exception exc )
          {
            Log.ErrorFormat( "Can't parse date/time: {0} for {1}, {2}, lrid {3}", exc.Message, this.trackerForeignId, this.FeedKind, this.callId );
          }
        }

        // note that user message is not always here - only for OK or Custom types.
        // Even then it could be absent.
        if ( xmlReader.Name == messageContentElementName && xmlReader.NodeType == XmlNodeType.Element )
        {
          userMessage = xmlReader.ReadElementContentAsString( );
        }

        if ( lat.HasValue && lon.HasValue && ts.HasValue && locationType != null )
        {
          // Read to the end of current <message> tag:
          while ( !( xmlReader.Name == "message" && xmlReader.NodeType == XmlNodeType.EndElement ) )
          {
            // if userMessage hasn't been read yet, try to catch it:
            if ( xmlReader.Name == messageContentElementName && xmlReader.NodeType == XmlNodeType.Element )
              userMessage = xmlReader.ReadElementContentAsString( );

            if ( !xmlReader.Read( ) )
              break;
          }

          // either we have userMessage or not, create a result:
          result = new Data.TrackPointData( locationType, lat.Value, lon.Value, ts.Value, userMessage );

          break;
        }

      } // while ( xmlReader.Read( ) )

      return result;
    }

    public static DateTime ParseXmlDateTime( string timeValue )
    {
      if ( timeValue.Length > 5 )
      {
        // date & time come as  '2013-01-10T11:03:36+0000' from the Spot server, while W3C and .NET require ':' in the 
        // timezone suffix i.e. '2013-01-10T11:03:36+00:00'
        string trailing5 = timeValue.Substring( timeValue.Length - 5 );
        if (
              (
                trailing5[0] == '+' ||
                trailing5[0] == '-'
              ) &&
              char.IsDigit( trailing5[1] ) &&
              char.IsDigit( trailing5[2] ) &&
              char.IsDigit( trailing5[3] ) &&
              char.IsDigit( trailing5[4] ) )
        {
          timeValue = timeValue.Insert( timeValue.Length - 2, ":" );
        }
      }

      return XmlConvert.ToDateTime( timeValue, XmlDateTimeSerializationMode.Utc );
    }

    private static DateTime BasePosixDateTime = new DateTime( 1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc );

    public static DateTime ParsePosixTime( double posixTime )
    {
      return BasePosixDateTime.AddSeconds( posixTime );
    }

    private bool ProcessErrorTag( XmlReader xmlReader )
    {
      bool isBadTrackerId = false;

      try
      {
        string errorDescr;
        string noDataMsg;
        string wrongTrackerIdMsg;
        string feedNotActiveMsg;

        if ( this.FeedKind == FeedKind.Feed_1_0 )
        {
          errorDescr = xmlReader.ReadElementContentAsString( );

          // these are the messages that SPOT server return when different errors happen in SecondChance request:
          noDataMsg = "Error happened. No data returned";
          wrongTrackerIdMsg = "Wrong guestLinkId";
          feedNotActiveMsg = wrongTrackerIdMsg;
        }
        else
        { //
          if ( !xmlReader.ReadToDescendant( "text" ) )
          {
            throw new ApplicationException( "Can't find 'text' element" );
          }

          errorDescr = xmlReader.ReadElementContentAsString( );

          // these are the messages that SPOT server return when different errors happen in Unofficial request:
          noDataMsg = "No Messages to display";
          wrongTrackerIdMsg = "Feed Not Found";
          feedNotActiveMsg = "Feed Currently Not Active"; // occurs when tracker was deleted?
        }

        if ( errorDescr.Contains( noDataMsg ) )
        {
          // do nothing, the well-formed XML without data will be processed as ResponseHadNoData later.
        }
        else if ( errorDescr.Contains( wrongTrackerIdMsg ) ||
                  errorDescr.Contains( feedNotActiveMsg ) )
        {
          isBadTrackerId = true;
        }
        else
        {
          if ( this.FeedKind == FeedKind.Feed_1_0 )
          { // if a deleted page id is passed to Feed_1_0, it returns a bit of a crap, but it seems it contains this:
            isBadTrackerId = errorDescr.Contains( "Guest Link  Does not exist in database" );
          }

          if ( !isBadTrackerId )
          {
            // errorDescr != noDataMsg && errorDescr != wrongTrackerIdMsg

            // Don't really know what to do in this case, assume that the tracker id is OK and it's just NO DATA.
            // But log it as error
            Log.FatalFormat( "Unknown error message for call to {0}, {1}, lrid {2}: {3}",
              this.trackerForeignId,
              this.FeedKind,
              this.callId,
              errorDescr );
          }
        }
      }
      catch ( Exception exc )
      {
        Log.FatalFormat( "Exception during processing an error message for a call to {0}, {1}, lrid {2}: {3}",
          this.trackerForeignId,
          this.FeedKind,
          this.callId,
          exc.Message
        );
      }

      return isBadTrackerId;
    }
  }
}