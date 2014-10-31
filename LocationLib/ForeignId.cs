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
using System.Diagnostics;

namespace FlyTrace.LocationLib
{
  [DebuggerDisplay( "{Type} - {Id}" )]
  public struct ForeignId : IEquatable<ForeignId>
  {
    public ForeignId( string type, string id )
    {
      Type = type;
      Id = id;
    }

    // ReSharper disable InconsistentNaming (consts are ok)
    public const string SPOT = "SPOT";

    public const string TEST = "TEST";
    // ReSharper restore InconsistentNaming


    /// <summary>E.g. SPOT, DeLorme, etc. Intentionally not strong typed.</summary>
    public readonly string Type;

    /// <summary>Unique ID of the tracker on its site of origin</summary>
    public readonly string Id;

    public override int GetHashCode( )
    {
      unchecked
      {
        return
          ( EqualityComparer<string>.Default.GetHashCode( Type ) * 397 ) ^
          EqualityComparer<string>.Default.GetHashCode( Id );
      }
    }

    public override bool Equals( object obj )
    {
      if ( !( obj is ForeignId ) )
        return false;

      return Equals( ( ForeignId ) obj );
    }

    public bool Equals( ForeignId other )
    {
      return
        Type == other.Type &&
        Id == other.Id;
    }

    public override string ToString( )
    {
      return string.Format( "{0} {1}", Type, Id );
    }
  }
}