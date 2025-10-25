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

    protected static Root? GetRoot(Compilation compilation)
    {
        var assemblyLocations = compilation.Assembly.Locations;

        // Try metadata files first
        var metadataRoot = assemblyLocations
            .Where(x => x.Kind == LocationKind.MetadataFile)
            .Select(x =>
            {
                var path = x.GetLineSpan().Path;
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                var parent = Directory.GetParent(path);
                return parent != null ? GetRootDirectory(parent.FullName) : null;
            })
            .OfType<Root>()
            .FirstOrDefault();

        if (metadataRoot != null)
        {
            return metadataRoot;
        }

        // Try all locations
        var anyRoot = assemblyLocations
            .Select(x =>
            {
                var path = x.GetLineSpan().Path;
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                var parent = Directory.GetParent(path);
                return parent != null ? GetRootDirectory(parent.FullName) : null;
            })
            .OfType<Root>()
            .FirstOrDefault();

        return anyRoot;
    }

    protected static SourceText GetSourceText([StringSyntax("c#")] string code)
    {
        return SourceText.From(code, Encoding.UTF8);
    }
    
    protected IEnumerable<SourceGeneratedPath> Distinct(Root root, List<FileInfo> files)
    {
        var usedIdentifiers = new HashSet<string>();

        foreach (var group in files.GroupBy(x => x.NameWithoutExtension()))
        {
            if (group.Count() > 1)
            {
                // Multiple files with same name - use path-based naming
                foreach (var file in group)
                {
                    FileSystemInfo fileSystemInfo = file;

                    if (file.NameWithoutExtension() == file.Directory!.Name)
                    {
                        fileSystemInfo = file.Directory;
                    }

                    var relativePath = root.MakeRelativePath(fileSystemInfo.FullName);
                    var formattedName = IdentifierHelper.SanitizePathToIdentifier(relativePath, file.Extension);

                    // Ensure uniqueness
                    formattedName = IdentifierHelper.ToValidIdentifier(formattedName, usedIdentifiers);

                    yield return new SourceGeneratedPath
                    {
                        File = file,
                        Name = formattedName
                    };
                }
            }
            else
            {
                // Single file with this name - use simple name
                var file = group.First();
                var simpleName = IdentifierHelper.ToValidIdentifier(group.Key, usedIdentifiers);

                yield return new SourceGeneratedPath
                {
                    File = file,
                    Name = simpleName
                };
            }
        }
    }
}