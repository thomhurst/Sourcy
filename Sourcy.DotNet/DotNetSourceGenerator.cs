using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Sourcy.DotNet;

[Generator]
internal class DotNetSourceGenerator : BaseSourcyGenerator
{
    public override void Initialize(IncrementalGeneratorInitializationContext context)
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
}