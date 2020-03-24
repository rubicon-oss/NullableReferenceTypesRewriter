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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.LocalDeclaration;
using NUnit.Framework;

namespace NullableReferenceTypesRewriter.Unittests.LocalDeclaration
{
  public class LocalDeclarationNullAnnotatorTest
  {
    [Test]
    [TestCase ("var obj = new object();")]
    [TestCase ("var obj = null;")]
    [TestCase ("var obj = null;")]
    public void LocalDeclarationNullAnnotator_DoesNotAnnotateDeclarationsToVar (string declarationSource)
    {
      var (model, syntax) = CompiledSourceFileProvider.CompileStatement (declarationSource);
      var rewriter = new LocalDeclarationNullAnnotator (model);

      var rewritten = rewriter.Visit (syntax) as LocalDeclarationStatementSyntax;

      Assert.That (rewritten, Is.Not.Null);
      Assert.That (rewritten, Is.EqualTo (syntax));
    }

    [Test]
    [TestCase ("string obj = null;")]
    [TestCase ("object obj = null;")]
    [TestCase ("MyClassNotInScope obj = null;")]
    public void LocalDeclarationNullAnnotator_DoesAnnotateDeclarationsToReferenceTypes (string declarationSource)
    {
      var (model, syntax) = CompiledSourceFileProvider.CompileStatement (declarationSource);
      var rewriter = new LocalDeclarationNullAnnotator (model);

      var rewritten = rewriter.Visit (syntax) as LocalDeclarationStatementSyntax;

      Assert.That (rewritten, Is.Not.Null);
      Assert.That (rewritten!.Declaration.Type, Is.InstanceOf<NullableTypeSyntax>());
    }

    [Test]
    [TestCase ("string s = returnNull();")]
    [TestCase ("object o = returnNull();")]
    public void LocalDecalrationNullAnnotator_DoesAnnotateDeclarationsCallingNullableMethods (string declarationSource)
    {
      var classContentTemplate =
          @"public string? returnNull() {
            return null;
          }
          public void TestMethod() {" +
          $"\r\n  {declarationSource}\r\n" +
          "}\r\n";

      var (semanticModel, syntaxNode) = CompiledSourceFileProvider.CompileInClass ("TestClass", classContentTemplate);
      var testMethod = syntaxNode.DescendantNodes().OfType<MethodDeclarationSyntax>().Single (m => m.Identifier.ToString() == "TestMethod");
      var declaration = testMethod.Body!.Statements.First();
      var rewriter = new LocalDeclarationNullAnnotator (semanticModel);

      var rewritten = rewriter.Visit (declaration) as LocalDeclarationStatementSyntax;

      Assert.That (rewritten, Is.Not.Null);
      Assert.That (rewritten!.Declaration.Type, Is.InstanceOf<NullableTypeSyntax>());
    }
  }
}