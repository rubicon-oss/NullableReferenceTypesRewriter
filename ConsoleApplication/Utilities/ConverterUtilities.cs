using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace NullableReferenceTypesRewriter.ConsoleApplication
{
  public class ConverterUtilities
  {

    public static async Task<Document> ApplyAll (Document doc, IEnumerable<IDocumentConverter> converters)
    {
      var document = doc;
      foreach (var converter in converters)
      {
        document = await converter.Convert (document);
      }

      return document;
    }
  }
}