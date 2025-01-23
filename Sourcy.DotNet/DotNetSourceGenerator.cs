using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Sourcy.DotNet;

[Generator]
internal class DotNetSourceGenerator : BaseSourcyGenerator
{    
    protected override void Initialize(SourceProductionContext context, Root root)
    {
        foreach (var file in root.EnumerateFiles())
        {
            if (file.Extension is ".csproj" or ".fsproj")
            {
                WriteProject(context, root, file);
            }
            
            if (file.Extension is ".sln" or ".slnx" or ".slnf")
            {
                WriteSolution(context, root, file);
            }
        }
    }

    private void WriteProject(SourceProductionContext context, Root root, FileInfo project)
    {
        var formattedName = root.MakeRelativePath(project.FullName).Replace('.', '_').Replace(Path.PathSeparator, '_');
        
        context.AddSource($"DotNetProjectExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(
            $$"""
              namespace Sourcy.DotNet;

              internal static partial class Projects
              {
                  public static global::System.IO.FileInfo {{formattedName}} { get; } = new global::System.IO.FileInfo(@"{{project.FullName}}");
              }
              """
        ));
    }
    
    private void WriteSolution(SourceProductionContext context, Root root, FileInfo solution)
    {
        var formattedName = root.MakeRelativePath(solution.FullName).Replace('.', '_').Replace(Path.PathSeparator, '_');
        
        context.AddSource($"DotNetSolutionExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(
            $$"""
              namespace Sourcy.DotNet;

              internal static partial class Solutions
              {
                  public static global::System.IO.FileInfo {{formattedName}} { get; } = new global::System.IO.FileInfo(@"{{solution.FullName}}");
              }
              """
        ));
    }

    private static string MakeRelative(FileInfo solution)
    {
        return solution.FullName;
    }
}