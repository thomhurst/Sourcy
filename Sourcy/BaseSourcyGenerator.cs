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
        var incrementalValuesProvider = context.SyntaxProvider.CreateSyntaxProvider((_, _) => true,
            (productionContext, _) => productionContext.SemanticModel.Compilation)
            .WithComparer(new CompilationComparer());
        
        context.RegisterSourceOutput(incrementalValuesProvider, InitializeInternal);
    }

    protected abstract void InitializeInternal(SourceProductionContext context, Compilation compilation);

    protected static Root GetRootDirectory(Compilation compilation)
    {
        var location = GetLocation(compilation);

        while (true)
        {
            if (Directory.Exists(Path.Combine(location.FullName, ".git")))
            {
                return new Root(location);
            }
            
            if (File.Exists(Path.Combine(location.FullName, ".sourcyroot")))
            {
                return new Root(location);
            }
            
            var parent = location.Parent;

            if (parent is null || parent == location || parent == location.Root)
            {
                return new Root(location);
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