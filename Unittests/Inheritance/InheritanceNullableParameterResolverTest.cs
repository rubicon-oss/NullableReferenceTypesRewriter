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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using NullableReferenceTypesRewriter.Inheritance;
using NUnit.Framework;

namespace NullableReferenceTypesRewriter.UnitTests.Inheritance
{
  [TestFixture]
  public class InheritanceNullableParameterResolverTest
  {
    private readonly string _testSolutionPath = Path.Combine (TestContext.CurrentContext.TestDirectory, "resources/TestSolution/TestSolution.sln");
    private Solution _testSolution = null!;
    private Project _testProject = null!;
    private Compilation _compilation = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp ()
    {
      var msBuild = MSBuildLocator.QueryVisualStudioInstances().First();
      MSBuildLocator.RegisterInstance (msBuild);
      using var workspace = MSBuildWorkspace.Create();
      _testSolution = await workspace.OpenSolutionAsync (_testSolutionPath);
      _testProject = _testSolution.Projects.Single();
      _compilation = await _testProject.GetCompilationAsync() ??
                     throw new InvalidOperationException ("Unable to compile TestSolution");
    }

    private (Document, SyntaxNode, SemanticModel) LoadInterface (string interfaceName)
    {
      var document = _testProject.Documents.Single (doc => doc.Name == interfaceName);
      var syntax = document.GetSyntaxRootAsync().GetAwaiter().GetResult()
                   ?? throw new InvalidOperationException ($"Unable to load syntax tree of {document.Name}");
      var semanticModel = _compilation.GetSemanticModel (syntax.SyntaxTree);
      return (document, syntax, semanticModel);
    }

    [Test]
    public void GetNullableImplementationParameters_WithNullableInterface_ResolvesMultipleImplementations ()
    {
      var (document, syntax, semantic) = LoadInterface ("Interface2.cs");
      var parameterResolver = new InheritanceNullableParameterResolver (document, semantic);

      parameterResolver.Visit (syntax);
      var parameters = parameterResolver.GetNullableImplementationParameters().ToList();

      Assert.That (parameters, Has.Exactly (2).Items);
      Assert.That (parameters[0].Item2, Is.EquivalentTo (new[] { "arg0" }));
      Assert.That (parameters[1].Item2, Is.EquivalentTo (new[] { "arg0" }));
    }


    [Test]
    public void GetNullableImplementationParameters_WithNullReturningInterface_ResolvesNullableImplementations ()
    {
      var (document, syntax, semantic) = LoadInterface ("NullReturningInterface.cs");
      var parameterResolver = new InheritanceNullableParameterResolver (document, semantic);

      parameterResolver.Visit (syntax);
      var parameters = parameterResolver.GetNullableImplementationParameters().ToList();

      Assert.That (parameters, Has.Exactly (1).Items);
      Assert.That (parameters[0].Item2, Is.EquivalentTo (new[] { "#return" }));
    }

    [Test]
    public void GetNullableInterfaceParameters_ResolvesNullableReturn ()
    {
      var (document, syntax, semantic) = LoadInterface ("ReturningInterface.cs");
      var parameterResolver = new InheritanceNullableParameterResolver (document, semantic);

      parameterResolver.Visit (syntax);
      var parameters = parameterResolver.GetNullableInterfaceParameters().ToList();

      Assert.That (parameters, Has.Exactly (1).Items);
      Assert.That (parameters[0].Item2, Is.EquivalentTo (new[] { "#return" }));
    }

    [Test]
    public void GetNullableInterfaceParameters_WithMultipleNullableImplementations_ResolvesNullableInterface ()
    {
      var (document, syntax, semantic) = LoadInterface ("Interface1.cs");
      var parameterResolver = new InheritanceNullableParameterResolver (document, semantic);

      parameterResolver.Visit (syntax);
      var parameters = parameterResolver.GetNullableInterfaceParameters().ToList();

      Assert.That (parameters, Has.Exactly (1).Items);
      Assert.That (parameters[0].Item2, Has.Exactly (2).Items);
      Assert.That (parameters[0].Item2, Is.EquivalentTo (new[] { "arg1", "arg2" }));
    }

    [Test]
    public void GetNullableInterfaceParameters_WithNullableChilClass_ResolvesNullableBaseClass ()
    {
      var (document, syntax, semantic) = LoadInterface ("BaseClass.cs");
      var parameterResolver = new InheritanceNullableParameterResolver (document, semantic);

      parameterResolver.Visit (syntax);
      var parameters = parameterResolver.GetNullableInterfaceParameters().ToList();

      Assert.That (parameters, Has.Exactly (1).Items);
      Assert.That (parameters[0].Item2, Is.EquivalentTo (new[] { "#return", "arg0" }));
    }
  }
}