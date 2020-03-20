﻿// Copyright (c) rubicon IT GmbH
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.ConsoleApplication.Utilities;

namespace NullableReferenceTypesRewriter.ConsoleApplication
{
  public class MethodNullAnnotator : SemanticCShapSyntaxRewriter
  {
    public MethodNullAnnotator (SemanticModel semanticModel)
        : base (semanticModel)
    {
    }

    public override SyntaxNode? VisitMethodDeclaration (MethodDeclarationSyntax node)
    {
      if (NullUtilities.DoesReturnVoid (node))
        return node;
      if (node.Body == null)
        return node;
      if (node.Body?.Statements.Count == 0)
        return node;

      var returnsNull = HasCanBeNullAttribute (node)
                        || NullUtilities.ReturnsNull (node, Model);

      if (!returnsNull)
        return node;

      return NullUtilities.MakeNullReturning (node, Model);
    }

    private static bool HasCanBeNullAttribute (MethodDeclarationSyntax node)
    {
      return node.AttributeLists.SelectMany (list => list.Attributes)
          .Any (attr => attr.Name.ToString().Contains ("CanBeNull"));
    }
  }
}