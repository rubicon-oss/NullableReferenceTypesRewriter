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
using NullableReferenceTypesRewriter.MethodReturn;
using NUnit.Framework;

namespace NullableReferenceTypesRewriter.UnitTests.MethodReturn
{
  [TestFixture]
  public class MethodReturnNullAnnotatorTest
  {
    private const string c_classTemplate =
        @"public class TestClass
{{
  private object? NullableAnnotatedReferenceMethod() {{ return null; }}
  private object  NonNullableReferenceMethod() {{ return new object(); }}

  private int     NullableValueMethod() {{ return null; }}
  private int     NonNullableValueMethod() {{ return 1; }}

  private object  NonNullableBoxedValueMethod() {{ return 1; }}
  private object? NullableBoxedValueMethod() {{ return 1; }}

  {0}
}}";

    [Test]
    [TestCase ("object t() { return null; }")]
    [TestCase ("object t(int i) { if (i == 1) { return null; } else { return new object();} }")]
    public void MethodNullReturnAnnotator_AnnotatesNullLiteralReturningMethod (string methodSource)
    {
      var source = string.Format (c_classTemplate, methodSource);
      var (model, syntax) = CompiledSourceFileProvider.CompileMethodInClass (source, "t");
      var rewriter = new MethodReturnNullAnnotator (model);

      var rewritten = (MethodDeclarationSyntax) rewriter.Visit (syntax);

      Assert.That (rewritten.ReturnType, Is.InstanceOf<NullableTypeSyntax>());
    }


    [Test]
    [TestCase ("object t() { return NullableAnnotatedReferenceMethod(); }")]
    [TestCase ("object t() { return NullableBoxedValueMethod(); }")]
    [TestCase ("object t(int i) { if (i == 1) { return NullableAnnotatedReferenceMethod(); } else { return new object();} }")]
    public void MethodNullReturnAnnotator_AnnotatesNullableAnnotatedReturningMethod (string methodSource)
    {
      var source = string.Format (c_classTemplate, methodSource);
      var (model, syntax) = CompiledSourceFileProvider.CompileMethodInClass (source, "t");
      var rewriter = new MethodReturnNullAnnotator (model);

      var rewritten = (MethodDeclarationSyntax) rewriter.Visit (syntax);

      Assert.That (rewritten.ReturnType, Is.InstanceOf<NullableTypeSyntax>());
    }

    [Test]
    [TestCase ("object t() { return new object(); }")]
    [TestCase ("object t() { return NonNullableReferenceMethod(); }")]
    [TestCase ("object t() { return NonNullableValueMethod(); }")]
    [TestCase ("object t() { return NonNullableBoxedValueMethod(); }")]
    public void MethodNullReturnAnnotator_DoesNotAnnotateNonNullReturningMethod (string methodSource)
    {
      var source = string.Format (c_classTemplate, methodSource);
      var (model, syntax) = CompiledSourceFileProvider.CompileMethodInClass (source, "t");
      var rewriter = new MethodReturnNullAnnotator (model);

      var rewritten = (MethodDeclarationSyntax) rewriter.Visit (syntax);

      Assert.That (rewritten.ReturnType, Is.Not.InstanceOf<NullableTypeSyntax>());
    }

    [Test]
    [TestCase ("[CanBeNull] object t() { return null; }")]
    [TestCase ("[CanBeNull] object t() { return NonNullableReferenceMethod(); }")]
    [TestCase ("[CanBeNull] object t() { return new object(); }")]
    public void MethodNullReturnAnnotator_DoesAnnotateMethodsWithCanBeNullAttribute (string methodSource)

    {
      var source = string.Format (c_classTemplate, methodSource);
      var (model, syntax) = CompiledSourceFileProvider.CompileMethodInClass (source, "t");
      var rewriter = new MethodReturnNullAnnotator (model);

      var rewritten = (MethodDeclarationSyntax) rewriter.Visit (syntax);

      Assert.That (rewritten.ReturnType, Is.InstanceOf<NullableTypeSyntax>());
    }

    [Test]
    [TestCase ("void object t() { return 0 == 0 ? new object() : null; }")]
    public void MethodNullReturnAnnotator_DoesNotAnnotateProvablyNonNullReturningMethods (string methodSource)
    {
      var source = string.Format (c_classTemplate, methodSource);
      var (model, syntax) = CompiledSourceFileProvider.CompileMethodInClass (source, "t");
      var rewriter = new MethodReturnNullAnnotator (model);

      var rewritten = (MethodDeclarationSyntax) rewriter.Visit (syntax);

      Assert.That (rewritten.ReturnType, Is.Not.InstanceOf<NullableTypeSyntax>());
    }
  }
}
