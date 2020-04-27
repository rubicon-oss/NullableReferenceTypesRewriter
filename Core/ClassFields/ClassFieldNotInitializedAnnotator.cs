using System;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypesRewriter.ClassFields
{
  public class ClassFieldNotInitializedAnnotator : CSharpSyntaxRewriter
  {
    private readonly SemanticModel _semanticModel;

    public ClassFieldNotInitializedAnnotator (SemanticModel semanticModel)
    {
      _semanticModel = semanticModel;
    }

    public override SyntaxNode? VisitClassDeclaration (ClassDeclarationSyntax node)
    {
      var fields = new FieldLocator (node, _semanticModel).LocateFields();

      var uninitializedFields = new ConstructorInitializationFilter (node, fields).GetUnitializedFields();

      return new FieldNullaleAnnotator (node, uninitializedFields).AnnotateFields();
    }
  }
}