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
using System.Threading;
using System.Web;

using log4net;
using log4net.Appender;

using FlyTrace.LocationLib;
using FlyTrace.LocationLib.ForeignAccess;
using FlyTrace.Service.Properties;
using log4net.Repository;

namespace FlyTrace.Service
{
  public class ForeignRequestsManager
  {
    /* There is a number of parameters controlling how calls to the foreign servers are scheduled.
     * 
     * One of those parameters is a maximum number of simultaneous polling calls to the foreign servers. 
     * This number can be 1 or more. When it's 1, only 1 call at a time is possible (which would be VERY 
     * slow for a reasanoble amount of trackers to watch, but such config is still possible)
     * 
     * Below "calls pack" is a set of simultaneously running (overlapping) calls. Here is an example of 
     * overlapping calls starting at separate moments in time:
     * 
     * Number of     0|callllll
     * calls in the  1|...call
     * current call  2|   ..calllllll
     * pack at the   2|     ..callll
     * moment of the 2|       ..calll
     * call start    3|         ..callllllll
     *               1|           ....call
     *                 ---------------------
     *                 012345678901234567890
     *                       time, sec
     * 
     * Assuming there is a queue of required but not started yet calls, here are the parameters to schedule 
     * those calls:
     * 
     * - MaxCallsInPack: int value >=1, defines maximum number of calls in current pack. When a call in 
     *    the pack is finished, another call can be started at the time defined by parameters described 
     *    above. In the example above this parameter could be 4 (see that 4 calls at the 11th and 12th 
     *    seconds).
     *    
     * - TimeFromPrevStart: int value in milliseconds, >=0, defines how often calls can be started. A next
     *   call from the queue can be started only after this time has passed after the START of the previous
     *   one. In the example above this parameter could be 2000.  Note that the queue of required calls could 
     *   be empty when the number of calls in the pack goes below limit, that's why in the example above some 
     *   calls are started with interval of 3 or 4 seconds after prev.start. And of course a call pack could 
     *   even be empty if there is no trackers to update.
     *   
     * - CallsGap: int value in milliseconds, >=0, in use only when there is just one call per time is 
     *    possible, i.e. when MaxCallsInPack (below) is 1. If value of CallsGap > 0, next call from the 
     *    queue can be started only after this time has passed after the END of the previous one. In the 
     *    example above this parameter is ignored because MaxCallsInPack>1.
     *    
     * Below is an example where CallsGap is in use, with MaxCallsInPack=1, TimeFromPrevStart=7000 
     * and CallsGap=2000:
     *      calllll..calll...callllllll..calll...call....call....call....
     *      01234567.0123456701234567....012345670123456701234567
     * Probably it doesn't make much sense to use both TimeFromPrevStart>0 and CallsGap>0, but to keep 
     * the algorithm simple they both are allowed to be non-zero.
     * 
     * Another parameter that controls which trackers need polling calls at all, i.e. which calls form a
     * queue that need to be scheduled in according to the rules above:
     * - RefreshInterval: time in seconds, >0, specifies when a tracker requires a call
     *   to the foreign server to refresh the tracker's data. It's time span that needs to pass after previous
     *   call for the same tracker (always counted from the prev.call start)
     */

    static ForeignRequestsManager( )
    {
      // TODO: remove
      Log.InfoFormat(
        "AvgAllowedMsBetweenCalls at the moment of this instance start: {0}",
        Settings.Default.AvgAllowedMsBetweenCalls
      );
    }

    // It could be just a static class, but I don't want to bother with 'static' everywhere.
    // So making it just a singleton. Note that initializing of this field shouldn't be 
    // protected by 'lock', 'volatile' or whatever, because it's guaranteed by CLR to be 
    // atomic set. No thread can use it until it's set & initialized.
    public static readonly ForeignRequestsManager Singleton = new ForeignRequestsManager( );

    private readonly Thread refreshThread;

    /// <summary>Constructor is private to make the instance accessible only via the <see cref="Singleton"/> field.</summary>
    private ForeignRequestsManager( )
    {
      try
      {
        InitRevisionPersister( );

        this.refreshThread = new Thread( RefreshThreadWorker ) { Name = "LocWorker", IsBackground = true };
        this.refreshThread.Start( ); // it waits until refreshThreadEvent set.

        this.refreshThreadEvent.Set( );
      }
      catch ( Exception exc )
      {
        Log.Error( "Error on starting-up", exc );
        throw;
      }

      InfoLog.Info( "Started." );
    }

    public ReaderWriterLockSlimEx HolderRwLock
    {
      get { return this.scheduler.HolderRwLock; }
    }

    public IDictionary<ForeignId, TrackerStateHolder> Trackers
    {
      get { return this.scheduler.Trackers; }
    }


    public void Stop( )
    {
      this.isStoppingWorkerThread = true;

      int closingRevision = -1;
      if ( this.revisionPersister.IsActive )
        closingRevision = this.revisionPersister.Shutdown( );

      this.refreshThreadEvent.Set( );
      if ( this.refreshThread.Join( 30000 ) )
      {
        // Log it as error to make sure it's logged
        InfoLog.InfoFormat( "Worker thread stopped, closing revision {0}.", closingRevision );
      }
      else
      {
        Log.Error( "Can't stop the worker thread" );
      }
    }

    private static readonly ILog Log = LogManager.GetLogger( "TDM" );

    /// <summary>
    /// Supposed to be always in at least for INFO level, i.e. don't use it too often. E.g. start/stop messages could go there.
    /// </summary>
    private static readonly ILog InfoLog = LogManager.GetLogger( "InfoLog" );

    private static readonly ILog IncrLog = LogManager.GetLogger( "TDM.IncrUpd" );

    public AdminAlerts AdminAlerts = new AdminAlerts( );

    public bool IsAlwaysFullGroup;

    private readonly RevisionPersister revisionPersister = new RevisionPersister( );

    private void InitRevisionPersister( )
    {
      if ( !Settings.Default.AllowIncrementalUpdates )
      {
        this.IsAlwaysFullGroup = true;
        IncrLog.Info( "Starting in 'always full group' mode" );
        return;
      }

      try
      {
        string revisionFilePath = HttpContext.Current.Server.MapPath( @"~/App_Data/revision.bin" );
        string initWarnings;

        if ( this.revisionPersister.Init( revisionFilePath, out initWarnings ) )
        {
          // Log it as an error (while it's actually not) to make sure it's logged:
          IncrLog.InfoFormat(
            "Revgen restored from '{0}' successfuly: current value is {1}",
            revisionFilePath,
            this.revisionPersister.ThreadUnsafeRevision
          );
        }
        else
        { // If Init didn't throw an error, then it's was inited
          IncrLog.ErrorFormat(
            "Revgen failed to restore from '{0}', so re-init it starting from {1}, and will try now to update all group versions in DB",
            revisionFilePath,
            this.revisionPersister.ThreadUnsafeRevision
          );

          string connString = Data.GetConnectionString( );

          SqlConnection sqlConn = new SqlConnection( connString );
          sqlConn.Open( );
          // Can't wrap sqlCmd into using because it's asynchronous
          SqlCommand sqlCmd = new SqlCommand( "UPDATE [Group] SET [Version] = [Version] + 1", sqlConn );

          sqlCmd.ExecuteNonQuery( );

          IncrLog.Warn( "All groups versions increased after restarting Revgen." );
        }

        if ( initWarnings != null )
        {
          AdminAlerts["Revgen init warning"] = initWarnings;
        }

        AdminAlerts["Revgen initialised at"] = this.revisionPersister.ThreadUnsafeRevision.ToString( );
      }
      catch ( Exception exc )
      {
        IncrLog.ErrorFormat( "Can't init Revgen properly or update all groups versions: {0}", exc );

        this.IsAlwaysFullGroup = true;

        if ( this.revisionPersister.IsActive )
          this.revisionPersister.Shutdown( );

        AdminAlerts["Revgen Init Error"] = exc.Message;
      }
    }

    private readonly Scheduler scheduler = new Scheduler( );

    private readonly AutoResetEvent refreshThreadEvent = new AutoResetEvent( false );

    private void RefreshThreadWorker( )
    {
      Global.ConfigureThreadCulture( );

      // It's OK to use "new" everywhere in this method and in methods it 
      // calls, because it's doesn't hit too often.
      // LINQ and enumerators are safe here for the same reason.

      while ( true )
      {
        try
        {
          PokeLog4NetBufferingAppendersSafe( );

          IEnumerable<TrackerStateHolder> trackersToRequest =
            this.scheduler.WaitForMomentOfNextRequestAndRemoveOldTrackers( this.refreshThreadEvent );

          if ( this.isStoppingWorkerThread )
            break;

          foreach ( TrackerStateHolder trackerStateHolder in trackersToRequest )
            StartForeignRequest( trackerStateHolder );
        }
        catch ( Exception exc )
        {
          Log.Error( "RefreshThreadWorker", exc );

          // Normally there should be no exception and this catch is the absolutely last resort.
          // So it's possible that such an unexpected error is not a single one and moreover 
          // it prevents the thread from waiting the reasonable time until requesting the 
          // next tracker etc. So put a Sleep here to prevent possible near-100% processor 
          // load of this thread. If the error is just something occasional, this Sleep is
          // not a problem because it wouldn't block any IO or user interaction, just the 
          // refresh would be delayed a bit:
          Thread.Sleep( 1000 );
        }
      }

      this.refreshThreadEvent.Close( ); // that's unmanaged resource, so closing that

      Log.Info( "Finishing worker thread..." );
    }

    private volatile bool isStoppingWorkerThread;

    private void StartForeignRequest( TrackerStateHolder trackerStateHolder )
    {
      LocationRequest locationRequest =
        ForeignAccessCentral
          .LocationRequestFactories[trackerStateHolder.ForeignId.Type]
          .CreateRequest( trackerStateHolder.ForeignId.Id );

      if ( !this.scheduler.HolderRwLock.TryEnterWriteLock( ) )
      {
        Log.ErrorFormat( "Can't enter write mode in BeginRequest for {0}", trackerStateHolder.ForeignId );
        return;
      }

      try
      {
        try
        {
          trackerStateHolder.CurrentRequest = locationRequest;
        }
        finally
        {
          this.scheduler.HolderRwLock.ExitWriteLock( );
        }

        // Despite TrackerStateHolder having CurrentRequest field, locationRequest still need to 
        // be passed as a implicit parameter because there is no guarantee that at the moment when 
        // OnEndReadLocation is called there will be no other request started. E.g. this request 
        // can be stuck, aborted by timeout, and then suddenly pop in again in OnEndReadLocation. 
        // It's unlikely but still possible. So pass locationRequest to avoid any chance that 
        // EndReadLocation is called on wrong locationRequest.
        Tuple<TrackerStateHolder, LocationRequest> paramTuple =
          new Tuple<TrackerStateHolder, LocationRequest>( trackerStateHolder, locationRequest );

        locationRequest.BeginReadLocation( OnEndReadLocation, paramTuple );
      }
      catch
      {
        trackerStateHolder.CurrentRequest = null; // no good => no CurrentRequest, no setting ScheduledTime to null.
        throw;
      }

      Thread.MemoryBarrier( ); // set ScheduledTime only _after_ successful BeginReadLocation.

      trackerStateHolder.ScheduledTime = null;
    }

    private void OnEndReadLocation( IAsyncResult ar )
    {
      Global.ConfigureThreadCulture( );

      RevisedTrackerState newTrackerState = null;
      ForeignId foreignId = default( ForeignId );
      long? lrid = null;

      try
      {
        LocationRequest locationRequest;
        TrackerStateHolder trackerStateHolder;
        {
          Tuple<TrackerStateHolder, LocationRequest> paramTuple = ( Tuple<TrackerStateHolder, LocationRequest> ) ar.AsyncState;

          trackerStateHolder = paramTuple.Item1;
          locationRequest = paramTuple.Item2;
        }

        lrid = locationRequest.Lrid;
        foreignId = trackerStateHolder.ForeignId;

        // Need to end it even if trackerStateHolder.CurrentRequest != locationRequest (see below)
        TrackerState trackerState = locationRequest.EndReadLocation( ar );

        this.scheduler.HolderRwLock.AttemptEnterUpgradeableReadLock( );
        // when we're here, other thread can be in read mode & no other can be in write or upgr. modes

        try
        {
          // It's possible that this request was just too slow, and it's not the current one any longer:
          if ( trackerStateHolder.CurrentRequest != locationRequest )
            return;

          // Do not save the new value back to the persister yet because it's not write mode yet
          // (logically can do that - see above the note about current mode - but it wouldn't be nice)
          // Later this value can be discarded if no real update happens
          int? newRevisionToUse =
              this.revisionPersister.IsActive
                ? ( this.revisionPersister.ThreadUnsafeRevision + 1 )
                : ( int? ) null;

          // This doesn't write anything too, just a local var so far:
          RevisedTrackerState mergedResult =
            RevisedTrackerState.Merge( trackerStateHolder.Snapshot, trackerState, newRevisionToUse );

          this.scheduler.HolderRwLock.AttemptEnterWriteLock( );

          try
          { // Rolling through the write mode as quick as possible:

            // If an exception thrown before and it didn't come to this point, the request will 
            // be aborted & set to null after a timeout. But normally it's set to null here:
            trackerStateHolder.CurrentRequest = null;

            // Merge can return the old tracker if no change occured:
            if ( !ReferenceEquals( trackerStateHolder.Snapshot, mergedResult ) )
            {
              trackerStateHolder.Snapshot = newTrackerState = mergedResult;

              if ( newRevisionToUse.HasValue ) // if so the revisionPersister.IsActive==true too
                this.revisionPersister.ThreadUnsafeRevision = newRevisionToUse.Value;
            }

            trackerStateHolder.RefreshTime = DateTime.UtcNow;
          }
          finally
          {
            this.scheduler.HolderRwLock.ExitWriteLock( );
          }
        }
        finally
        {
          this.scheduler.HolderRwLock.ExitUpgradeableReadLock( );

          // Notify scheduler that something's happened. Even if it's exception, or return 
          // without setting CurrentRequest, etc - anyway waking up the scheduler doesn't hurt:
          this.refreshThreadEvent.Set( );
        }
      }
      catch ( Exception exc )
      {
        Log.ErrorFormat( "Can't end read location for lrid {0}, {1}: {2}", lrid, foreignId, exc );
      }
      finally
      {
        // Keep logging out of rwLock to make latter as short as possible
        if ( newTrackerState != null && IncrLog.IsDebugEnabled )
          IncrLog.DebugFormat( "New data obtained by lrid {0}: {1}", lrid, newTrackerState );
      }
    }

    internal void ClearTrackers( )
    {
      this.scheduler.ClearTrackers( );
      this.refreshThreadEvent.Set( );
    }

    internal void AddMissingTrackers( List<TrackerName> trackerIds )
    {
      if ( this.scheduler.AddMissingTrackers( trackerIds ) )
      {
        this.refreshThreadEvent.Set( );
      }
    }

    private DateTime prevBufferingAppendersPokingTs = DateTime.UtcNow;

    /// <summary>
    /// See <see cref="PokeLog4NetBufferingAppendersSafe"/> method. Defines time between flushes.
    /// This time is in minutes;
    /// </summary>
    private const int BufferingAppendersFlushPeriod = 30;

    private void PokeLog4NetBufferingAppendersSafe( )
    {
      // Method to flush events in buffered appenders like SmtpAppender. Problem is that it can keep 
      // an unfrequent single event in the buffer until the service is stopping, even if TimeEvaluator
      // is in use. The latter would flush the buffer only when another event is coming after specified 
      // time. But if a single important event comes into the buffer, it would just stay there.
      //
      // This method solves the problem flushing each BufferingAppenderSkeleton in case it's not Lossy, 
      // after every 30 minutes.

      try
      {
        if ( ( DateTime.UtcNow - prevBufferingAppendersPokingTs ).TotalMinutes > BufferingAppendersFlushPeriod )
        {
          prevBufferingAppendersPokingTs = DateTime.UtcNow;

          // queue it into the thread pool to avoid potential delays in log4net in processing that stuff:
          ThreadPool.QueueUserWorkItem( Log4NetBufferingAppendersFlushWorker );
        }
      }
      catch ( Exception exc )
      {
        Log.Error( "PokeLog4NetBufferingAppendersSafe", exc );
      }
    }

    private void Log4NetBufferingAppendersFlushWorker( object state )
    {
      Global.ConfigureThreadCulture( );
      ILog log = LogManager.GetLogger( "LogFlush" );

      string errName = "";
      try
      {
        string destTimeString;
        try
        {
          DateTime destTime =
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.Now, Settings.Default.AdminTimezone );

          destTimeString = destTime + " (admin TZ time)";
        }
        catch ( Exception exc )
        {
          Log.Warn( "Can't convert to the admin time zone", exc );
          destTimeString = DateTime.Now + " (server time)";
        }

        ILoggerRepository defaultRepository = LogManager.GetRepository( );

        foreach ( IAppender appender in defaultRepository.GetAppenders( ) )
        {
          string logName = appender.GetType( ).Name;

          errName = logName;
          if ( appender is BufferingAppenderSkeleton )
          {
            log.InfoFormat( "Flushing {0} at {1}", logName, destTimeString );
            BufferingAppenderSkeleton bufferingAppender = appender as BufferingAppenderSkeleton;
            if ( !bufferingAppender.Lossy )
            {
              bufferingAppender.Flush( );
            }
          }
        }
      }
      catch ( Exception exc )
      {
        log.ErrorFormat( "Can't poke buffering appenders, error happened for '{0}': {1}", errName, exc );
      }
    }
  }
}