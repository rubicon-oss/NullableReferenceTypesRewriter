using System.Linq;
using System.Reflection;
using NullableReferenceTypesRewriter.ClassFields;
using NUnit.Framework;

namespace NullableReferenceTypesRewriter.Unittests.ClassFields
{
  public class ClassFieldLocatorTest
  {
    [Test]
    public void FieldLocator_LocatesSingleField ()
    {
      var classSource =
          @"public class TestClass {
  private string s;
  public TestClass() {
    s = """";
  }
}";
      var (semanticModel, syntax) = CompiledSourceFileProvider.CompileClass (classSource);
      var fieldLocator = new FieldLocator (syntax, semanticModel);

      var fields = fieldLocator.LocateFields();

      Assert.That (fields, Has.One.Items);
      var field = fields.Single();
      Assert.That (field.Type.ToString(), Is.EqualTo ("string"));
      Assert.That (field.Variables, Has.One.Items);
      Assert.That (field.Variables, Has.One.Items);
      var variable = field.Variables.Single();
      Assert.That (variable.Identifier.ToString(), Is.EqualTo ("s"));
    }

    [Test]
    public void FieldLocator_LocatesMultipleFields ()
    {
      var classSource =
          @"public class TestClass {
  private string s;
  private object o;
  public TestClass() {
    s = """";
  }
}";
      var (semanticModel, syntax) = CompiledSourceFileProvider.CompileClass (classSource);
      var fieldLocator = new FieldLocator (syntax, semanticModel);

      var fields = fieldLocator.LocateFields();

      Assert.That (fields, Has.Exactly (2).Items);
    }

    [Test]
    public void FieldLocator_LocatesNoFields ()
    {
      var classSource =
          @"public class TestClass {
  public TestClass() {
  }
}";
      var (semanticModel, syntax) = CompiledSourceFileProvider.CompileClass (classSource);
      var fieldLocator = new FieldLocator (syntax, semanticModel);

      var fields = fieldLocator.LocateFields();

      Assert.That (fields, Has.Exactly (0).Items);
    }

    [Test]
    public void FieldLocator_LocatesOnlyUnitializedFields ()
    {
      var classSource =
          @"public class TestClass {
  private string s;
  private object o = new object();
  
  public TestClass() {
  }
}";
      var (semanticModel, syntax) = CompiledSourceFileProvider.CompileClass (classSource);
      var fieldLocator = new FieldLocator (syntax, semanticModel);

      var fields = fieldLocator.LocateFields();

      Assert.That (fields, Has.One.Items);
      var field = fields.Single();
      Assert.That (field.Type.ToString(), Is.EqualTo ("string"));
      Assert.That (field.Variables, Has.One.Items);
      Assert.That (field.Variables, Has.One.Items);
      var variable = field.Variables.Single();
      Assert.That (variable.Identifier.ToString(), Is.EqualTo ("s"));
    }

    [Test]
    public void FieldLocator_LocatesFieldsInitializedToNull ()
    {
      var classSource =
          @"public class TestClass {
  private string s = null;
  private object o = new object();
  
  public TestClass() {
  }
}";
      var (semanticModel, syntax) = CompiledSourceFileProvider.CompileClass (classSource);
      var fieldLocator = new FieldLocator (syntax, semanticModel);

      var fields = fieldLocator.LocateFields();

      Assert.That (fields, Has.One.Items);
      var field = fields.Single();
      Assert.That (field.Type.ToString(), Is.EqualTo ("string"));
      Assert.That (field.Variables, Has.One.Items);
      Assert.That (field.Variables, Has.One.Items);
      var variable = field.Variables.Single();
      Assert.That (variable.Identifier.ToString(), Is.EqualTo ("s"));
    }

    [Test]
    public void FieldLocator_IgnoresValueTypes ()
    {
      const string classSource = @"public class Test 
{
  private int i;
  private decimal d;
  private System.DayOfWeek day;
  private string s;

  public Test() {}
}";
      var (semantic, syntax) = CompiledSourceFileProvider.CompileClass (classSource);
      var fieldLocator = new FieldLocator (syntax, semantic);

      var fields = fieldLocator.LocateFields();

      Assert.That (fields, Has.One.Items);
      var field = fields.Single();
      Assert.That (field.Type.ToString(), Is.EqualTo ("string"));

    }

  }
}