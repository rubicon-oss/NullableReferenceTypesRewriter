using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.MethodArguments;
using NUnit.Framework;

namespace NullableReferenceTypesRewriter.UnitTests.MethodArguments
{
  [TestFixture]
  public class MethodParameterNullAnnotatorTest
  {
    [Test]
    public void MethodParameterNullAnnotator_WithNullableList_AnnotatesParameters ()
    {
      var (_, methodSyntax) = CompiledSourceFileProvider.CompileMethod (
          "void T (object o1, object o2, string s1, string s2, int i1, double d1) {}");
      var nullableParameter = methodSyntax.ParameterList.Parameters.Where (parameter => parameter.Identifier.ToString().Contains ("2"));
      var rewriter = new MethodParameterNullAnnotator (nullableParameter.ToList());

      var rewritten = rewriter.Visit (methodSyntax);

      Assert.That (rewritten, Is.InstanceOf<MethodDeclarationSyntax>());
      var rewrittenMethod = (rewritten as MethodDeclarationSyntax)!;
      Assert.That (rewrittenMethod.ParameterList.Parameters[0], Is.EqualTo (methodSyntax.ParameterList.Parameters[0]).Using<SyntaxNode>( (p1,p2) => p1.IsEquivalentTo(p2)));
      Assert.That (rewrittenMethod.ParameterList.Parameters[2], Is.EqualTo (methodSyntax.ParameterList.Parameters[2]).Using<SyntaxNode>( (p1,p2) => p1.IsEquivalentTo(p2)));
      Assert.That (rewrittenMethod.ParameterList.Parameters[4], Is.EqualTo (methodSyntax.ParameterList.Parameters[4]).Using<SyntaxNode>( (p1,p2) => p1.IsEquivalentTo(p2)));
      Assert.That (rewrittenMethod.ParameterList.Parameters[5], Is.EqualTo (methodSyntax.ParameterList.Parameters[5]).Using<SyntaxNode>( (p1,p2) => p1.IsEquivalentTo(p2)));
      Assert.That(rewrittenMethod.ParameterList.Parameters[1].Type, Is.InstanceOf<NullableTypeSyntax>());
      Assert.That(rewrittenMethod.ParameterList.Parameters[3].Type, Is.InstanceOf<NullableTypeSyntax>());
    }

    [Test]
    public void MethodParameterNullAnnotator_WithEmptyList_ReturnsInput ()
    {
      var (_, methodSyntax) = CompiledSourceFileProvider.CompileMethod (
          "void T (object o1, object o2, string s1, string s2, int i1, double d1) {}");
      var rewriter = new MethodParameterNullAnnotator (new ParameterSyntax[0]);

      var rewritten = rewriter.Visit (methodSyntax);

      Assert.That(rewritten, Is.EqualTo(methodSyntax));
    }
    [Test]
    public void MethodParameterNullAnnotator_AnnotatesCanBeNullParameters ()
    {
      var (_, methodSyntax) = CompiledSourceFileProvider.CompileMethod (
          "void T (object o1, [CanBeNull] object o2, string s1, [CanBeNull] string s2, int i1, double d1) {}");
      var rewriter = new MethodParameterNullAnnotator (new ParameterSyntax[0]);

      var rewritten = rewriter.Visit (methodSyntax);

      Assert.That (rewritten, Is.InstanceOf<MethodDeclarationSyntax>());
      var rewrittenMethod = (rewritten as MethodDeclarationSyntax)!;
      Assert.That (rewrittenMethod.ParameterList.Parameters[0], Is.EqualTo (methodSyntax.ParameterList.Parameters[0]).Using<SyntaxNode>( (p1,p2) => p1.IsEquivalentTo(p2)));
      Assert.That (rewrittenMethod.ParameterList.Parameters[2], Is.EqualTo (methodSyntax.ParameterList.Parameters[2]).Using<SyntaxNode>( (p1,p2) => p1.IsEquivalentTo(p2)));
      Assert.That (rewrittenMethod.ParameterList.Parameters[4], Is.EqualTo (methodSyntax.ParameterList.Parameters[4]).Using<SyntaxNode>( (p1,p2) => p1.IsEquivalentTo(p2)));
      Assert.That (rewrittenMethod.ParameterList.Parameters[5], Is.EqualTo (methodSyntax.ParameterList.Parameters[5]).Using<SyntaxNode>( (p1,p2) => p1.IsEquivalentTo(p2)));
      Assert.That(rewrittenMethod.ParameterList.Parameters[1].Type, Is.InstanceOf<NullableTypeSyntax>());
      Assert.That(rewrittenMethod.ParameterList.Parameters[3].Type, Is.InstanceOf<NullableTypeSyntax>());

    }
  }
}