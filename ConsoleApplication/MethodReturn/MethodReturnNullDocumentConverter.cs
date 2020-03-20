using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace NullableReferenceTypesRewriter.ConsoleApplication
{
  public class MethodNullDocumentConverter: IDocumentConverter
  {
    public async Task<Document> Convert (Document doc)
    {
      var semantic = await doc.GetSemanticModelAsync();
      var syntax = await doc.GetSyntaxRootAsync();

      var newSyntax =  new MethodNullAnnotator(semantic!).Visit (syntax!);

      return doc.WithSyntaxRoot (newSyntax);
    }
  }
}