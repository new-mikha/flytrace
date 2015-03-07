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

namespace FlyTrace.ConfigHelper
{
  class Program
  {
    static int Main( string[] args )
    {
      try
      {
        Console.WriteLine( "Flytrace Config Helper Tool" );
        if ( args.Length == 0
             || args[0].ToLower( ).Contains( "help" )
             || args[0].Contains( "?" ) )
        {
          Console.WriteLine( "Usage:" );
          Console.WriteLine( "\tTo fetch a section from the 2nd level and put it into a separate file:" );
          Console.WriteLine( "\tConfigHelper fetch <section name> <source file> <dest file>" );
          return 1;
        }

        Console.WriteLine( "Command line: " + string.Join( " ", args ) );


        Console.WriteLine( "Done." );

        return 0;
      }
      catch ( Exception exc )
      {
        Console.WriteLine( "Error in Flytrace Config Helper Tool: " + exc );
        return 1;
      }
    }
  }
}
