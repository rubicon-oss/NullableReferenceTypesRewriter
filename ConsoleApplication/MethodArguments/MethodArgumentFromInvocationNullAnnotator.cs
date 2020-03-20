using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullableReferenceTypesRewriter.ConsoleApplication.Utilities;

namespace NullableReferenceTypesRewriter.ConsoleApplication.MehtodArguments
{
  public class MethodArgumentFromInvocationNullAnnotator: SemanticCShapSyntaxRewriter
  {

    private readonly Action<ParameterSyntax> _parameterCallback;

    public MethodArgumentFromInvocationNullAnnotator (SemanticModel semanticModel, Action<ParameterSyntax> parameterCallback)
        : base(semanticModel)
    {
      _parameterCallback = parameterCallback;
    }

    public override SyntaxNode? VisitInvocationExpression (InvocationExpressionSyntax node)
    {
      var nullableArguments = node.ArgumentList.Arguments
          .Select((arg, index) => (arg, index))
          .Where (arg => NullUtilities.IsDefinitlyNull (arg.arg.Expression, Model))
          .ToList();

      if (nullableArguments.Count == 0)
        return node;

      var methodDeclaration = Model.GetSymbolInfo (node);

      var methodSyntax = (MethodDeclarationSyntax?) methodDeclaration.Symbol?.DeclaringSyntaxReferences.FirstOrDefault()
          ?.GetSyntaxAsync()?.GetAwaiter().GetResult();

      if (methodSyntax == null)
        return node;

      foreach (var argument in nullableArguments)
      {
        var nullableParameter = methodSyntax.ParameterList.Parameters[argument.index];
        _parameterCallback (nullableParameter);
      }

      return node;
    }
  }
}