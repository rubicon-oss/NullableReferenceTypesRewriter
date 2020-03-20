using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.ConsoleApplication.Utilities;

namespace NullableReferenceTypesRewriter.ConsoleApplication
{
  public class CastExpressionNullAnnotator : CSharpSyntaxRewriter
  {
    private SemanticModel _semanticModel;

    public CastExpressionNullAnnotator (SemanticModel semanticModel)
    {
      _semanticModel = semanticModel;
    }

    public override SyntaxNode? VisitCastExpression (CastExpressionSyntax node)
    {
      var type = node.Type;
      if (type is NullableTypeSyntax)
        return node;

      if (NullUtilities.IsDefinitlyNull (node.Expression, _semanticModel))
        return node.WithType (NullUtilities.MakeNullable (type));

      return base.VisitCastExpression (node);
    }
  }
}