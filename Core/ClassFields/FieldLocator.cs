using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.Utilities;

namespace NullableReferenceTypesRewriter.ClassFields
{
  public class FieldLocator : CSharpSyntaxRewriter
  {
    private readonly ClassDeclarationSyntax _classDeclarationSyntax;
    private readonly List<VariableDeclarationSyntax> _fieldDeclarations = new List<VariableDeclarationSyntax>();
    private readonly SemanticModel _semanticModel;

    public FieldLocator (ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
    {
      _classDeclarationSyntax = classDeclarationSyntax;
      _semanticModel = semanticModel;
    }

    public ReadOnlyCollection<VariableDeclarationSyntax> LocateFields ()
    {
      Visit (_classDeclarationSyntax);
      return new ReadOnlyCollection<VariableDeclarationSyntax> (_fieldDeclarations);
    }

    public override SyntaxNode? VisitFieldDeclaration (FieldDeclarationSyntax node)
    {
      if (!IsReadOnly(node)
          && !DefinesValueTypeField(node.Declaration)
          && node.Declaration.Variables.All (v => HasNoInitializer (v) || IsInitializedToNull (v)))
      {
        _fieldDeclarations.Add (node.Declaration);
      }

      return base.VisitFieldDeclaration (node);
    }

    private bool IsReadOnly (MemberDeclarationSyntax field)
      => field.Modifiers.FirstOrDefault (mod => mod.ToString() == "readonly").Kind() != SyntaxKind.None;

    private bool HasNoInitializer (VariableDeclaratorSyntax variableDeclarator)
      => variableDeclarator.Initializer == null;

    private bool DefinesValueTypeField (VariableDeclarationSyntax declaration)
    {
      var typeSymbol = _semanticModel.GetTypeInfo (declaration.Type).Type as INamedTypeSymbol;
      return typeSymbol == null
          || typeSymbol.IsValueType;
    }

    private bool IsInitializedToNull (VariableDeclaratorSyntax variableDeclarator)
    {
      if (variableDeclarator.Initializer != null)
      {
        return NullUtilities.CanBeNull (variableDeclarator.Initializer.Value, _semanticModel);
      }

      return false;
    }
  }
}