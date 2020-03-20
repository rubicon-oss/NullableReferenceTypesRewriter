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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.ConsoleApplication.Utilities;

namespace NullableReferenceTypesRewriter.ConsoleApplication.MethodArguments
{
  public class MethodArgumentFromInvocationNullAnnotator : CSharpSyntaxRewriter
  {
    private readonly Action<ParameterSyntax> _parameterCallback;
    private readonly SemanticModel _semanticModel;

    public MethodArgumentFromInvocationNullAnnotator (SemanticModel semanticModel, Action<ParameterSyntax> parameterCallback)
    {
      _parameterCallback = parameterCallback;
      _semanticModel = semanticModel;
    }

    public override SyntaxNode? VisitInvocationExpression (InvocationExpressionSyntax node)
    {
      var nullableArguments = node.ArgumentList.Arguments
          .Select ((arg, index) => (arg, index))
          .Where (arg => NullUtilities.CanBeNull (arg.arg.Expression, _semanticModel))
          .ToList();

      if (nullableArguments.Count == 0)
        return node;

      var methodDeclaration = _semanticModel.GetSymbolInfo (node);

      var methodSyntax = (MethodDeclarationSyntax?) methodDeclaration.Symbol?.DeclaringSyntaxReferences.FirstOrDefault()
          ?.GetSyntaxAsync()?.GetAwaiter().GetResult();

      if (methodSyntax == null)
        return node;

      foreach (var argument in nullableArguments)
      {
        var nullableParameter = methodSyntax.ParameterList.Parameters[argument.index];
        _parameterCallback (nullableParameter);
      }

      return node;
    }
  }
}