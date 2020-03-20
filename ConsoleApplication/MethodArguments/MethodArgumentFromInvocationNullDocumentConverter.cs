using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullableReferenceTypesRewriter.ConsoleApplication.MehtodArguments
{
  public class MethodArgumentFromInvocationNullDocumentConverter : IDocumentConverter
  {
    public async Task<Document> Convert (Document doc)
    {
      var semantic = await doc.GetSemanticModelAsync();
      var syntax = await doc.GetSyntaxRootAsync();

      var argList = new HashSet<ParameterSyntax>();

      var _ = new MethodArgumentFromInvocationNullAnnotator (semantic!, (arg) => argList.Add(arg)).Visit (syntax!);
      var newSyntax = new MethodParameterNullAnnotator (argList).Visit (syntax);

      return doc.WithSyntaxRoot(newSyntax);
    }
  }
}