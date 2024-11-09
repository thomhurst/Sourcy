using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Sourcy.Docker;

[Generator]
internal class DockerSourceGenerator : BaseSourcyGenerator
{
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext productionContext, Compilation compilation)
    {
        var root = GetRootDirectory(compilation);

        foreach (var project in root.EnumerateFiles("Dockerfile", SearchOption.AllDirectories))
        {
            WriteDockerfile(productionContext, project);
        }
    }

    private static void WriteDockerfile(SourceProductionContext productionContext, FileInfo project)
    {
        var formattedName = project.Directory!.Name.Replace('.', '_');
        
        productionContext.AddSource($"DockerFileExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(
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