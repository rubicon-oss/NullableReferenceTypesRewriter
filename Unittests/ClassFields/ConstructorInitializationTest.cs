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
using NullableReferenceTypesRewriter.ClassFields;
using NUnit.Framework;

namespace NullableReferenceTypesRewriter.UnitTests.ClassFields
{
  public class ConstructorInitializationTest
  {
    private string c_constructorTemplate =
        @"public class TestClass 
{{
  private string field1;
  private string field2;
  private string field3;

  public TestClass() 
    : this(1)
  {{
    {0}
  }}

  private TestClass(int i) 
  {{
    {1}
  }}
  
}}";

    private string c_multipleConstructorTemplate =
        @"public class TestClass 
{{
  private string field1;
  private string field2;
  private string field3;

  public TestClass(string f1, string f2, string f3)
  {{
    {0}
  }}

  private TestClass(string f1, string f2) 
  {{
    {1}
  }}

  private TestClass() 
  {{
    {2}
  }}
  
}}";

    [Test]
    public void ConstructorInitializationFilter_WithNoInitializations_FiltersNothing()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileClass (string.Format (c_constructorTemplate, "", ""));
      var (f1, f2, f3) = GetTestFields (syntax);
      var constructorInitFilter = new ConstructorInitializationFilter (syntax, new[] { f1, f2, f3 });

      var uninitialized = constructorInitFilter.GetUnitializedFields();

      Assert.That (uninitialized, Has.Exactly (3).Items);
      Assert.That (uninitialized, Is.EquivalentTo (new[] { f1, f2, f3 }));
    }

    [Test]
    public void ConstructorInitializationFilter_WithInitializerInOneCtor ()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileClass (string.Format (c_constructorTemplate, "", @"field1 = ""hello""; "));
      var (f1, f2, f3) = GetTestFields (syntax);
      var constructorInitFilter = new ConstructorInitializationFilter (syntax, new[] { f1, f2, f3 });

      var uninitialized = constructorInitFilter.GetUnitializedFields();

      Assert.That (uninitialized, Has.Exactly (2).Items);
      Assert.That (uninitialized, Is.EquivalentTo (new[] {f2, f3 }));
    }

    [Test]
    public void ConstructorInitializationFilter_WithInitializerInCtorOverload_IsNotRecognized ()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileClass (string.Format (c_constructorTemplate, @"field1 = ""hello""; ", @"field2 = """";"));
      var (f1, f2, f3) = GetTestFields (syntax);
      var constructorInitFilter = new ConstructorInitializationFilter (syntax, new[] { f1, f2, f3 });

      var uninitialized = constructorInitFilter.GetUnitializedFields();

      Assert.That (uninitialized, Has.Exactly (2).Items);
      Assert.That (uninitialized, Is.EquivalentTo (new[] {f1, f3 }));
    }

    [Test]
    public void ConstructorInitializationFilter_WithMultipleValidCtors ()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileClass (string.Format (c_multipleConstructorTemplate,
          "field1 = \"hello\"; \r\n" +
          "field2 = \"\"; \r\n" +
          "field3 = \"\"; \r\n",

          "field1 = \"hello\"; \r\n" +
          "field2 = \"\"; \r\n" +
          "field3 = \"\"; \r\n",

          "field1 = \"hello\"; \r\n" +
          "field2 = \"\"; \r\n" +
          "field3 = \"\"; \r\n"));

      var (f1, f2, f3) = GetTestFields (syntax);
      var constructorInitFilter = new ConstructorInitializationFilter (syntax, new[] { f1, f2, f3 });

      var uninitialized = constructorInitFilter.GetUnitializedFields();

      Assert.That (uninitialized, Is.Empty);
    }

    [Test]
    public void ConstructorInitializationFilter_WithMultipleValidCtors_MissingInitInOneCtor ()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileClass (string.Format (c_multipleConstructorTemplate,
          "field1 = \"hello\"; \r\n" +
          "field2 = \"\"; \r\n" +
          "field3 = \"\"; \r\n",

          "field1 = \"hello\"; \r\n" +
          "field3 = \"\"; \r\n",

          "field1 = \"hello\"; \r\n" +
          "field2 = \"\"; \r\n" +
          "field3 = \"\"; \r\n"));

      var (f1, f2, f3) = GetTestFields (syntax);
      var constructorInitFilter = new ConstructorInitializationFilter (syntax, new[] { f1, f2, f3 });

      var uninitialized = constructorInitFilter.GetUnitializedFields();

      Assert.That (uninitialized, Has.Exactly (1).Items);
      Assert.That (uninitialized, Is.EquivalentTo (new[] { f2 }));
    }

    [Test]
    public void ConstructorInitializationFilter_WithMultipleValidCtors_MissingInitInMultipleCtors ()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileClass (string.Format (c_multipleConstructorTemplate,
          "field1 = \"hello\"; \r\n" +
          "field3 = \"\"; \r\n",

          "field1 = \"hello\"; \r\n" +
          "field3 = \"\"; \r\n",

          "field1 = \"hello\"; \r\n" +
          "field2 = \"\"; \r\n"));

      var (f1, f2, f3) = GetTestFields (syntax);
      var constructorInitFilter = new ConstructorInitializationFilter (syntax, new[] { f1, f2, f3 });

      var uninitialized = constructorInitFilter.GetUnitializedFields();

      Assert.That (uninitialized, Has.Exactly (2).Items);
      Assert.That (uninitialized, Is.EquivalentTo (new[] { f2, f3 }));
    }

    private static (VariableDeclarationSyntax, VariableDeclarationSyntax, VariableDeclarationSyntax) GetTestFields (ClassDeclarationSyntax @class)
    {
      var members = @class.Members.OfType<FieldDeclarationSyntax>().Select (f => f.Declaration).ToArray();
      return (members[0], members[1], members[2]);
    }
  }
}