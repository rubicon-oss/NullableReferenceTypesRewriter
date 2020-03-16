using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableTypeSetter.ConsoleApplication
{
 public class MethodNullAnnotator : CSharpSyntaxRewriter
  {
    private SemanticModel _semanticModel;

    public MethodNullAnnotator (SemanticModel semanticModel)
    {
      _semanticModel = semanticModel;
    }

    public override SyntaxNode? VisitMethodDeclaration (MethodDeclarationSyntax node)
    {
      if (DoesReturnVoid (node))
        return node;
      if (node.Body == null)
        return node;
      if (node.Body?.Statements.Count == 0)
        return node;

      var retrunsNull = HasCanBeNullAttribute (node)
                        || ReturnsNull (node, _semanticModel);

      if (!retrunsNull)
        return node;

      return node.WithReturnType (
          MakeNullable (node.ReturnType));
    }

    private static bool ReturnsNull (MethodDeclarationSyntax node, SemanticModel semanticModel)
    {
      var returnStatements = semanticModel.AnalyzeControlFlow (
              node.Body?.Statements.First(),
              node.Body?.Statements.Last())
          .ReturnStatements;

      return returnStatements.Any (
          stmt => stmt is ReturnStatementSyntax returnStatement
                  && IsDefinitlyNull (returnStatement.Expression!, semanticModel));
    }

    private static bool HasCanBeNullAttribute (MethodDeclarationSyntax node)
    {
      return node.AttributeLists.SelectMany (list => list.Attributes)
          .Any (attr => attr.Name.ToString().EndsWith ("CanBeNull"));
    }

    private static bool DoesReturnVoid (MethodDeclarationSyntax node)
    {
      return node.ReturnType is PredefinedTypeSyntax type
             && type.Keyword.Kind() == SyntaxKind.VoidKeyword;
    }

    public static bool IsDefinitlyNull (ExpressionSyntax expression, SemanticModel semanticModel)
    {
      var typeInfo = semanticModel.GetTypeInfo (expression);

      if (typeInfo.Nullability.FlowState != NullableFlowState.None
          || typeInfo.Nullability.Annotation == NullableAnnotation.Annotated)
      {
        Console.WriteLine (expression);
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