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
                     .Where(x => x.Name is "package-lock.json")
                     .Where(x => !IsInNodeModules(x))
                     .Distinct())
        {
            WriteNpmProject(context, packageJson.Directory!);
        }
        
        foreach (var yarnLock in root.EnumerateFiles()
                     .Where(x => x.Name is "yarn.lock")
                     .Where(x => !IsInNodeModules(x))
                     .Distinct())
        {
            WriteYarnProject(context, yarnLock.Directory!);
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

    private static void WriteNpmProject(SourceProductionContext context, DirectoryInfo projectDirectory)
    {
        var formattedName = projectDirectory.Name.Replace('.', '_');
        
        context.AddSource($"NpmProjectExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(
            $$"""
              namespace Sourcy.Node.Npm;

              internal static partial class Projects
              {
                  public static global::System.IO.DirectoryInfo {{formattedName}} { get; } = new global::System.IO.DirectoryInfo(@"{{projectDirectory.FullName}}");
              }
              """
        ));
    }
    
    private static void WriteYarnProject(SourceProductionContext context, DirectoryInfo projectDirectory)
    {
        var formattedName = projectDirectory.Name.Replace('.', '_');
        
        context.AddSource($"YarnProjectExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(
            $$"""
              namespace Sourcy.Node.Yarn;

              internal static partial class Projects
              {
                  public static global::System.IO.DirectoryInfo {{formattedName}} { get; } = new global::System.IO.DirectoryInfo(@"{{projectDirectory.FullName}}");
              }
              """
        ));
    }
}