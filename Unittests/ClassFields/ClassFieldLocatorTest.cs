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
using NullableReferenceTypesRewriter.ClassFields;
using NUnit.Framework;

namespace NullableReferenceTypesRewriter.UnitTests.ClassFields
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