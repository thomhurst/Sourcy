using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Sourcy.DotNet;

[Generator]
internal class DotNetSourceGenerator : BaseSourcyGenerator
{
    private readonly List<FileInfo> _writtenProjects = [];
    private readonly List<FileInfo> _writtenSolutions = [];
    
    protected override void Initialize(SourceProductionContext context, Root root)
    {
        foreach (var file in root.EnumerateFiles())
        {
            if (file.Extension is ".csproj" or ".fsproj")
            {
                WriteProject(context, file);
            }
            
            if (file.Extension is ".sln" or ".slnx" or ".slnf")
            {
                WriteSolution(context, file);
            }
        }
    }

    private void WriteProject(SourceProductionContext context, FileInfo project)
    {
        var formattedName = _writtenProjects.Any(x => x.Name == project.Name) 
            ? project.FullName.Replace('.', '_').Replace(':', '_') 
            : Path.GetFileNameWithoutExtension(project.FullName).Replace('.', '_');
        
        _writtenProjects.Add(project);

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
    
    private void WriteSolution(SourceProductionContext context, FileInfo solution)
    {
        var formattedName = _writtenSolutions.Any(x => x.Name == solution.Name) 
            ? solution.FullName.Replace('.', '_').Replace(':', '_') 
            : Path.GetFileNameWithoutExtension(solution.FullName).Replace('.', '_');
        
        _writtenSolutions.Add(solution);
        
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
}