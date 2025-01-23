#pragma warning disable RS1035

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Sourcy.Extensions;

namespace Sourcy;

public abstract class BaseSourcyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {               
        var sourcyDirectoryValuesProvider = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) => provider.GlobalOptions.TryGetValue("build_property.projectdir", out var directory)
                ? directory
                : null);

        context.RegisterSourceOutput(sourcyDirectoryValuesProvider, (productionContext, sourcyDirectory) =>
        {
            if (sourcyDirectory is null || GetRootDirectory(sourcyDirectory) is not {} root)
            {
                Debug.WriteLine("No Sourcy Directory found.");
                return;
            }
            
            Initialize(productionContext, root);
        });
    }

    protected abstract void Initialize(SourceProductionContext context, Root root);

    protected static Root? GetRootDirectory(string? path)
    {
        if (path is null)
        {
            return null;
        }
        
        var location = new DirectoryInfo(path);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        while (location is not null)
        {
            if (Directory.Exists(Path.Combine(location.FullName, ".git")))
            {
                break;
            }

            if (File.Exists(Path.Combine(location.FullName, ".sourcyroot")))
            {
                break;
            }

            location = location.Parent;
        }

        if (location is null)
        {
            return null;
        }

        return new Root(location);
    }

    protected static Root GetRoot(Compilation compilation)
    {
        var assemblyLocations = compilation.Assembly.Locations;

        return assemblyLocations
                   .Where(x => x.Kind == LocationKind.MetadataFile)
                   .Select(x => GetRootDirectory(Directory.GetParent(x.GetLineSpan().Path)!.FullName))
                   .OfType<Root>()
                   .FirstOrDefault()
               ?? assemblyLocations
                   .Select(x => GetRootDirectory(Directory.GetParent(x.GetLineSpan().Path)!.FullName))
                   .OfType<Root>()
                   .First();
    }

    protected static SourceText GetSourceText([StringSyntax("c#")] string code)
    {
        return SourceText.From(code, Encoding.UTF8);
    }
    
    protected IEnumerable<SourceGeneratedPath> Distinct(Root root, List<FileInfo> files)
    {
        foreach (var group in files.GroupBy(x => x.NameWithoutExtension()))
        {
            if (group.Count() > 1)
            {
                foreach (var file in group)
                {
                    FileSystemInfo fileSystemInfo = file;
        
                    if (file.NameWithoutExtension() == file.Directory!.Name)
                    {
                        fileSystemInfo = file.Directory;
                    }
        
                    var formattedName = root.MakeRelativePath(fileSystemInfo.FullName)
                        .Replace(file.Extension, string.Empty)
                        .Replace('.', '_')
                        .Replace(@"\", "__")
                        .Replace("/", "__")
                        .Trim('_');

                    yield return new SourceGeneratedPath
                    {
                        File = file,
                        Name = formattedName
                    };
                }
            }
            else
            {
                yield return new SourceGeneratedPath
                {
                    File = group.First(),
                    Name = group.Key.Replace('.', '_')
                };
            }
        }
    }
}