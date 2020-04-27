using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.Utilities;

namespace NullableReferenceTypesRewriter.ClassFields
{
  public class FieldNullaleAnnotator: CSharpSyntaxRewriter
  {
    private readonly ClassDeclarationSyntax _classDeclaration;
    private readonly IReadOnlyCollection<VariableDeclarationSyntax> _uninitializedVariables;

    public FieldNullaleAnnotator (ClassDeclarationSyntax classDeclaration, IReadOnlyCollection<VariableDeclarationSyntax> uninitializedVariables)
    {
      _classDeclaration = classDeclaration;
      _uninitializedVariables = uninitializedVariables;
    }

    public ClassDeclarationSyntax AnnotateFields ()
    {
      return (ClassDeclarationSyntax) Visit (_classDeclaration);
    }

    public override SyntaxNode? VisitFieldDeclaration (FieldDeclarationSyntax node)
    {

      if (_uninitializedVariables.Contains (node.Declaration))
      {
        return node.WithDeclaration (
            node.Declaration.WithType (NullUtilities.ToNullable(node.Declaration.Type)));
      }

      return node;
    }
  }
}