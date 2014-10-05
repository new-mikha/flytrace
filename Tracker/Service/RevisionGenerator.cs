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
using System.IO;
using System.Text;

using log4net;

namespace FlyTrace.Service
{
  /// <summary>All of the public members are thread safe.</summary>
  public static class RevisionGenerator
  {
    private static readonly object sync = new object( );

    private static readonly ILog log = LogManager.GetLogger( "RevGen" );

    private static FileStream persistingFileStream;

    public static bool IsActive { get; private set; }

    private static int revision;

    private static readonly Encoding FileEncoding = Encoding.UTF8;

    private const string ClosedAck = "closed";

    public static int Revision
    {
      get
      {
        lock ( sync )
        {
          if ( !IsActive )
            throw new InvalidOperationException( "Non initialized" );

          int result = revision;

          return revision;
        }
      }
    }

    internal static bool TryGetCurrentRevision( out int result )
    {
      lock ( sync )
      {
        if ( !IsActive )
        {
          result = 0;
          return false;
        }

        result = revision;
        return true;
      }
    }


    /// <summary>
    /// Initializes revision generator. If false returned, it's initialised but revision hasn't been restored from the file
    /// so it's restarted from zero. If exception is thrown, it's not initialised and shouldn't be used. After a succeessful
    /// call to this it can't be called for a second time without calling Shutdown first.
    /// </summary>
    /// <param name="persistingFilePath"></param>
    /// <returns></returns>
    public static bool Init( string persistingFilePath, out string initWarnings )
    {
      lock ( sync )
      {
        if ( IsActive )
          throw new InvalidOperationException( "Already initialized" );

        try
        {
          bool result;

          try
          {
            persistingFileStream = File.Open( persistingFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read );

            // parse & check the file
            StreamReader sr = new StreamReader( persistingFileStream, FileEncoding );
            string line1 = sr.ReadLine( );
            string line2 = sr.ReadLine( );
            string rest = sr.ReadToEnd( );

            result =
              line1 != null &&
              line2 != null &&
              ( rest == null || rest.Trim( ) == "" ) &&
              int.TryParse( line1, out revision ) &&
              revision >= 0 &&
              line2 == ClosedAck;

            if ( result )
              initWarnings = null;
            else
              initWarnings = "Can't parse revgen file";

            persistingFileStream.Seek( 0, SeekOrigin.Begin );
            persistingFileStream.SetLength( 0 );
          }
          catch ( FileNotFoundException exc )
          {
            initWarnings = exc.Message;
            persistingFileStream = File.Open( persistingFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read );
            result = false;
          }

          StreamWriter sw = new StreamWriter( persistingFileStream, FileEncoding );
          sw.Write( revision );
          sw.Flush( );
          persistingFileStream.Flush( );

          IsActive = true;

          return result;
        }
        catch ( Exception exc )
        {
          log.Error( "Can't initialize revgen", exc );
          if ( persistingFileStream != null )
          {
            try
            {
              persistingFileStream.Close( );
            }
            catch ( Exception excClose )
            {
              log.Error( "Can't close revgen properly", excClose );
            }
            persistingFileStream = null;
          }

          throw;
        }
      }
    }

    public static int IncrementRevision( )
    {
      lock ( sync )
      {
        if ( !IsActive )
          throw new InvalidOperationException( );

        revision++;

        return revision;
      }
    }

    public static bool TryIncrementRevision( out int result )
    {
      lock ( sync )
      {
        if ( !IsActive )
        {
          result = 0;
          return false;
        }

        revision++;

        result = revision;

        return true;
      }
    }

    public static int Shutdown( )
    {
      lock ( sync )
      {
        if ( !IsActive )
          throw new InvalidOperationException( "Not initialized" );

        IsActive = false;

        if ( persistingFileStream != null )
        {
          try
          {
            persistingFileStream.Seek( 0, SeekOrigin.Begin );

            StreamWriter sw = new StreamWriter( persistingFileStream, FileEncoding );
            sw.WriteLine( revision );
            sw.Write( ClosedAck );
            sw.Flush( );

            persistingFileStream.Close( );
          }
          catch ( Exception excClose )
          {
            log.Error( "Can't close revgen properly", excClose );
            throw;
          }
          finally
          {
            persistingFileStream = null;
          }
        }

        return revision;
      }
    }
  }
}