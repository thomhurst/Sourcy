using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Sourcy.Extensions;

namespace Sourcy.DotNet;

[Generator]
internal class DotNetSourceGenerator : BaseSourcyGenerator
{    
    protected override void Initialize(SourceProductionContext context, Root root)
    {
        var projects = new List<FileInfo>();
        var solutions = new List<FileInfo>();
        
        foreach (var file in root.EnumerateFiles())
        {
            if (file.Extension is ".csproj" or ".fsproj" or ".vbproj")
            {
                projects.Add(file);
            }
            
            if (file.Extension is ".sln" or ".slnx" or ".slnf")
            {
                solutions.Add(file);
            }
        }
        
        WriteProjects(context, Distinct(root, projects));

        WriteSolutions(context, Distinct(root, solutions));
    }

    private void WriteProjects(SourceProductionContext context, IEnumerable<SourceGeneratedPath> projects)
    {
        var sourceBuilder = new StringBuilder();
        
        sourceBuilder.AppendLine("namespace Sourcy.DotNet;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("internal static class Projects");
        sourceBuilder.AppendLine("{");
        
        foreach (var project in projects)
        {
            sourceBuilder.AppendLine($"\tpublic static global::System.IO.FileInfo {project.Name} {{ get; }} = new global::System.IO.FileInfo(@\"{project.File.FullName}\");");
        }
        
        sourceBuilder.AppendLine("}");
        
        context.AddSource($"DotNetProjectExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(sourceBuilder.ToString()));
    }

    private void WriteSolutions(SourceProductionContext context, IEnumerable<SourceGeneratedPath> solutions)
    {
        var sourceBuilder = new StringBuilder();
        
        sourceBuilder.AppendLine("namespace Sourcy.DotNet;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("internal static class Solutions");
        sourceBuilder.AppendLine("{");
        
        foreach (var solution in solutions)
        {
            sourceBuilder.AppendLine($"\tpublic static global::System.IO.FileInfo {solution.Name} {{ get; }} = new global::System.IO.FileInfo(@\"{solution.File.FullName}\");");
        }
        
        sourceBuilder.AppendLine("}");
        
        context.AddSource($"DotNetSolutionExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(sourceBuilder.ToString()));
    }
}

