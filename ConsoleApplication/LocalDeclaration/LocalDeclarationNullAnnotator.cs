using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.ConsoleApplication.Utilities;

namespace NullableReferenceTypesRewriter.ConsoleApplication
{
  public class LocalDeclarationNullAnnotator : CSharpSyntaxRewriter
  {
    private SemanticModel _semanticModel;

    public LocalDeclarationNullAnnotator (SemanticModel semanticModel)
    {
      _semanticModel = semanticModel;
    }

    public override SyntaxNode? VisitLocalDeclarationStatement (LocalDeclarationStatementSyntax node)
    {
      if (node.Declaration.Type.IsVar) return node;

      if (node.Declaration.Type is NullableTypeSyntax)
        return node;

      var type = node.Declaration.Type;
      var isNullable = node.Declaration.Variables.ToList()
          .Where (variable => variable.Initializer != null)
          .Any (variable => NullUtilities.IsDefinitlyNull (variable.Initializer!.Value, _semanticModel));

      if (isNullable)
      {
        return node.WithDeclaration (node.Declaration.WithType (NullUtilities.MakeNullable (type)));
      }
      else
      {
        return node;
      }
    }
  }
}