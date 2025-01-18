#pragma warning disable RS1035

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sourcy;

public abstract class BaseSourcyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var rootDirectoryValuesProvider = context.CompilationProvider
        .Select((compilation, _) => GetLocation(compilation))
        .Select((directory, _) => GetRootDirectory(directory));

        context.RegisterSourceOutput(rootDirectoryValuesProvider, Initialize);
    }

    protected abstract void Initialize(SourceProductionContext context, Root root);

    protected static Root GetRootDirectory(string path)
    {
        var location = GetLocation(path);

        while (true)
        {
            if (location == location.Root)
            {
                break;
            }

            if (Directory.Exists(Path.Combine(location.FullName, ".git")))
            {
                break;
            }

            if (File.Exists(Path.Combine(location.FullName, ".sourcyroot")))
            {
                break;
            }

            var parent = location.Parent;

            if (parent is null || parent == location.Root)
            {
                break;
            }

            location = parent;
        }

        return new Root(location);
    }

    protected static DirectoryInfo GetLocation(string path)
    {
        return new FileInfo(path).Directory!;
    }

    protected static string GetLocation(Compilation compilation)
    {
        var assemblyLocations = compilation.Assembly.Locations;

        var fileLocation = assemblyLocations
                               .FirstOrDefault(x => x.Kind is LocationKind.MetadataFile)
                           ?? assemblyLocations.First();

        return Directory.GetParent(fileLocation.GetLineSpan().Path)!.FullName!;
    }

    protected static SourceText GetSourceText([StringSyntax("c#")] string code)
    {
        return SourceText.From(code, Encoding.UTF8);
    }
}