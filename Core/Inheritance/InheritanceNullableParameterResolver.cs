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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace NullableReferenceTypesRewriter.Inheritance
{
  public class InheritanceNullableParameterResolver : CSharpSyntaxRewriter
  {
    private readonly Document _document;

    private readonly Dictionary<IMethodSymbol, IReadOnlyCollection<IMethodSymbol>> _interfaceImplementations =
        new Dictionary<IMethodSymbol, IReadOnlyCollection<IMethodSymbol>>();

    private readonly SemanticModel _semanticModel;

    public InheritanceNullableParameterResolver (Document document, SemanticModel semanticModel)
    {
      _document = document;
      _semanticModel = semanticModel;
    }

    public override SyntaxNode? VisitMethodDeclaration (MethodDeclarationSyntax node)
    {
      var symbol = (IMethodSymbol) _semanticModel.GetDeclaredSymbol (node);

      var implementations = SymbolFinder.FindImplementationsAsync (symbol, _document.Project.Solution).GetAwaiter().GetResult().Cast<IMethodSymbol>().ToArray();

      if (implementations.Length == 0)
        implementations = SymbolFinder.FindOverridesAsync (symbol, _document.Project.Solution).GetAwaiter().GetResult()
            .Cast<IMethodSymbol>().ToArray();

      if (implementations.Length > 0)
      {
        if (_interfaceImplementations.TryGetValue (symbol, out var foundImplementations))
          _interfaceImplementations.Add (symbol, foundImplementations.Concat (implementations).ToArray());
        else
          _interfaceImplementations[symbol] = implementations;
      }

      return base.VisitMethodDeclaration (node);
    }

    public IEnumerable<(SyntaxReference, string[])> GetNullableInterfaceParameters ()
    {
      foreach (var (interfaceMethod, implementations) in _interfaceImplementations.Select (kvp => (kvp.Key, kvp.Value)))
      {
        var nullableImplementationParameters = implementations.Aggregate (
            new HashSet<string>(),
            (set, method) =>
            {
              if (method.ReturnNullableAnnotation == NullableAnnotation.Annotated)
                set.Add ("#return");

              foreach (var nullableParameter in NullableParameters (method.Parameters))
                set.Add (nullableParameter.Name);
              return set;
            });
        yield return (interfaceMethod.DeclaringSyntaxReferences.FirstOrDefault(), nullableImplementationParameters.ToArray());
      }
    }

    public IEnumerable<(SyntaxReference, string[])> GetNullableImplementationParameters ()
    {
      var list = new List<(SyntaxReference, string[])>();
      foreach (var (interfaceMethod, implementations) in _interfaceImplementations.Select (kvp => (kvp.Key, kvp.Value)))
      {
        var returnEnumerable = interfaceMethod.ReturnNullableAnnotation == NullableAnnotation.Annotated
            ? new[] { "#return" }
            : Enumerable.Empty<string>();

        var nullableParameterNames = NullableParameters (interfaceMethod.Parameters)
            .Select (param => param.Name).Concat (returnEnumerable)
            .ToArray();
        var implementationReferences = implementations.Select (impl => impl.DeclaringSyntaxReferences.FirstOrDefault()).ToArray();

        foreach (var implementationReference in implementationReferences)
          list.Add ((implementationReference, nullableParameterNames));
      }

      return list;
    }

    private IEnumerable<IParameterSymbol> NullableParameters (IEnumerable<IParameterSymbol> parameters)
    {
      return parameters.Where (p => p.NullableAnnotation == NullableAnnotation.Annotated);
    }
  }
}