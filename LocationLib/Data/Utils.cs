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
using System.Linq.Expressions;
using System.Reflection;

namespace FlyTrace.LocationLib.Data
{
  internal static class Utils
  {
    /// <summary>
    /// Checks that the passed expression includes all fields/properties of the type. See Remarks section for 
    /// details &amp; examples.
    /// </summary>
    /// <remarks>
    /// Method checks that the passed expression includes all fields/properties of the template parameter type.
    /// It's applied to equality expressions that are vitally important for a correct work of the application algorithms. 
    /// 
    /// It could be a part of unit tests but there is no guarantee that a unit test would be run after a mistake considered 
    /// as "a minor change". So to make the code robust, this method checks that all fields and properties are included into 
    /// the expression and does some other basic checks. So it's a kind of an always-run self test.
    /// 
    /// In case of a found problem it raises an exception, preventing instances of type T to be created at all.
    /// Another approach would be to report to log4net and return trivial expression (always returning false), but it would be
    /// much harded to notice.
    /// 
    /// Examples of good and bad expressions:
    /// 
    /// Taking this struct:
    /// struct Foo
    /// {
    ///   int A, B, C;
    /// }
    /// 
    /// This would be Ok:
    /// 
    /// <![CDATA[
    ///   ( x, y ) =>
    ///     x.A == y.A &&
    ///     x.B == y.B &&
    ///     x.C == y.C
    /// ]]>
    /// 
    /// Order of properties and parameters "x" and "y" is not important, e.g. the above could also be:
    /// <![CDATA[
    ///   ( x, y ) =>
    ///     y.B == x.B &&
    ///     x.A == y.A &&
    ///     x.C == y.C
    /// ]]>
    /// 
    /// If another field is added to Foo, then expression above (as it is) is not Ok anymore. New field needs to 
    /// be added there too:
    /// <![CDATA[
    ///   ( x, y ) =>
    ///     y.B == x.B &&
    ///     x.A == y.A &&
    ///     x.C == y.C &&
    ///     x.NewField == y.NewField
    /// ]]>
    /// The expression right above is OK if NewField was added, but e.g. the first example becomes invalid.
    /// 
    /// This expression is bad (even with Foo having only A, B and C fields)
    /// <![CDATA[
    /// ( x, y ) =>
    ///     x.A == x.A &&  // bad becase "x" used on both sides of equality expression
    ///     x.B == y.B &&
    ///     x.C == y.C
    /// ]]>
    /// This is bad because the method found that x.A is comapared to itself.
    /// 
    /// --- Using methods to comapare:
    /// 
    /// Considering NewField hasn't been added, this is Ok for Foo struct:
    /// <![CDATA[
    ///   ( x, y ) =>
    ///     x.A == y.A &&
    ///     x.B == y.B &&
    ///     x.C.Equals( y.C )
    /// ]]>
    /// 
    /// Ok as well:
    /// <![CDATA[
    /// ( x, y ) =>
    ///     x.A == y.A &&
    ///     x.B == y.B &&
    ///     SomeStaticClass.SomeStaticMethod( x.C, y.C ) // any other parameters might be used in any orders here
    /// ]]>
    /// 
    /// Note that if some complex procedure is required for equality check of some newly added field, it's best to make
    /// it in a form of a static method, like for example TrackPointData.ArePointsEqual() does.
    /// 
    /// If another complex field is added to the struct (with a new type) this new type should have equality checks 
    /// based on the same approach. E.g. see reference between Location, TrackPointData and array of TrackPointData, 
    /// and how ArePointsEqual and ArePointArraysEqual methods are incorporated into equality expression for Location.
    ///     
    /// Some of the errors would not be catched.E.g. this WOULD NOT be catched because "x" and "y" are referenced equal number of times:
    /// <![CDATA[
    /// ( x, y ) =>
    ///     x.A == x.A &&   // comparing x to x
    ///     y.B == y.B &&   // comparing y to y
    ///     x.C == y.C
    /// ]]>
    /// 
    /// Many other cases would not be catched too. I.e. it's possible to trick this method.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="expr"></param>
    /// <param name="excludedMembers">Member names that should be excluded.</param>
    /// <returns></returns>
    internal static Func<T, T, bool> EqualityExpressionCheck<T>
    (
      Expression<Func<T, T, bool>> expr,
      params string[] excludedMembers
    )
    {
      if ( expr == null )
      {
        throw new ArgumentNullException( "expr", string.Format( "Expression is null for {0}", typeof( T ).Name ) );
      }

      Dictionary<string, int> paramsUsageStat = new Dictionary<string, int>( );

      HashSet<string> accessedMemberNames = new HashSet<string>( );

      TraverseExpression( expr.Body, paramsUsageStat, accessedMemberNames );

      CheckSymmetricalParamsUsage( expr.Body, paramsUsageStat );

      CheckFullSetUsageOfFieldsAndProps( typeof( T ), accessedMemberNames, excludedMembers );

      return expr.Compile( );
    }

    #region Private code

    private static void TraverseExpression( Expression expr, Dictionary<string, int> paramsUsageStat, HashSet<string> accessedMemberNames )
    {
      if ( expr is BinaryExpression )
      {
        BinaryExpression be = expr as BinaryExpression;
        TraverseExpression( be.Left, paramsUsageStat, accessedMemberNames );
        TraverseExpression( be.Right, paramsUsageStat, accessedMemberNames );
      }
      else if ( expr is UnaryExpression )
      {
        UnaryExpression ue = expr as UnaryExpression;
        TraverseExpression( ue.Operand, paramsUsageStat, accessedMemberNames );
      }
      else if ( expr is MethodCallExpression )
      {
        MethodCallExpression mce = expr as MethodCallExpression;
        foreach ( Expression argExpr in mce.Arguments )
        {
          TraverseExpression( argExpr, paramsUsageStat, accessedMemberNames );
        }

        if ( mce.Object != null )
          TraverseExpression( mce.Object, paramsUsageStat, accessedMemberNames );
      }
      else if ( expr is MemberExpression )
      {
        MemberExpression me = expr as MemberExpression;
        accessedMemberNames.Add( me.Member.Name );
        TraverseExpression( me.Expression, paramsUsageStat, accessedMemberNames );
      }
      else if ( expr is ParameterExpression )
      {
        ParameterExpression pe = expr as ParameterExpression;
        int paramUsageNum;
        paramsUsageStat.TryGetValue( pe.Name, out paramUsageNum );
        paramsUsageStat[pe.Name] = paramUsageNum + 1;
      }
      else
      {
        throw new ApplicationException(
          string.Format(
            "Node supposed to be either BinaryExpression or MethodCallExpression, but it's {0} ({1})",
            expr == null ? "null" : expr.GetType( ).Name,
            expr
          )
        );
      }
    }

    private static void CheckSymmetricalParamsUsage( Expression expr, Dictionary<string, int> paramsUsageStat )
    {
      if ( paramsUsageStat.Count != 2 )
      {
        throw new ApplicationException(
          string.Format(
            "Both parameters x and y should be used in expression {0}, while at least one is not found at all.",
            expr
          )
        );
      }

      int paramUsageCount = -1;
      foreach ( KeyValuePair<string, int> kvp in paramsUsageStat )
      { // the cycle has either zero or two iterations
        if ( paramUsageCount < 0 )
          paramUsageCount = kvp.Value; // first parameter usage count
        else if ( paramUsageCount != kvp.Value )
        {
          throw new ApplicationException(
            string.Format(
              "Parameters are not used symmetrically in expression {0} ({1} usage(s) of one and {2} usage(s) for another, while it should be equal).",
              expr,
              paramUsageCount,
              kvp.Value
            )
          );
        }
      }
    }

    private static void CheckFullSetUsageOfFieldsAndProps
    (
      Type type,
      IEnumerable<string> namesToCheck,
      IEnumerable<string> excludedMembers
    )
    {
      if ( namesToCheck == null )
        throw new ArgumentNullException( "namesToCheck" );

      if ( excludedMembers == null )
        excludedMembers = Enumerable.Empty<string>( );

      Type sourceType = type;
      // If later need to specifally exclude some field or property from this check, add a 
      // special custom attribute to apply to such field/prop, and check for that attribute here:

      List<string> typeFieldsAndProps = new List<string>( );

      while ( type != null )
      { // iterate through heirarchy to return even private fields:
        FieldInfo[] fields = type.GetFields( BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );

        PropertyInfo[] props = type.GetProperties( BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );

        typeFieldsAndProps.AddRange(
          fields
          .Select( fld => fld.Name )
          .Union(
            props
            .Select( p => p.Name )
          )
        );

        type = type.BaseType;

        if ( type == null ||
             type.FullName.StartsWith( "System" ) ) break;
      }

      string[] excludedMembersArr = excludedMembers as string[] ?? excludedMembers.ToArray( );

      if ( typeFieldsAndProps.Except( namesToCheck ).Except( excludedMembersArr ).Any( ) )
      {
        throw new ApplicationException(
          string.Format(
            "Some required names are missing in expression for {0}",
            sourceType.Name
          )
        );
      }

      if ( excludedMembersArr.Except( typeFieldsAndProps ).Any( ) )
      {
        throw new ApplicationException(
          string.Format(
            "excludedMembers contains field(s) that is not found in the type {0}",
            sourceType.Name
          )
        );
      }
    }

    #endregion
  }
}