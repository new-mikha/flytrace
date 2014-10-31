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
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using FlyTrace.LocationLib;

using log4net;

namespace FlyTrace.Service
{
  [DebuggerDisplay( "{Name} - {ForeignId}" )]
  public struct TrackerName
  {
    public string Name;

    public ForeignId ForeignId;
  }

  public struct GroupConfig
  {
    public List<TrackerName> TrackerNames;

    public int VersionInDb;

    public bool ShowUserMessages;

    public DateTime? StartTs;
  }

  public class GroupFacade
  {
    private static readonly ILog Log = LogManager.GetLogger( "GrpFcde" );

    private SqlCommand sqlCmd;

    private enum Operation { None, GetGroupTrackerIds, GetTrackerId };

    private int operation;

    public const int TestGroup = 1;

    public IAsyncResult BeginGetGroupTrackerIds( int group, AsyncCallback callback, object asyncState )
    {
      if ( group == TestGroup )
      {
        AsyncResult<int> result = new AsyncResult<int>( callback, asyncState );
        result.SetAsCompleted( group, true );
        return result;
      }

      /* TODO
       * Reading data from DB at every call to the web service is probably the narrowest bottleneck 
       * in the process. Could be optimized along the following lines:
       * - Cache data in the dictionary, where value is a something that have a list of 
       *    trackers + some other params (see below)
       * - There should be a separate thread responsible for filling the values of the dicitonary.
       * - When a new group is asked for, add "empty" value to the dictionary (using lock, which means that 
       *    the same lock is used for reading from the dictionary too. Although some no-locking technique 
       *    could be used, but that looks redundant)
       * - Wake up the retrieve thread
       * - Wait for the thread to retrieve the data for all "dirty" or "emtpy" values. EventWaitHandle for every group 
       *    could probably be used for that because it's a rare operation?
       * - Data changes in the web UI should be detectable. For that, probably the whole class library should 
       *    be moved into the main project, so the change event could be called directly when a group page is 
       *    updated. Alterntatively WCF with some fast channel like tcp could be used)
       * - If the data changed, a dictionary value should be marked as "dirty", and the retrieve thread should be kicked.
       * - When a call to this class finds that the dictionary value is dirty, it should wait for for the 
       *    retrieve thread.
       * 
       * ... but all of the above looks too dramatic to implement at the moment, so keeping it as TO DO and 
       * just reading DB every time in the recommended asynchronous way.
       */

      // Ensure that this instance is not used already for some other call:
      int prevOp =
        Interlocked.CompareExchange(
          ref this.operation,
          ( int ) ( Operation.GetGroupTrackerIds ),
          ( int ) ( Operation.None ) );

      if ( prevOp != ( int ) Operation.None )
      {
        throw new InvalidOperationException(
          string.Format(
            "Cannot use an instance of {0} for simultaneous operations. Call End* method first.",
            this.GetType( )
          )
        );
      }

      string connString = Tools.ConnectionStringModifier.AsyncConnString;

      // use parameter (rather than add groupId value to the string) to 
      // allow SQL Server cache execution plan for the query:
      SqlConnection sqlConn = new SqlConnection( connString );
      sqlConn.Open( );
      try
      {
        this.sqlCmd = new SqlCommand( "GetGroupTrackerIds", sqlConn );
        this.sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;

        // Use parameter, not formatted sql string - see above why
        this.sqlCmd.Parameters.Add( "@GroupId", System.Data.SqlDbType.Int );

        SqlParameter versionPar = this.sqlCmd.Parameters.Add( "@Version", System.Data.SqlDbType.Int );
        versionPar.Direction = System.Data.ParameterDirection.Output;

        SqlParameter displayUserMessagesPar = this.sqlCmd.Parameters.Add( "@DisplayUserMessages", System.Data.SqlDbType.Bit );
        displayUserMessagesPar.Direction = System.Data.ParameterDirection.Output;

        SqlParameter startTsPar = this.sqlCmd.Parameters.Add( "@StartTs", System.Data.SqlDbType.DateTime );
        startTsPar.Direction = System.Data.ParameterDirection.Output;

        this.sqlCmd.Parameters[0].Value = group;

        return this.sqlCmd.BeginExecuteReader( callback, asyncState );
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Can't start getting group IDs for group {0}: {1}", group, exc.Message );
        sqlConn.Close( );
        throw;
      }
    }

    public GroupConfig EndGetGroupTrackerIds( IAsyncResult asyncResult )
    {
      if ( asyncResult is AsyncResult<int> )
      { // it's a test group with faked locations taken from files
        int group = ( asyncResult as AsyncResult<int> ).EndInvoke( );

        if ( group != TestGroup )
          throw new ApplicationException( string.Format( "Unknown test group {0}", group ) );

        return GetTestGroup( );
      }

      GroupConfig result;

      int prevOp =
        Interlocked.CompareExchange(
          ref this.operation,
          ( int ) ( Operation.None ),
          ( int ) ( Operation.GetGroupTrackerIds ) );

      if ( prevOp != ( int ) Operation.GetGroupTrackerIds )
      {
        throw new InvalidOperationException( "End* call doesn't have a corresponding Begin* call." );
      }

      result.TrackerNames = new List<TrackerName>( );

      try
      {
        using ( SqlDataReader sqlDataReader = this.sqlCmd.EndExecuteReader( asyncResult ) )
        {
          while ( sqlDataReader.Read( ) )
          {
            TrackerName trackerId;

            trackerId.Name = sqlDataReader["Name"].ToString( );

            ForeignId foreignId = new ForeignId( "SPOT", sqlDataReader["TrackerForeignId"].ToString( ) );
            trackerId.ForeignId = foreignId;

            result.TrackerNames.Add( trackerId );
          }
        }

        object objValue = this.sqlCmd.Parameters["@Version"].Value;
        if ( objValue is DBNull )
          result.VersionInDb = 0;
        else
          result.VersionInDb = Convert.ToInt32( objValue );

        objValue = this.sqlCmd.Parameters["@DisplayUserMessages"].Value;
        if ( objValue is DBNull )
          result.ShowUserMessages = false;
        else
          result.ShowUserMessages = Convert.ToBoolean( objValue );

        try
        {
          object objStartTs = this.sqlCmd.Parameters["@StartTs"].Value;
          if ( objStartTs is DBNull )
            result.StartTs = null;
          else
          {
            DateTime temp = Convert.ToDateTime( objStartTs );
            if ( temp.Kind == DateTimeKind.Unspecified )
              result.StartTs = DateTime.SpecifyKind( temp, DateTimeKind.Utc );
            else
              result.StartTs = temp.ToUniversalTime( );
          }
        }
        catch ( Exception exc )
        { // It's a service feature, so mainly just ignore the error:
          Log.ErrorFormat( "Can't get StartTs: {0}", exc.Message );
          result.StartTs = null;
        }
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Can't end getting group IDs: {0}", exc.Message );
        throw;
      }
      finally
      {
        this.sqlCmd.Connection.Close( );
      }

      return result;
    }

    private static GroupConfig GetTestGroup( )
    {
      GroupConfig result;

      string[] names = TestSource.Singleton.GetTestNames( );

      result.VersionInDb = 1;
      result.ShowUserMessages = true;

      result.TrackerNames =
        ( from n in names
          select new TrackerName( )
          {
            Name = n,
            ForeignId =
              new ForeignId(
                ForeignId.TEST,
                TestSource.TestIdPrefix + n 
              )
          }
        ).ToList( );

      result.StartTs = null;

      return result;
    }

    public IAsyncResult BeginGetTrackerId( int group, string trackerName, AsyncCallback callback, object asyncState )
    {
      /* TODO: see comment in BeginGetGroupTrackerIds re caching - the same approach is applicable to this method too.
       */

      // Ensure that this instance is not used already for some other call:
      int prevOp =
        Interlocked.CompareExchange(
          ref this.operation,
          ( int ) ( Operation.GetTrackerId ),
          ( int ) ( Operation.None ) );

      if ( prevOp != ( int ) Operation.None )
      {
        throw new InvalidOperationException(
          string.Format(
            "Cannot use an instance of {0} for simultaneous operations. Call End* method first.",
            this.GetType( )
          )
        );
      }

      string connString = Tools.ConnectionStringModifier.AsyncConnString;

      // 1. Use parameter (rather than add groupId value to the string) to allow SQL Server cache 
      //    execution plan for the query.
      // 2. Get the name too because Name column's collation is case-insensitive, so get the right writing 
      //    (not sure if it's really needed, but it costs nothing)
      const string sql =
        "SELECT [Name], [TrackerForeignId] FROM [GroupTracker] WHERE GroupId = @GroupId AND [Name] = @TrackerName";

      SqlConnection sqlConn = new SqlConnection( connString );
      sqlConn.Open( );
      try
      {
        this.sqlCmd = new SqlCommand( sql, sqlConn );

        // Use parameter, not formatted sql string - see above why
        this.sqlCmd.Parameters.Add( "@GroupId", System.Data.SqlDbType.Int );
        this.sqlCmd.Parameters[0].Value = group;

        // Use parameter, not formatted sql string - see above why
        this.sqlCmd.Parameters.Add( "@TrackerName", System.Data.SqlDbType.NVarChar, 50 );
        this.sqlCmd.Parameters["@TrackerName"].Value = trackerName;

        return this.sqlCmd.BeginExecuteReader( callback, asyncState );
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Can't start getting single ID for group {0} and name {1}: {2}", group, trackerName, exc.Message );
        sqlConn.Close( );
        throw;
      }
    }

    public TrackerName EndGetTrackerId( IAsyncResult asyncResult )
    {
      int prevOp =
        Interlocked.CompareExchange(
          ref this.operation,
          ( int ) ( Operation.None ),
          ( int ) ( Operation.GetGroupTrackerIds ) );

      if ( prevOp != ( int ) Operation.GetTrackerId )
      {
        throw new InvalidOperationException( "End* call doesn't have a corresponding Begin* call." );
      }

      try
      {
        using ( SqlDataReader sqlDataReader = this.sqlCmd.EndExecuteReader( asyncResult ) )
        {
          if ( sqlDataReader.Read( ) )
          {
            TrackerName result;

            result.Name = sqlDataReader["Name"].ToString( );

            ForeignId foreignId = new ForeignId( "SPOT", sqlDataReader["TrackerForeignId"].ToString( ) );
            result.ForeignId = foreignId;

            return result;
          }

          throw new ApplicationException( "Can't find a tracker with such name" );
        }
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Can't end getting single ID: {0}", exc.Message );
        throw;
      }
      finally
      {
        this.sqlCmd.Connection.Close( );
      }
    }
  }
}