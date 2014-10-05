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
using System.Web;
using System.Threading;
using System.IO;
using log4net;
using System.Text;

namespace FlyTrace.Service
{
  /// <summary>All members are thread UNSAFE.</summary>
  public class RevisionPersister
  {
    private readonly ILog log = LogManager.GetLogger( "RevGen" );

    private FileStream persistingFileStream;

    public bool IsActive { get; private set; }

    private readonly Encoding FileEncoding = Encoding.UTF8;

    private const string ClosedAck = "closed";

    public int ThreadUnsafeRevision;

    /// <summary>
    /// Initializes revision generator. If false returned, it's initialised but revision hasn't been restored from the file
    /// so it's restarted from zero. If exception is thrown, it's not initialised and shouldn't be used. After a succeessful
    /// call to this it can't be called for a second time without calling Shutdown first.
    /// </summary>
    /// <param name="persistingFilePath"></param>
    /// <returns></returns>
    public bool Init( string persistingFilePath, out string initWarnings )
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
            int.TryParse( line1, out ThreadUnsafeRevision ) &&
            ThreadUnsafeRevision >= 0 &&
            line2 == ClosedAck;

          if ( result )
            initWarnings = null;
          else
            initWarnings = "Can't parse revision file";

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
        sw.Write( ThreadUnsafeRevision );
        sw.Flush( );
        persistingFileStream.Flush( );

        IsActive = true;

        return result;
      }
      catch ( Exception exc )
      {
        log.Error( "Can't initialize revision persister", exc );
        if ( persistingFileStream != null )
        {
          try
          {
            persistingFileStream.Close( );
          }
          catch ( Exception excClose )
          {
            log.Error( "Can't close revision persister properly", excClose );
          }
          persistingFileStream = null;
        }

        throw;
      }
    }

    public int Shutdown( )
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
          sw.WriteLine( ThreadUnsafeRevision );
          sw.Write( ClosedAck );
          sw.Flush( );

          persistingFileStream.Close( );
        }
        catch ( Exception excClose )
        {
          log.Error( "Can't close revision persister properly", excClose );
          throw;
        }
        finally
        {
          persistingFileStream = null;
        }
      }

      return ThreadUnsafeRevision;
    }
  }
}