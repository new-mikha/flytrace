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

namespace FlyTrace.Service.Tools
{
  public static class ConnectionStringModifier
  {
    /// <summary>
    /// Connection string from the config file, but with added "Asynchronous Processing=True" that 
    /// is required by BeginExecuteReader.
    /// </summary>
    /// <remarks>
    /// Absolutely no need in any lind of lock here because .NET ensures that static initialized is run once 
    /// and once only. Notice that if web.config changed then the app is restarted, so the conn.string needs
    /// to be obtained once only.
    /// </remarks>
    public static readonly string AsyncConnString = GetConnectionString( );

    private static string GetConnectionString( )
    {
      // Note that this project (Service.sln) is in the subdirectory of the main project (Tracker.sln), so 
      // the connection string is probably inherited from the parent directory config file.
      string originalConnString =
        WebConfigurationManager.ConnectionStrings["TrackerConnectionString"].ConnectionString;

      SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder( originalConnString )
      {
        AsynchronousProcessing = true
      };

      return builder.ConnectionString;
    }
  }
}