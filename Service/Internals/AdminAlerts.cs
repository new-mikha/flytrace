using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlyTrace.LocationLib;

namespace FlyTrace.Service.Internals
{
  internal class AdminAlerts
  {
    private readonly Dictionary<string, string> messages =
      new Dictionary<string, string>( StringComparer.InvariantCultureIgnoreCase );

    public int CoordAccessCount;

    public readonly DateTime StartTime = TimeService.Now;

    public List<KeyValuePair<string, string>> GetMessages( )
    {
      lock ( this.messages )
      {
        return this.messages.ToList( );
      }
    }

    /// <summary>
    /// It's thread-safe so a bit slow. Use specific fields for time-critical access 
    /// (e.g. Interlocked on <see cref="CoordAccessCount"/> field)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string this[string key]
    {
      get
      {
        lock ( this.messages )
        {
          string result;
          this.messages.TryGetValue( key, out result );
          return result;
        }
      }

      set
      {
        lock ( this.messages )
        {
          if ( this.messages.ContainsKey( key ) )
            this.messages[key] = value;
          else
            this.messages.Add( key, value );
        }
      }
    }
  }
}