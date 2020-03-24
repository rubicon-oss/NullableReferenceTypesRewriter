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
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.MethodArguments;
using NUnit.Framework;

namespace NullableReferenceTypesRewriter.Unittests.MethodArguments
{
  public class MethodArgumentFromInvocationNullAnnotatorTest
  {
    private const string c_template = "%TEMPLATE%";

    private const string c_argumentTemplate =
        @"public class ArgumentTest
{{
  public void TestMethod (int structType, object referenceType1, double structType2, string referenceType2)
  {{
  }}

  public void CallSite ()
  {{
    {0};
  }}
}}";

    [Test]
    public void MethodArgumentFromInvocationNullAnnotator_InvokesCallbackWithSingleNullableArgument ()
    {
      var source = string.Format (c_argumentTemplate, "TestMethod (1, null, 2.3, new string())");
      var (semanticModel, classSyntax) = CompiledSourceFileProvider.CompileClass (source);
      var callSiteMethod = classSyntax.Members[1] as MethodDeclarationSyntax;
      var list = new List<ParameterSyntax>();
      var rewriter = new MethodArgumentFromInvocationNullAnnotator (semanticModel, p => list.Add (p));

      var rewritten = rewriter.Visit (callSiteMethod);

      Assert.That (rewritten, Is.EqualTo (callSiteMethod));
      Assert.That (list, Has.One.Items);
      Assert.That (list[0].Identifier.ToString(), Is.EqualTo ("referenceType1"));
    }

    [Test]
    public void MethodArgumentFromInvocationNullAnnotator_InvokesCallbackWithMultipleNullableArgument ()
    {
      var source = string.Format (c_argumentTemplate, "TestMethod (1, null, 2.3, null)");
      var (semanticModel, classSyntax) = CompiledSourceFileProvider.CompileClass (source);
      var callSiteMethod = classSyntax.Members[1] as MethodDeclarationSyntax;
      var list = new List<ParameterSyntax>();
      var rewriter = new MethodArgumentFromInvocationNullAnnotator (semanticModel, p => list.Add (p));

      var rewritten = rewriter.Visit (callSiteMethod);

      Assert.That (rewritten, Is.EqualTo (callSiteMethod));
      Assert.That (list, Has.Count.EqualTo (2));

      Assert.That (list[0].Identifier.ToString(), Is.EqualTo ("referenceType1"));
      Assert.That (list[1].Identifier.ToString(), Is.EqualTo ("referenceType2"));
    }
  }
}