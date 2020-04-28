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
    private readonly List<FieldDeclarationSyntax> _fieldDeclarations = new List<FieldDeclarationSyntax>();
    private readonly SemanticModel _semanticModel;

    public FieldLocator (ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
    {
      _classDeclarationSyntax = classDeclarationSyntax;
      _semanticModel = semanticModel;
    }

    public ReadOnlyCollection<FieldDeclarationSyntax> LocateFields ()
    {
      Visit (_classDeclarationSyntax);
      return new ReadOnlyCollection<FieldDeclarationSyntax> (_fieldDeclarations);
    }

    public override SyntaxNode? VisitFieldDeclaration (FieldDeclarationSyntax node)
    {
      if (!IsReadOnly(node)
          && !DefinesValueTypeField(node.Declaration)
          && node.Declaration.Variables.All (v => HasNoInitializer (v) || IsInitializedToNull (v)))
      {
        _fieldDeclarations.Add (node);
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