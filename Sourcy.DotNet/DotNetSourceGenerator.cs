using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Sourcy.DotNet;

[Generator]
internal class DotNetSourceGenerator : BaseSourcyGenerator
{
    protected override void InitializeInternal(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (productionContext, compilation) =>
        {
            Execute(productionContext, compilation);
        });
    }

    private void Execute(SourceProductionContext productionContext, Compilation compilation)
    {
        var root = GetRootDirectory(compilation);

        if (IsDebug)
        {
            productionContext.AddSource($"Sourcy-DotNet-RootDirectory-{Guid.NewGuid():N}.txt", "// " + root.FullName);
        }

        foreach (var project in root.EnumerateFiles("**.*sproj", SearchOption.AllDirectories))
        {
            if (IsDebug)
            {
                productionContext.AddSource($"Sourcy-DotNet-Project-{Guid.NewGuid():N}.txt", "// " + project.FullName);
            }

            WriteProject(productionContext, project);
        }
        
        foreach (var solution in root.EnumerateFiles("**.sln", SearchOption.AllDirectories))
        {
            if (IsDebug)
            {
                productionContext.AddSource($"Sourcy-DotNet-Solution-{Guid.NewGuid():N}.txt", "// " + solution.FullName);
            }
            
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