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

namespace FlyTrace.Service.Internals
{
  [DebuggerDisplay( "{Name} - {ForeignId}" )]
  internal struct TrackerName
  {
    public string Name;

    public ForeignId ForeignId;
  }

  internal struct GroupDef
  {
    public List<TrackerName> TrackerNames;

    public int VersionInDb;

    public bool ShowUserMessages;

    public DateTime? StartTs;
  }

  internal class GroupFacade
  {
    private static readonly ILog Log = LogManager.GetLogger( "GrpFcde" );

    private int groupId;

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

      {
        GroupDef cachedResult;
        if ( TryGetFromCache( group, out cachedResult ) )
        {
          AsyncResult<GroupDef> result = new AsyncResult<GroupDef>( callback, asyncState );
          result.SetAsCompleted( cachedResult, true );
          return result;
        }
      }

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
        this.groupId = group;

        return this.sqlCmd.BeginExecuteReader( callback, asyncState );
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Can't start getting group IDs for group {0}: {1}", group, exc.Message );
        sqlConn.Close( );
        throw;
      }
    }

    public GroupDef EndGetGroupTrackerIds( IAsyncResult asyncResult )
    {
      if ( asyncResult is AsyncResult<int> )
      { // it's a test group with faked locations taken from files
        int group = ( asyncResult as AsyncResult<int> ).EndInvoke( );

        if ( group != TestGroup )
          throw new ApplicationException( string.Format( "Unknown test group {0}", group ) );

        return GetTestGroup( );
      }

      if ( asyncResult is AsyncResult<GroupDef> )
      { // result was cached, so now it is just a technical call to retrieve it:
        return ( asyncResult as AsyncResult<GroupDef> ).EndInvoke( );
      }

      GroupDef result;

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

      SetInCache( this.groupId, result );

      return result;
    }

    private static GroupDef GetTestGroup( )
    {
      GroupDef result;

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

    private readonly static Dictionary<int, GroupDef> Cache = new Dictionary<int, GroupDef>( );

    private readonly static ReaderWriterLockSlim RwLock = new ReaderWriterLockSlim( );

    internal static bool TryGetFromCache( int groupId, out GroupDef groupDef )
    {
      if ( !Properties.Settings.Default.GroupDefCacheEnabled )
      {
        groupDef = default( GroupDef );
        return false;
      }

      RwLock.EnterReadLock( );
      try
      {
        return Cache.TryGetValue( groupId, out groupDef );
      }
      finally
      {
        RwLock.ExitReadLock( );
      }
    }

    private static void SetInCache( int groupId, GroupDef groupDef )
    {
      if ( !Properties.Settings.Default.GroupDefCacheEnabled )
      {
        return;
      }

      RwLock.EnterWriteLock( );
      try
      {
        Cache[groupId] = groupDef;
      }
      finally
      {
        RwLock.ExitWriteLock( );
      }
    }

    internal static void ResetCache( )
    {
      // For data integrity safety, do NOT use check for the GroupDefCacheEnabled value here,
      // just always clear the cache if asked for.

      RwLock.EnterWriteLock( );
      try
      {
        Cache.Clear( );
      }
      finally
      {
        RwLock.ExitWriteLock( );
      }
    }
  }
}