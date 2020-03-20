using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace NullableReferenceTypesRewriter.ConsoleApplication
{
  public class CastExpressionNullDocumentConverter: IDocumentConverter
  {
    public async Task<Document> Convert (Document doc)
    {
      var semantic = await doc.GetSemanticModelAsync();
      var syntax = await doc.GetSyntaxRootAsync();

      var newSyntax =  new CastExpressionNullAnnotator(semantic!).Visit (syntax!);

      return doc.WithSyntaxRoot (newSyntax);
    }
  }
}