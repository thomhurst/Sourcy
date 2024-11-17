#pragma warning disable RS1035

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sourcy;

public abstract class BaseSourcyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var valuesProvider = context.CompilationProvider
            .Select(static (compilation, _) => new CompilationWrapper(compilation));
        
        context.RegisterSourceOutput(valuesProvider, (sourceProductionContext, compilation) =>
        {
            InitializeInternal(sourceProductionContext, compilation.Compilation);
        });
    }

    protected abstract void InitializeInternal(SourceProductionContext context, Compilation compilation);

    protected static DirectoryInfo GetRootDirectory(Compilation compilation)
    {
        var location = GetLocation(compilation);

        while (true)
        {
            if (Directory.Exists(Path.Combine(location.FullName, ".git")))
            {
                return location;
            }
            
            if (File.Exists(Path.Combine(location.FullName, ".sourcyroot")))
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

    protected static DirectoryInfo GetLocation(Compilation compilation)
    {
        var assemblyLocations = compilation.Assembly.Locations;

        var fileLocation = assemblyLocations
                               .FirstOrDefault(x => x.Kind is LocationKind.MetadataFile)
                           ?? assemblyLocations.First();

        return Directory.GetParent(fileLocation.GetLineSpan().Path)!;
    }

    protected static SourceText GetSourceText([StringSyntax("c#")] string code)
    {
        return SourceText.From(code, Encoding.UTF8);
    }
}