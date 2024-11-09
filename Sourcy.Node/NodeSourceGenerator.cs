#pragma warning disable RS1035

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sourcy.Node;

[Generator]
internal class NodeSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext productionContext, Compilation compilation)
    {
        var root = GetRootDirectory(compilation);

        foreach (var packageJson in root.EnumerateFiles("package.json", SearchOption.AllDirectories)
                     .Where(x => !IsInNodeModules(x)))
        {
            WriteProject(productionContext, packageJson.Directory!);
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

    private static void WriteProject(SourceProductionContext productionContext, DirectoryInfo projectDirectory)
    {
        var formattedName = projectDirectory.Name.Replace('.', '_');
        
        productionContext.AddSource($"NodeProjectExtensions{Guid.NewGuid():N}.g.cs", GetSourceText(
            $$"""
              namespace Sourcy.Node;

              internal static partial class Projects
              {
                  public static global::System.IO.DirectoryInfo {{formattedName}} { get; } = new global::System.IO.DirectoryInfo(@"{{projectDirectory.FullName}}");
              }
              """
        ));
    }

    private static DirectoryInfo GetRootDirectory(Compilation compilation)
    {
        var location = GetLocation(compilation);

        while (true)
        {
            if (Directory.Exists(Path.Combine(location.FullName, ".git")))
            {
                return location;
            }
            
            var parent = location.Parent;

            if (parent is null || parent == location || parent == location.Root)
            {
                return location;
            }

            location = parent;
        }
    }

    private static DirectoryInfo GetLocation(Compilation compilation)
    {
        var assemblyLocations = compilation.Assembly.Locations;

        var fileLocation = assemblyLocations
                               .FirstOrDefault(x => x.Kind is LocationKind.MetadataFile)
                           ?? assemblyLocations.First();

        return Directory.GetParent(fileLocation.GetLineSpan().Path)!;
    }

    private static SourceText GetSourceText([StringSyntax("c#")] string code)
    {
        return SourceText.From(code, Encoding.UTF8);
    }
}