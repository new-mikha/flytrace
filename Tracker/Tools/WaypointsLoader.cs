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
using System.Web;
using System.Web.UI.WebControls;
using System.Xml;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FlyTrace.Tools
{
  public static class WaypointsLoader
  {
    /// <summary>
    /// Load waypoints from a file to the Waypoints table. If a waypoint already exists (matched by name) for,
    /// this event it's either replaced or skipped depending on <paramref name="replaceExistingWaypoints"/> value.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="fileUpload"></param>
    /// <param name="replaceExistingWaypoints">If true, existing waypoints (matched by name) replaced with values 
    /// from the uploaded file. Otherwise it's skipped.</param>
    public static int LoadWaypoints( int eventId, FileUpload fileUpload, bool replaceExistingWaypoints )
    {
      int updatedRows = 0;

      if ( !fileUpload.HasFile )
      {
        throw new ApplicationException( "No file loaded." );
      }

      string fileExtension = Path.GetExtension( fileUpload.FileName ).ToLower( );
      if ( fileExtension != ".kml" && fileExtension != ".cup" )
      {
        throw new ApplicationException( "File is not recognized. Use either Google Earth file (*.KML), or SeeYou waypoints file (*.CUP)" );
      }

      TrackerDataSetTableAdapters.WaypointTableAdapter adapter = new TrackerDataSetTableAdapters.WaypointTableAdapter( );
      TrackerDataSet.WaypointDataTable existingData = adapter.GetDataByEventId( eventId );

      TrackerDataSet.WaypointDataTable uploadedWaypoints;
      if ( fileExtension == ".kml" )
        uploadedWaypoints = LoadKml( fileUpload, eventId );
      else
        uploadedWaypoints = LoadCup( fileUpload, eventId );

      if ( uploadedWaypoints.Count > 0 )
      {
        // At the moment uploadedWaypoints has negative values (-1, -2, ...) in Id column, 
        // while existingData has real IDs from DB. To match them, use Name field as a key:
        uploadedWaypoints.SetNameKeyOnly( );
        existingData.SetNameKeyOnly( );

        existingData.Merge( uploadedWaypoints );
      }

      updatedRows = adapter.Update( existingData );

      return updatedRows;
    }

    private static TrackerDataSet.WaypointDataTable LoadCup( FileUpload fileUpload, int eventId )
    {
      // See http://download.naviter.com/docs/cup_format.pdf 
      // and http://forum.naviter.com/showthread.php/2577-Number-of-digits-after-decimal-point-for-Longitude-in-CUP-file

      TrackerDataSet.WaypointDataTable result = new TrackerDataSet.WaypointDataTable( );

      StreamReader reader = new StreamReader( fileUpload.FileContent );

      string line = null;
      try
      {
        bool isFirstLine = true;

        while ( ( line = reader.ReadLine( ) ) != null )
        {
          // Neither format spec (see above) nor the example we have say anything about 
          // empty line, but let's be flexible:
          if ( line.Trim( ) == "" ) continue;

          try
          {
            // See the link to the CUP format specification above:
            if ( line.StartsWith( "---" ) ) break; // this means that the waypoints section has ended

            string[] elements = line.Split( ',' );

            // "JONNY",JONNY,,3318.830S,14750.088E,227.0m,1,,,,"field next to Burrawang Rd"

            if ( elements.Length < 11 )
            {
              throw new ApplicationException( "Unexpected number of elements" );
            }

            string id = elements[1].Trim('"').Trim();
            string description = elements[0].Trim('"').Trim();
            if (id == "")
            {
              id = description;
            }
            
            double lat = GetCupCoord( elements[3], 'S', 'N', 90 );
            double lon = GetCupCoord( elements[4], 'W', 'E', 180 );

            {
              char charPrefix;
              int deg;
              int min;
              int minFraction;

              CoordControls.DegMin.CoordToDegMin( lat, 'S', 'N', out charPrefix, out deg, out min, out minFraction );
            }

            double alt = GetCupAlt( elements[5] );

            TrackerDataSet.WaypointRow row =
              result.AddWaypointRow( eventId, id, lat, lon, alt, description );

            if ( string.IsNullOrEmpty( description ) )
            {
              row.SetDescriptionNull( );
            }
          }
          catch
          {
            // The format spec (see above) doesn't say anything about the first line,
            // but the example we've got has it. So try to parse the first line, but it fails 
            // consider that it's header:
            if ( !isFirstLine ) throw;
            isFirstLine = false;
          }
        }
      }
      catch ( Exception exc )
      {
        if ( line != null )
        {
          if ( line.Length > 120 )
          {
            line = line.Remove( 118 ) + "...";
          }

          throw new ApplicationException(
            string.Format( "{0}\nProblem found in line:\n{1}", exc.Message, line ),
            exc );
        }
        else
          throw;
      }


      return result;
    }

    private static double GetCupAlt( string altStr )
    {
      string originalAltStr = altStr;

      altStr = altStr.Trim( );

      double factor;
      if ( altStr.EndsWith( "m", StringComparison.InvariantCultureIgnoreCase ) )
      {
        factor = 1.0;
      }
      else if ( altStr.EndsWith( "f", StringComparison.InvariantCultureIgnoreCase ) )
      {
        factor = 0.3048;
      }
      else
      {
        throw new ApplicationException(
         string.Format( "Elevation '{0}' doesn't end with a correct unit symbol ('m' or 'f')", altStr ) );
      }

      altStr = altStr.Remove( altStr.Length - 1, 1 );

      return Global.ToDouble( altStr ) * factor;
    }

    private static double GetCupCoord( string coordStr, char neg, char pos, int maxDeg )
    {
      string originalCoordStr = coordStr;

      coordStr = coordStr.Trim( );

      int sign;
      if ( coordStr.EndsWith( neg.ToString( ), StringComparison.InvariantCultureIgnoreCase ) )
      {
        sign = -1;
      }
      else if ( coordStr.EndsWith( pos.ToString( ), StringComparison.InvariantCultureIgnoreCase ) )
      {
        sign = 1;
      }
      else
      {
        throw new ApplicationException(
         string.Format( "Coordinate '{0}' doesn't end with a correct hemisphere symbol ('{1}' or '{2}')", coordStr, neg, pos ) );
      }

      // Let coordStr be "3318.830S" for the comments below:
      coordStr = coordStr.Remove( coordStr.Length - 1, 1 );

      double temp = Global.ToDouble( coordStr ); // temp = 3318.830 at the moment

      if ( temp < 0 )
        throw new ApplicationException(
          string.Format( "Invalid coordinate format in '{0}' (negative value)", originalCoordStr ) );

      double minFractions = temp - Math.Floor( temp ); // minFractions is 0.830

      // Minutes always use 2 digits, so divide by 100:
      temp = Math.Floor( temp ) / 100.0; // temp now is 33.18

      double deg = Math.Floor( temp ); // deg = 33.0
      if ( deg > maxDeg )
        throw new ApplicationException(
          string.Format( "Invalid coordinate format in '{0}' (degrees value too large)", originalCoordStr ) );

      double min = ( temp - deg ) * 100.0; // min = 18.0
      if ( ( min + minFractions ) > 60 )
        throw new ApplicationException(
          string.Format( "Invalid coordinate format in '{0}' (minutes value too large)", originalCoordStr ) );

      return sign * (
          deg + ( min + minFractions ) / 60.0
      );
    }

    private static TrackerDataSet.WaypointDataTable LoadKml( FileUpload fileUpload, int eventId )
    {
      TrackerDataSet.WaypointDataTable result = new TrackerDataSet.WaypointDataTable( );

      XmlReader reader = XmlReader.Create( fileUpload.FileContent );

      bool inPlacemark = false;

      string name = null;

      string description = null;

      double? lat = null;
      double? lon = null;
      double? alt = null;

      try
      {
        while ( reader.Read( ) )
        {
          if ( reader.Name == "Placemark" )
          {
            if ( reader.NodeType == XmlNodeType.Element )
            {
              inPlacemark = true;
              name = null;
              description = null;
              lat = null;
              lon = null;
              alt = null;
            }

            if ( reader.NodeType == XmlNodeType.EndElement )
            {
              if ( name == null || lat == null || lon == null || alt == null )
              { // description could be null
                throw new ApplicationException( "At least one requred value is absent" );
              }

              TrackerDataSet.WaypointRow existingRow =
                result.Where( r => r.Name.ToLower( ) == name.ToLower( ) ).FirstOrDefault( );

              if ( existingRow != null )
              {
                if ( lat != existingRow.Lat ||
                     lon != existingRow.Lon ||
                     alt != existingRow.Alt )
                {
                  throw new ApplicationException( "There are two rows with same name but different values in the file." );
                }
              }
              else
              {
                TrackerDataSet.WaypointRow row =
                  result.AddWaypointRow( eventId, name, lat.Value, lon.Value, alt.Value, description == null ? "" : description );

                if ( string.IsNullOrEmpty( description ) )
                {
                  row.SetDescriptionNull( );
                }
              }

              name = null;
              description = null;
              lat = null;
              lon = null;
              alt = null;
              inPlacemark = false;
            }
          }

          if ( inPlacemark && reader.NodeType == XmlNodeType.Element )
          {
            if ( reader.Name == "name" )
            {
              name = reader.ReadElementContentAsString( );
            }

            if ( reader.Name == "description" )
            {
              description = reader.ReadElementContentAsString( );
            }

            if ( reader.Name == "coordinates" )
            {
              if ( lat.HasValue || lon.HasValue || alt.HasValue )
              {
                throw new ApplicationException( "Got <coordinates>, but already have lat, or lon, or alt." );
              }

              string str = reader.ReadElementContentAsString( );
              string[] latAndLon = str.Split( ',' );
              try
              {
                lon = Global.ToDouble( latAndLon[0] );
                lat = Global.ToDouble( latAndLon[1] );
                alt = Global.ToDouble( latAndLon[2] );
              }
              catch ( Exception exc )
              {
                throw new ApplicationException(
                  string.Format( "{0}: {1}", latAndLon, exc.Message ),
                  exc );
              }
            }
          }
        }
      }
      catch ( Exception exc )
      {
        if ( name != null )
          throw new ApplicationException( string.Format( "Problem with {0}: {1}", name, exc.Message ), exc );
        else
          throw;
      }

      return result;
    }
  }
}