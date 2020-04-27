using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.ClassFields;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NullableReferenceTypesRewriter.Unittests.ClassFields
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