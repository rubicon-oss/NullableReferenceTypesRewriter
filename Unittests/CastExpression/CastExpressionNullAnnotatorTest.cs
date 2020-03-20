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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.ConsoleApplication.CastExpression;
using NUnit.Framework;

namespace NUnit2To3SyntaxConverter.Unittests.CastExpression
{
  public class CastExpressionNullAnnotatorTest
  {
    private const string c_template = "%TEMPLATE%";

    private const string c_castTemplate =
        "public void Test() { " +
        "var a = %TEMPLATE%;" +
        "}";

    [Test]
    [TestCase ("(object) new object()")]
    [TestCase ("(string) new object()")]
    public void CastExpressionNullAnnotator_DoesNotAnnotateNonNullableCast (string castExpression)
    {
      var source = c_castTemplate.Replace (c_template, castExpression);
      var (semanticModel, method) = CompiledSourceFileProvider.CompileMethod (source);
      var stmt = (method.Body!.Statements.First() as LocalDeclarationStatementSyntax)!;
      var castExpressionSyntax = (stmt.Declaration.Variables.First().Initializer!.Value as CastExpressionSyntax)!;
      var rewriter = new CastExpressionNullAnnotator (semanticModel);

      var rewritten = rewriter.Visit (castExpressionSyntax);

      Assert.That (rewritten, Is.InstanceOf<CastExpressionSyntax>());
      Assert.That (rewritten, Is.EqualTo (castExpressionSyntax));
    }

    [Test]
    [TestCase ("(object) null")]
    [TestCase ("(string) null")]
    [TestCase ("(string) (new System.Random().Next() < 0 ? null : new object())")]
    public void CastExpressionNullAnnotator_DoesAnnotateNullableCast (string castExpression)
    {
      var source = c_castTemplate.Replace (c_template, castExpression);
      var (semanticModel, method) = CompiledSourceFileProvider.CompileMethod (source);
      var stmt = (method.Body!.Statements.First() as LocalDeclarationStatementSyntax)!;
      var castExpressionSyntax = (stmt.Declaration.Variables.First().Initializer!.Value as CastExpressionSyntax)!;
      var rewriter = new CastExpressionNullAnnotator (semanticModel);

      var rewritten = rewriter.Visit (castExpressionSyntax);

      Assert.That (rewritten, Is.InstanceOf<CastExpressionSyntax>());
      var rewrittenCastExpression = rewritten as CastExpressionSyntax;
      Assert.That (rewrittenCastExpression!.Type, Is.InstanceOf<NullableTypeSyntax>());
    }
  }
}