using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace NullableTypeSetter.ConsoleApplication
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

      foreach (var document in project.Documents)
      {
        var newDocument = await Convert (document);
        await WriteChanges(document, newDocument);
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

    private static async Task<Document> Convert (Document document)
    {
      var semanticModel = await document.GetSemanticModelAsync()
                          ?? throw new Exception();
      var syntaxRoot = await document.GetSyntaxRootAsync()
                       ?? throw new Exception();

      var rewritten = new MethodNullAnnotator(semanticModel).Visit(syntaxRoot);

      return document.WithSyntaxRoot (rewritten);
    }

    private static Project LoadProject (Solution solution, string projectName)
    {
      var project = solution.Projects.Single(p => p.Name == projectName);
      var compilationOptions = project.CompilationOptions as CSharpCompilationOptions;

      compilationOptions = compilationOptions?.WithNullableContextOptions (NullableContextOptions.Enable);
      if (compilationOptions != null)
        project = project.WithCompilationOptions (compilationOptions);

      return project;
    }

    public static Task<Solution> LoadSolutionSpace (string solutionPath)
    {
      var msBuild = MSBuildLocator.QueryVisualStudioInstances().First();
      MSBuildLocator.RegisterInstance (msBuild);
      using var workspace = MSBuildWorkspace.Create();
      workspace.WorkspaceFailed += (sender, args) => { Console.WriteLine (args.Diagnostic); };

      return workspace.OpenSolutionAsync (@"C:\Development\Remotion-git\Remotion.sln");
    }
  }
}