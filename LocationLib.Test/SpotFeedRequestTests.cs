using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using FlyTrace.LocationLib;
using FlyTrace.LocationLib.Data;
using FlyTrace.LocationLib.ForeignAccess.Spot;
using Xunit;
using Xunit.Abstractions;

namespace LocationLib.Test
{
  public class SpotFeedRequestTests
  {
    public SpotFeedRequestTests(ITestOutputHelper output)
    {
      _output = output;
    }

    [Fact]
    public void BasicTest()
    {
      TrackerState trackerState = Req("BasicRequest.xml");

      Assert.Equal(2, trackerState.Position.FullTrack.Length);

      Assert.Equal("OK", trackerState.Position.CurrPoint.LocationType);
      Assert.Equal(1, trackerState.Position.CurrPoint.Latitude);
      Assert.Equal(2, trackerState.Position.CurrPoint.Longitude);

      Assert.Equal("TRACK", trackerState.Position.PreviousPoint.LocationType);
      Assert.Equal(3, trackerState.Position.PreviousPoint.Latitude);
      Assert.Equal(4, trackerState.Position.PreviousPoint.Longitude);

      Assert.True(ReferenceEquals(trackerState.Position.CurrPoint, trackerState.Position.FullTrack[0]));
      Assert.True(ReferenceEquals(trackerState.Position.PreviousPoint, trackerState.Position.FullTrack[1]));

      Assert.True(trackerState.Position.CurrPoint.ForeignTime > trackerState.Position.PreviousPoint.ForeignTime);
    }

    [Fact]
    public void CombinationsTest()
    {
      const string resourceName = "Combinations.xml";
      string xml = GetResourceAsString(resourceName);
      XDocument doc = XDocument.Load(new StringReader(xml));
      IEnumerable<XElement> messageElements = doc.XPathSelectElements("/response/feedMessageResponse/messages/message");
      int messagesCount = messageElements.Count();

      TrackerState trackerState = Req(resourceName);

      // Some of the elements are intentionally not valid in the test XML:
      Assert.Equal(messagesCount - 4, trackerState.Position.FullTrack.Length);

      DateTime? time = null;
      for (var iMessage = 0; iMessage < trackerState.Position.FullTrack.Length; iMessage++)
      {
        TrackPointData pointData = trackerState.Position.FullTrack[iMessage];
        try
        {
          if (time == null)
          {
            time = pointData.ForeignTime;
          }

          double latitude = pointData.Latitude;
          Assert.True((int)latitude == 1 || (int)latitude == 2 || (int)latitude == 11 || (int)latitude == 12);

          double longitude = pointData.Longitude;
          Assert.True((int)longitude == 20 || (int)longitude == 21);

          Assert.Equal(DateTimeKind.Utc, pointData.ForeignTime.Kind);
          Assert.Equal(time, pointData.ForeignTime);

          bool shouldHaveMessage = (int)latitude == 1 || (int)latitude == 11;
          bool shouldHaveAltitude = (int)latitude == 11 || (int)latitude == 12;
          bool isOk = (int)longitude == 20;
          bool isTrack = (int)longitude == 21;

          time = time.Value.AddMinutes(-10);

          if (shouldHaveAltitude)
            Assert.Equal(123, pointData.Altitude);
          else
            Assert.Null(pointData.Altitude);

          if (isTrack)
            Assert.Equal("TRACK", pointData.LocationType);

          if (isOk)
            Assert.Equal("OK", pointData.LocationType);

          if (shouldHaveMessage)
            Assert.Equal("Need beer and retrieve", pointData.UserMessage);
          else
            Assert.Null(pointData.UserMessage);
        }
        catch
        {
          _output.WriteLine($"Problem with message #{iMessage}");
          throw;
        }
      }
    }

    private static TrackerState Req(string resourceName)
    {
      string xml = ProcessTestXml(resourceName);
      var request = new SpotFeedRequest("test", xml, 1);

      TrackerState result = null;

      request.BeginRequest(asyncResult =>
      {
        result = request.EndRequest(asyncResult);
      }, null);

      Assert.NotNull(result);

      return result;
    }

    private static string GetResourceAsString(string resourceName)
    {
      Assembly assembly = Assembly.GetExecutingAssembly();

      using (Stream stream = assembly.GetManifestResourceStream("LocationLib.Test." + resourceName))
      {
        if (stream == null)
          throw new FileNotFoundException($"Cannot find resource '{resourceName}'");

        using (StreamReader reader = new StreamReader(stream))
        {
          return reader.ReadToEnd();
        }
      }
    }

    private readonly ITestOutputHelper _output;


    //[Fact]
    public void ReplaceTestXml()
    {
      _output.WriteLine(ProcessTestXml("Combinations.xml"));
    }


    // Utility method to make date-time in test XML going backwards properly with each message
    private static string ProcessTestXml(string resourceName)
    {
      string xml = GetResourceAsString(resourceName);
      XDocument doc = XDocument.Load(new StringReader(xml));
      IEnumerable<XElement> messageElements = doc.XPathSelectElements("/response/feedMessageResponse/messages/message");

      DateTime? reference = null;

      foreach (XElement messageElement in messageElements)
      {
        if (messageElement.Element("id")?.Value == "wrong data")
          continue;

        DateTime? unixTime = null;
        DateTime? xmlTime = null;

        ProcessUnixTime(messageElement, ref unixTime, ref reference);

        ProcessXmlDateTime(messageElement, ref xmlTime, ref reference);

        Assert.True(unixTime != null || xmlTime != null);

        if (unixTime != null && xmlTime != null)
        {
          Assert.Equal(unixTime.Value, xmlTime.Value);
        }

        Assert.NotNull(reference);

        reference = reference.Value.AddMinutes(-10);
      }

      return doc.ToString();
    }

    private static void ProcessXmlDateTime(XElement messageElement, ref DateTime? xmlTime, ref DateTime? reference)
    {

      XElement xmlTimeElement = messageElement.Element("dateTime");
      if (xmlTimeElement != null)
      {
        xmlTime = SpotFeedRequest.ParseXmlDateTime(xmlTimeElement.Value);

        Assert.Equal(xmlTimeElement.Value, ConvertTimeToXmlString(xmlTime.Value));

        if (reference == null)
        {
          reference = xmlTime;
        }

        xmlTimeElement.Value = ConvertTimeToXmlString(reference.Value);
      }

    }

    private static void ProcessUnixTime(XElement messageElement, ref DateTime? unixTime, ref DateTime? reference)
    {

      XElement unixTimeElement = messageElement.Element("unixTime");
      if (unixTimeElement != null)
      {
        double unixTimeDouble = double.Parse(unixTimeElement.Value);
        unixTime = SpotFeedRequest.ParsePosixTime(unixTimeDouble);

        Assert.Equal(unixTimeElement.Value, ConvertTimeToUnixTime(unixTime.Value));
        if (reference == null)
        {
          reference = unixTime;
        }

        unixTimeElement.Value = ConvertTimeToUnixTime(reference.Value);
      }
    }

    private static string ConvertTimeToUnixTime(DateTime value)
    {
      return ((long)(value - SpotFeedRequest.BasePosixDateTime).TotalSeconds).ToString();
    }

    private static string ConvertTimeToXmlString(DateTime value)
    {
      return value.ToString("yyyy-MM-ddTHH:mm:ss+0000");
    }
  }
}
