#pragma warning disable RS1035

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sourcy.DotNet;

[Generator]
internal class DotNetSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (productionContext, compilation) =>
        {
            Execute(productionContext, compilation);
        });
    }

    private static void Execute(SourceProductionContext productionContext, Compilation compilation)
    {
        var root = GetRootDirectory(compilation);

        foreach (var project in root.EnumerateFiles("**.*sproj", SearchOption.AllDirectories))
        {
            WriteProject(productionContext, project);
        }
        
        foreach (var solution in root.EnumerateFiles("**.sln", SearchOption.AllDirectories))
        {
            WriteSolution(productionContext, solution);
        }
    }

    private static void WriteProject(SourceProductionContext productionContext, FileInfo project)
    {
        var formattedName = Path.GetFileNameWithoutExtension(project.FullName).Replace('.', '_');
        
        productionContext.AddSource($"DotNetProjectExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(
            $$"""
              namespace Sourcy.DotNet;

              internal static partial class Projects
              {
                  public static global::System.IO.FileInfo {{formattedName}} { get; } = new global::System.IO.FileInfo(@"{{project.FullName}}");
              }
              """
        ));
    }
    
    private static void WriteSolution(SourceProductionContext productionContext, FileInfo solution)
    {
        var formattedName = Path.GetFileNameWithoutExtension(solution.FullName).Replace('.', '_');
        
        productionContext.AddSource($"DotNetSolutionExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(
            $$"""
              namespace Sourcy.DotNet;

              internal static partial class Solutions
              {
                  public static global::System.IO.FileInfo {{formattedName}} { get; } = new global::System.IO.FileInfo(@"{{solution.FullName}}");
              }
              """
        ));
    }

    private static DirectoryInfo GetRootDirectory(Compilation compilation)
    {
        var location = GetLocation(compilation);

        while (true)
        {
            if (Directory.Exists(Path.Combine(location.FullName, ".git")))
            {
                return location;
            }
            
            var parent = location.Parent;

            if (parent is null || parent == location || parent == location.Root)
            {
                return location;
            }

            location = parent;
        }
    }

    private static DirectoryInfo GetLocation(Compilation compilation)
    {
        var assemblyLocations = compilation.Assembly.Locations;

        var fileLocation = assemblyLocations
                               .FirstOrDefault(x => x.Kind is LocationKind.MetadataFile)
                           ?? assemblyLocations.First();

        return Directory.GetParent(fileLocation.GetLineSpan().Path)!;
    }

    private static SourceText GetSourceText([StringSyntax("c#")] string code)
    {
        return SourceText.From(code, Encoding.UTF8);
    }
}