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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using NullableReferenceTypesRewriter.CastExpression;
using NullableReferenceTypesRewriter.ClassFields;
using NullableReferenceTypesRewriter.Inheritance;
using NullableReferenceTypesRewriter.LocalDeclaration;
using NullableReferenceTypesRewriter.MethodArguments;
using NullableReferenceTypesRewriter.MethodReturn;
using NullableReferenceTypesRewriter.Utilities;

namespace NullableReferenceTypesRewriter.ConsoleApplication
{
  internal class Program
  {
    public static async Task Main (string[] args)
    {
      if (args.Length != 2)
      {
        Console.WriteLine ("Please supply a solution directory and a project name to convert.");
        Console.WriteLine ("eg.:     nrtRewriter \"C:\\Develop\\MyCode\\MyCode.sln\" Core.Utils");
      }

      var solutionPath = args[0];
      var projectName = args[1];

      var solution = await LoadSolutionSpace (solutionPath);
      var project = LoadProject (solution, projectName);

      IEnumerable<(Document, Document)> converted = await ApplyConverter (
          project.Documents,
          new IDocumentConverter[]
          {
              new MethodReturnNullDocumentConverter(),
              new LocalDeclarationNullDocumentConverter(),
              new CastExpressionNullDocumentConverter(),
              new MethodArgumentFromInvocationNullDocumentConverter(),
              new ClassFieldNotInitializedDocumentConverter()
          });

       converted = await new InheritanceProjectConverter().Convert (converted);

      foreach (var (oldDocument, document) in converted)
      {
        await WriteChanges (oldDocument, document);
      }
    }

    private static async Task WriteChanges (Document oldDocument, Document newDocument)
    {
      try
      {
        var newRootNode = await newDocument.GetSyntaxRootAsync();
        var oldRootNode = await oldDocument.GetSyntaxRootAsync();

        if (oldRootNode == newRootNode) return;

        using var fileStream = new FileStream (newDocument.FilePath!, FileMode.Truncate);
        using var writer = new StreamWriter (fileStream, Encoding.Default);
        newRootNode!.WriteTo (writer);
      }
      catch (IOException ex)
      {
        throw new InvalidOperationException ($"Unable to write source file '{newDocument.FilePath}'.", ex);
      }
    }

    private static Project LoadProject (Solution solution, string projectName)
    {
      var project = solution.Projects.Single (p => p.Name == projectName);
      var compilationOptions = project.CompilationOptions as CSharpCompilationOptions;

      compilationOptions = compilationOptions?.WithNullableContextOptions (NullableContextOptions.Enable);
      if (compilationOptions != null)
        project = project.WithCompilationOptions (compilationOptions);

      return project;
    }

    private static Task<(Document, Document)[]> ApplyConverter (IEnumerable<Document> documents, IEnumerable<IDocumentConverter> converters)
    {
      var tasks = documents.Select (async doc => (doc, await ConverterUtilities.ApplyAll (doc, converters)));
      return Task.WhenAll(tasks);
    }

    private static Task<Solution> LoadSolutionSpace (string solutionPath)
    {
      var msBuild = MSBuildLocator.QueryVisualStudioInstances().First();
      MSBuildLocator.RegisterInstance (msBuild);
      using var workspace = MSBuildWorkspace.Create();
      workspace.WorkspaceFailed += (sender, args) => { Console.WriteLine (args.Diagnostic); };

      return workspace.OpenSolutionAsync (solutionPath);
    }
  }
}