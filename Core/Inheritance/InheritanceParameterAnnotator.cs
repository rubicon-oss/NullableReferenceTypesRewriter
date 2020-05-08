// Copyright (c) rubicon IT GmbH
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.Utilities;

namespace NullableReferenceTypesRewriter.Inheritance
{
  public class InheritanceParameterAnnotator : CSharpSyntaxRewriter
  {
    private readonly Dictionary<MethodDeclarationSyntax, string[]> _nullableInterfaces;

    public InheritanceParameterAnnotator (Dictionary<MethodDeclarationSyntax, string[]> nullableInterfaces)
    {
      _nullableInterfaces = nullableInterfaces;
    }

    public override SyntaxNode? VisitMethodDeclaration (MethodDeclarationSyntax node)
    {
      if (_nullableInterfaces.TryGetValue (node, out var parameterNames))
      {
        if (parameterNames.Contains ("#return"))
          node = node.WithReturnType (NullUtilities.ToNullable (node.ReturnType));
        var newParameterList = parameterNames.Aggregate (node.ParameterList, ToNullableParameter);
        return node.WithParameterList (newParameterList);
      }

      return base.VisitMethodDeclaration (node);
    }


    private ParameterListSyntax ToNullableParameter (ParameterListSyntax parameterListSyntax, string parameterName)
    {
      var parameter = parameterListSyntax.Parameters.FirstOrDefault (param => param.Identifier.ToString() == parameterName);

      if (parameter == null)
        return parameterListSyntax;

      var nullableParameter = parameter.WithType (NullUtilities.ToNullable (parameter.Type!));

      return parameterListSyntax.WithParameters (parameterListSyntax.Parameters.Replace (parameter, nullableParameter));
    }
  }
}