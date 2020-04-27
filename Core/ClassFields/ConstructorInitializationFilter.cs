using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypesRewriter.ClassFields
{
  public class ConstructorInitializationFilter: CSharpSyntaxRewriter
  {
    private readonly ClassDeclarationSyntax _classDeclaration;
    private readonly IReadOnlyCollection<VariableDeclarationSyntax> _variables;
    private readonly ISet<VariableDeclarationSyntax> _uninitializedVariables = new HashSet<VariableDeclarationSyntax>();

    public ConstructorInitializationFilter (ClassDeclarationSyntax classDeclaration, IReadOnlyCollection<VariableDeclarationSyntax> variables)
    {
      _classDeclaration = classDeclaration;
      _variables = variables;
    }

    public override SyntaxNode? VisitConstructorDeclaration (ConstructorDeclarationSyntax node)
    {
      if (node.Initializer != null)
        return node;

      var assignments = node.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>().ToArray();

      foreach (var variable in _variables)
      {
        if(!IsParameterAssinged (assignments, variable))
        {
          _uninitializedVariables.Add (variable);
        }
      }

      return node;
    }

    public IReadOnlyCollection<VariableDeclarationSyntax> GetUnitializedFields ()
    {
      Visit (_classDeclaration);
      return _uninitializedVariables.ToArray();
    }

    private bool IsParameterAssinged (IEnumerable<AssignmentExpressionSyntax> assignments, VariableDeclarationSyntax variable)
    {
      return assignments.FirstOrDefault (assingment => assingment.Left.ToString() == variable.Variables.First().Identifier.ValueText) != null;
    }

  }
}