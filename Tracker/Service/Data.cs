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

using System.Web.Configuration;
using System.Data.SqlClient;

namespace FlyTrace.Service
{
  public static class Data
  {
    private static volatile string OriginalConnString;

    private static volatile string AsyncConnString;

    public static string GetConnectionString( )
    {
      /* Connection string from the web.config probably has to be modified to allow async connection,
       * because original one doesn't have to have "Asynchronous Processing=True" in it, while 
       * BeginExecuteReader requires that. Goal here is to make it fast and efficient, using the no-lock 
       * technique described below.
       * 
       * The connection string is cached to save calls to new SqlConnectionStringBuilder. But it could be changed in 
       * web.config at any time, so original version from the web.config also has to be stored. If web.config version 
       * differs from what is stored as web.config version, then the cached value with async.clause is updated.
       * 
       * Value of OriginalConnString and AsyncConnString fields can be accessed and written from many threads.
       * That's OK - every time we compare OriginalConnString with a value from web.config, and if it differs we rebuild the
       * modified one. There could be many processes doing that simultaneously, that's OK too because eventually 
       * all of them will write the correct value, and all of them will use the correct value. Finally, AsyncConnString could 
       * already have a correct value while OriginalConnString is still old - again, it's OK
       */

      // Note that this project (Service.sln) is in the subdirectory of the main project (Tracker.sln), so 
      // the connection string is probably inherited from the parent directory config file.
      string originalConnString =
        WebConfigurationManager.ConnectionStrings["TrackerConnectionString"].ConnectionString;

      // Note that OriginalConnString is volatile
      if ( OriginalConnString == null || OriginalConnString != originalConnString )
      {
        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder( originalConnString );
        builder.AsynchronousProcessing = true;

        // Note that both AsyncConnString and OriginalConnString are volatile, so assigning them always 
        // happen exactly in this order:
        AsyncConnString = builder.ConnectionString;
        OriginalConnString = originalConnString;
      }

      return AsyncConnString;
    }
  }
}