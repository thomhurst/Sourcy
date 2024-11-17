using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Sourcy.Docker;

[Generator]
internal class DockerSourceGenerator : BaseSourcyGenerator
{
    protected override void InitializeInternal(SourceProductionContext context, Compilation compilation)
    {
        var root = GetRootDirectory(compilation);

        foreach (var project in root.EnumerateFiles("Dockerfile", SearchOption.AllDirectories))
        {
            WriteDockerfile(context, project);
        }
    }

    private static void WriteDockerfile(SourceProductionContext context, FileInfo project)
    {
        var formattedName = project.Directory!.Name.Replace('.', '_');
        
        context.AddSource($"DockerFileExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(
            $$"""
              namespace Sourcy.Docker;

              internal static partial class Dockerfiles
              {
                  public static global::System.IO.FileInfo {{formattedName}} { get; } = new global::System.IO.FileInfo(@"{{project.FullName}}");
              }
              """
        ));
    }
}