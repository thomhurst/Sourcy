using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Sourcy.Docker;

[Generator]
internal class DockerSourceGenerator : BaseSourcyGenerator
{
    protected override void Initialize(SourceProductionContext context, Root root)
    {
        foreach (var project in root.EnumerateFiles()
                     .Where(x => x.Name is "Dockerfile"))
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