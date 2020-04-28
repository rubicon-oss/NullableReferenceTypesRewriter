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

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypesRewriter.ClassFields
{
  public class ConstructorInitializationFilter : CSharpSyntaxRewriter
  {
    private readonly ClassDeclarationSyntax _classDeclaration;
    private readonly IReadOnlyCollection<FieldDeclarationSyntax> _fields;
    private readonly ISet<FieldDeclarationSyntax> _uninitializedFields = new HashSet<FieldDeclarationSyntax>();

    public ConstructorInitializationFilter (ClassDeclarationSyntax classDeclaration, IReadOnlyCollection<FieldDeclarationSyntax> fields)
    {
      _classDeclaration = classDeclaration;
      _fields = fields;
    }

    public override SyntaxNode? VisitConstructorDeclaration (ConstructorDeclarationSyntax node)
    {
      if (node.Initializer != null && node.Initializer.ThisOrBaseKeyword.Text == "this")
        return node;

      var assignments = node.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>().ToArray();

      foreach (var field in _fields)
      {
        if (IsStaticField (field)
            || !IsParameterAssigned (assignments, field.Declaration))
        {
          _uninitializedFields.Add (field);
        }
      }

      return node;
    }

    public IReadOnlyCollection<FieldDeclarationSyntax> GetUnitializedFields ()
    {
      Visit (_classDeclaration);
      return _uninitializedFields.ToArray();
    }

    private static bool IsStaticField (FieldDeclarationSyntax field)
    {
      return field.Modifiers.FirstOrDefault (mod => mod.Text == "static").Kind() != SyntaxKind.None;
    }

    private static bool IsParameterAssigned (IEnumerable<AssignmentExpressionSyntax> assignments, VariableDeclarationSyntax variable)
    {
      return assignments.FirstOrDefault (assingment => assingment.Left.ToString() == variable.Variables.First().Identifier.ValueText) != null;
    }
  }
}