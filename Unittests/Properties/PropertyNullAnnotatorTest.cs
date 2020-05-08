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

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.Properties;
using NUnit.Framework;

namespace NullableReferenceTypesRewriter.UnitTests.Properties
{
  [TestFixture]
  public class PropertyNullAnnotatorTest
  {
    [Test]
    [TestCase ("[CanBeNull] public string A {get; set;}")]
    [TestCase ("[CanBeNull] public string A {get;}")]
    [TestCase (@"[CanBeNull] public string A {get; set;} = """";")]
    [TestCase (@"[CanBeNull] public string A => """";")]
    public void PropertyAnnotator_WithCanBeNullAttribute_AnnotatesNullable (string classContent)
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileInClass ("TestClass", classContent);
      var annotator = new PropertyNullAnnotator (semantic);

      var annotated = annotator.Visit (syntax);

      Assert.That (annotated.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>(), Has.One.Items);
      var property = annotated.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>().Single();
      Assert.That (property.Type, Is.InstanceOf<NullableTypeSyntax>());
      Assert.That (((NullableTypeSyntax) property.Type).ElementType.ToString(), Is.EqualTo ("string"));
    }

    [Test]
    [TestCase ("private string? _s; public string A { get { return _s; }; set {_s = value; }; } }")]
    [TestCase ("public string A { get { return null; }; } }")]
    [TestCase ("private string? T() { return null; } public string A { get { return T(); }; } }")]
    public void PropertyAnnotator_WithBlockGetterReturnsNull_AnnotatesNullable (string classContent)
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileInClass ("TestClass", classContent);
      var annotator = new PropertyNullAnnotator (semantic);

      var annotated = annotator.Visit (syntax);

      Assert.That (annotated.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>(), Has.One.Items);
      var property = annotated.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>().Single();
      Assert.That (property.Type, Is.InstanceOf<NullableTypeSyntax>());
      Assert.That (((NullableTypeSyntax) property.Type).ElementType.ToString(), Is.EqualTo ("string"));
    }

    [Test]
    [TestCase (@"public string A { get; } = ""string"";")]
    [TestCase (@"public string A => ""string"";")]
    [TestCase (
        @"public TestClass () {}
        public string A { get; } = ""string"";")]
    public void PropertyAnnotator_WithInitializedAutoProperty_DoesNotAnnotateNullable (string classContent)
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileInClass ("TestClass", classContent);
      var annotator = new PropertyNullAnnotator (semantic);

      var annotated = annotator.Visit (syntax);

      Assert.That (annotated, Is.EqualTo (syntax));
    }

    [Test]
    public void PropertyAnnotator_WithCtorInitializedAutoProperty_DoesNotAnnotateNullable ()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileInClass (
          "TestClass",
          @"public TestClass ()
          {
            A = ""string"";
          }
          public string A {get;}");
      var annotator = new PropertyNullAnnotator (semantic);

      var annotated = annotator.Visit (syntax);

      Assert.That (annotated, Is.EqualTo (syntax));
    }

    [Test]
    public void PropertyAnnotator_WithUninitializedAutoProperty_AnnotatesNullable ()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileInClass (
          "TestClass",
          @"public TestClass () {}
          public TestClass (string s) { A = s; }
          public string A {get;}");
      var annotator = new PropertyNullAnnotator (semantic);

      var annotated = annotator.Visit (syntax);

      Assert.That (annotated.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>(), Has.One.Items);
      var property = annotated.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>().Single();
      Assert.That (property.Type, Is.InstanceOf<NullableTypeSyntax>());
      Assert.That (((NullableTypeSyntax) property.Type).ElementType.ToString(), Is.EqualTo ("string"));
    }

    [Test]
    public void PropertyAnnotator_WithInitializedInMultipleCtorsAutoProperty_DoesNotAnnotateNullable ()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileInClass (
          "TestClass",
          @"public TestClass () { A = """";}
          public TestClass (string s) { A = s; }
          public TestClass (string s, int i): this(s) { }
          public string A {get;}");
      var annotator = new PropertyNullAnnotator (semantic);

      var annotated = annotator.Visit (syntax);

      Assert.That (annotated, Is.EqualTo (syntax));
    }

    [Test]
    public void PropertyAnnotator_WithReadonlyPropertyInInterface_DoesNotAnnotateNullable ()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileInNameSpace("TestNamespace",
          @"public interface ITest
{
  string Property { get; }
}");
      var annotator = new PropertyNullAnnotator (semantic);

      var annotated = annotator.Visit (syntax);

      Assert.That (annotated, Is.EqualTo (syntax));
    }
  }
}