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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NullableReferenceTypesRewriter.Utilities
{
  public static class NullUtilities
  {
    public static bool ReturnsNull (MethodDeclarationSyntax node, SemanticModel semanticModel)
    {
      var returnStatements = semanticModel.AnalyzeControlFlow (
              node.Body?.Statements.First(),
              node.Body?.Statements.Last())
          .ReturnStatements;

      return returnStatements.Any (
          stmt => stmt is ReturnStatementSyntax returnStatement
                  && CanBeNull (returnStatement.Expression!, semanticModel));
    }

    public static bool CanBeNull (ExpressionSyntax expression, SemanticModel semanticModel)
    {
      var typeInfo = semanticModel.GetTypeInfo (expression);

      return typeInfo.Nullability.FlowState switch
      {
          NullableFlowState.MaybeNull => true,
          _ => false
      };
    }

    public static bool ReturnsVoid (MethodDeclarationSyntax node)
    {
      return node.ReturnType is PredefinedTypeSyntax type
             && type.Keyword.Kind() == SyntaxKind.VoidKeyword;
    }

    public static MethodDeclarationSyntax ToNullReturning (MethodDeclarationSyntax method)
    {
      return method.WithReturnType (ToNullable (method.ReturnType));
    }

    public static TypeSyntax ToNullable (TypeSyntax typeSyntax)
    {
      if (typeSyntax is NullableTypeSyntax)
        return typeSyntax;
      var nullable = NullableType (typeSyntax.WithoutTrailingTrivia());
      return nullable
          .WithTrailingTrivia (typeSyntax.GetTrailingTrivia());
    }
  }
}