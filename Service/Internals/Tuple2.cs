using System;
using System.Collections.Generic;

namespace FlyTrace.Service.Internals
{
  // TODO: remove after moving to .NET 4.x
  [Serializable]
  internal class Tuple<T1, T2> : IEquatable<Tuple<T1, T2>>, IComparable<Tuple<T1, T2>>
  {
    private readonly T1 item1;
    private readonly T2 item2;

    public T1 Item1 { get { return this.item1; } }

    public T2 Item2 { get { return this.item2; } }

    public Tuple( T1 item1, T2 item2 )
    {
      this.item1 = item1;
      this.item2 = item2;
    }

    public bool Equals( Tuple<T1, T2> other )
    {
      return
        other != null &&
        EqualityComparer<T1>.Default.Equals( this.item1, other.item1 ) &&
        EqualityComparer<T2>.Default.Equals( this.item2, other.item2 );
    }

    public int CompareTo( Tuple<T1, T2> other )
    {
      if ( other == null )
        throw new ArgumentNullException( "other" );

      int num = Comparer<T1>.Default.Compare( this.item1, other.item1 );
      if ( num != 0 )
        return num;

      return Comparer<T2>.Default.Compare( this.item2, other.item2 );
    }

    public override bool Equals( object obj )
    {
      return Equals( obj as Tuple<T1, T2> );
    }

    public override int GetHashCode( )
    {
      return
        ( EqualityComparer<T1>.Default.GetHashCode( item1 ) * 397 ) ^
        EqualityComparer<T2>.Default.GetHashCode( item2 );
    }

    public override string ToString( )
    {
      return string.Format( "({0}, {1})", this.item1, this.item2 );
    }
  }
}