using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.ConsoleApplication.Utilities;

namespace NullableReferenceTypesRewriter.ConsoleApplication.MehtodArguments
{
  public class MethodParameterNullAnnotator : CSharpSyntaxRewriter
  {
    public MethodParameterNullAnnotator (IReadOnlyCollection<ParameterSyntax> nullableParameters)
    {
      _nullableParameters = nullableParameters;
    }

    private readonly IReadOnlyCollection<ParameterSyntax> _nullableParameters;

    public override SyntaxNode? VisitMethodDeclaration (MethodDeclarationSyntax node)
    {
      var newParameters = node.ParameterList.Parameters;

      foreach (var parameter in node.ParameterList.Parameters)
      {
        if (_nullableParameters.Contains (parameter))
        {
          if (parameter.Type == null)
            continue;

          var toReplace = newParameters.SingleOrDefault (param => param.Identifier.ToString() == parameter.Identifier.ToString());
          if(toReplace.Type != null)
            newParameters = newParameters.Replace (toReplace, toReplace.WithType (NullUtilities.MakeNullable (toReplace.Type)));
        }
      }

      return node.WithParameterList(node.ParameterList.WithParameters(newParameters));
    }
  }
}