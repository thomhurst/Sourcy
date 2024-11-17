using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Sourcy.Node;

[Generator]
internal class NodeSourceGenerator : BaseSourcyGenerator
{
    protected override void InitializeInternal(SourceProductionContext context, Compilation compilation)
    {
        var root = GetRootDirectory(compilation);

        foreach (var packageJson in root.EnumerateFiles("package.json", SearchOption.AllDirectories)
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