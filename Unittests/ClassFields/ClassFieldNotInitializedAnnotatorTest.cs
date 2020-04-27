using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.ClassFields;
using NUnit.Framework;

namespace NullableReferenceTypesRewriter.Unittests.ClassFields
{
  public class ClassFieldNotInitializedAnnotatorTest
  {
    private const string c_classWithOneUnitializedField =
        @"public class Test 
{
  private int i;
  private string s1;
  private string s2;
  private string s3 = """";
  public Test()
  {
    s2 = """";
  }
}";

    private const string c_classWithOneUnitializedFieldAnnotated =
        @"public class Test 
{
  private int i;
  private string? s1;
  private string s2;
  private string s3 = """";
  public Test()
  {
    s2 = """";
  }
}";

    [Test]
    public void ClassFieldNotInitializedAnnotator_DoesAnnotateSingleUninitializedField ()
    {
      var (semantic, syntax) = CompiledSourceFileProvider.CompileClass (c_classWithOneUnitializedField);
      var (_, expected) = CompiledSourceFileProvider.CompileClass (c_classWithOneUnitializedFieldAnnotated);
      var annotator = new ClassFieldNotInitializedAnnotator(semantic);

      var annotatedClass = annotator.Visit (syntax);

      Assert.That (annotatedClass.ToString(), Is.EqualTo (expected.ToString()));
    }
  }
}