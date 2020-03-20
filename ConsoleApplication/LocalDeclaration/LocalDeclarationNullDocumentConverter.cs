using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace NullableReferenceTypesRewriter.ConsoleApplication
{
  public class LocalDeclarationNullDocumentConverter : IDocumentConverter
  {
    public async Task<Document> Convert (Document doc)
    {
      var semantic = await doc.GetSemanticModelAsync();
      var syntax = await doc.GetSyntaxRootAsync();

      var newSyntax = new LocalDeclarationNullAnnotator (semantic!).Visit (syntax!);

      return doc.WithSyntaxRoot (newSyntax);
    }
  }
}