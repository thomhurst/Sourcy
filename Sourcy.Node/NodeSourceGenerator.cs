using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Sourcy.Node;

[Generator]
internal class NodeSourceGenerator : BaseSourcyGenerator
{
    protected override void Initialize(SourceProductionContext context, Root root)
    {
        foreach (var packageJson in root.EnumerateFiles()
                     .Where(x => x.Name is "package.json")
                     .Where(x => !IsInNodeModules(x)))
        {
            WriteProject(context, packageJson.Directory!);
        }
    }

    private static bool IsInNodeModules(FileInfo fileInfo)
    {
        var parent = fileInfo.Directory;

        while (parent is not null)
        {
            if (parent.Name == "node_modules")
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }

    private static void WriteProject(SourceProductionContext context, DirectoryInfo projectDirectory)
    {
        var formattedName = projectDirectory.Name.Replace('.', '_');
        
        context.AddSource($"NodeProjectExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(
            $$"""
              namespace Sourcy.Node;

              internal static partial class Projects
              {
                  public static global::System.IO.DirectoryInfo {{formattedName}} { get; } = new global::System.IO.DirectoryInfo(@"{{projectDirectory.FullName}}");
              }
              """
        ));
    }
}