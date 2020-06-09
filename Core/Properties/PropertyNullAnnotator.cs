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
using NullableReferenceTypesRewriter.Utilities;

namespace NullableReferenceTypesRewriter.Properties
{
  public class PropertyNullAnnotator : CSharpSyntaxRewriter
  {
    private readonly SemanticModel _semanticModel;

    public PropertyNullAnnotator (SemanticModel semanticModel)
    {
      _semanticModel = semanticModel;
    }

    public override SyntaxNode? VisitPropertyDeclaration (PropertyDeclarationSyntax node)
    {
      if (HasCanBeNullAttribute (node)
          || IsInClassOrStruct (node)
          && (GetterReturnsNull (node)
              || IsUninitialized (node)))
        return node.WithType (NullUtilities.ToNullable (node.Type));

      return node;
    }

    private bool IsInClassOrStruct (PropertyDeclarationSyntax node)
    {
      var parent = node.Parent;
      return parent is ClassDeclarationSyntax
             || parent is StructDeclarationSyntax;
    }

    private bool IsUninitialized (PropertyDeclarationSyntax node)
    {
      return IsAutoProperty (node)
             && !IsNotNullInitialized (node)
             && !IsNotNullInitializedInConstructor (node);
    }

    private bool IsAutoProperty (BasePropertyDeclarationSyntax node)
    {
      return node.AccessorList?.Accessors.All (
                 accessor => accessor.Body == null
                             && accessor.ExpressionBody == null)
             ?? false;
    }

    private bool GetterReturnsNull (PropertyDeclarationSyntax node)
    {
      var propertySymbol = _semanticModel.GetDeclaredSymbol (node);
      if (propertySymbol.GetMethod == null)
      {
        return false;
      }

      var getter = propertySymbol.GetMethod;
      var getterSyntax = getter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as AccessorDeclarationSyntax;

      if (getterSyntax?.Body != null)
        return NullUtilities.ReturnsNull (getterSyntax.Body.Statements, _semanticModel);
      if (getterSyntax?.ExpressionBody != null)
        return NullUtilities.CanBeNull (getterSyntax.ExpressionBody.Expression, _semanticModel);
      return false;
    }

    private bool IsNotNullInitialized (PropertyDeclarationSyntax node)
    {
      return node.Initializer != null
             && !NullUtilities.CanBeNull (node.Initializer.Value, _semanticModel);
    }

    private bool IsNotNullInitializedInConstructor (PropertyDeclarationSyntax node)
    {
      if (!(node.Parent is ClassDeclarationSyntax parentClass))
        return false;

      var constructors = parentClass.DescendantNodesAndSelf()
          .OfType<ConstructorDeclarationSyntax>()
          .Where (ctor => ctor.Initializer?.ThisOrBaseKeyword.Text != "this");

      var isAssigned = constructors
          .Select (ctor => ctor.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>())
          .All (ctorAssignments => ctorAssignments.Any (assingnment => assingnment?.Left.ToString() == node.Identifier.Text));

      return isAssigned;
    }

    private bool HasCanBeNullAttribute (PropertyDeclarationSyntax node)
    {
      var attributeList = node.AttributeLists.SelectMany (attrLists => attrLists.Attributes);
      return attributeList.FirstOrDefault (attr => attr.Name.ToString().StartsWith ("CanBeNull")) != null;
    }
  }
}