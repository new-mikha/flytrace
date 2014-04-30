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
using System.Web;
using System.Web.Compilation;
using System.CodeDom;
using System.Web.UI;

namespace FlyTrace.Tools
{
  // Taken from here: http://weblogs.asp.net/infinitiesloop/archive/2006/08/09/The-CodeExpressionBuilder.aspx

  [ExpressionPrefix( "Code" )]
  public class CodeExpressionBuilder : ExpressionBuilder
  {
    public override CodeExpression GetCodeExpression( BoundPropertyEntry entry ,
       object parsedData , ExpressionBuilderContext context )
    {
      return new CodeSnippetExpression( entry.Expression );
    }
  }
}