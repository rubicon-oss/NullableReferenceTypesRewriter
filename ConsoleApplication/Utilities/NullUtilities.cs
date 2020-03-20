using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypesRewriter.ConsoleApplication
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
                  && IsDefinitlyNull (returnStatement.Expression!, semanticModel));
    }

    public static bool IsDefinitlyNull (ExpressionSyntax expression, SemanticModel semanticModel)
    {
      var typeInfo = semanticModel.GetTypeInfo (expression);

      if (typeInfo.Nullability.FlowState == NullableFlowState.NotNull)
      {
        Console.WriteLine ($"NOT NULL:    {expression}");
        return false;
      }

      if (typeInfo.Nullability.FlowState == NullableFlowState.MaybeNull)
      {
        Console.WriteLine ($"MAYBE NULL:  {expression}");
        return true;
      }

      if (typeInfo.Nullability.FlowState == NullableFlowState.None)
      {
        Console.WriteLine ($"UNDECIDED:   {expression}");
      }

      return expression switch
      {
          LiteralExpressionSyntax literal => literal.Kind() == SyntaxKind.NullLiteralExpression ||
                                             literal.Kind() == SyntaxKind.DefaultExpression,
          InvocationExpressionSyntax invocation => InvokesNullReturningMethod (invocation, semanticModel),
          _ => false,
      };
    }

    private static bool InvokesNullReturningMethod (InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
      var method = semanticModel.GetSymbolInfo (invocation)
          .Symbol as IMethodSymbol;
      if (method == null)
        return false;
      if (!method.ReturnType.IsReferenceType)
        return false;

      return method.DeclaringSyntaxReferences.Any (syntaxRef => DoesReturnVoid ((MethodDeclarationSyntax) syntaxRef.GetSyntax()));
    }

    public static bool DoesReturnVoid (MethodDeclarationSyntax node)
    {
      return node.ReturnType is PredefinedTypeSyntax type
             && type.Keyword.Kind() == SyntaxKind.VoidKeyword;
    }


    public static TypeSyntax MakeNullable (TypeSyntax typeSyntax)
    {
      if (typeSyntax is NullableTypeSyntax)
        return typeSyntax;
      var nullable = SyntaxFactory.NullableType (typeSyntax.WithoutTrailingTrivia());
      return nullable
          .WithTrailingTrivia (typeSyntax.GetTrailingTrivia());
    }
  }
}