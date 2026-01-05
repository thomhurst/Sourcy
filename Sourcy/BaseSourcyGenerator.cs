#pragma warning disable RS1035

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Sourcy.Extensions;

namespace Sourcy;

public abstract class BaseSourcyGenerator : IIncrementalGenerator
{
    // Maximum number of parent directories to traverse when looking for root
    private const int MaxRootSearchDepth = 30;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sourcyOptionsProvider = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) =>
            {
                provider.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir);
                provider.GlobalOptions.TryGetValue("build_property.SourcyRootPath", out var customRoot);
                return (ProjectDir: projectDir, CustomRoot: customRoot);
            });

        context.RegisterSourceOutput(sourcyOptionsProvider, (productionContext, options) =>
        {
            Root? root = null;

            // First, try custom root path if specified
            if (!string.IsNullOrWhiteSpace(options.CustomRoot))
            {
                root = TryGetCustomRoot(productionContext, options.CustomRoot!);
            }

            // Fall back to auto-detection if custom root wasn't specified or was invalid
            if (root is null)
            {
                if (options.ProjectDir is null)
                {
                    Debug.WriteLine("No Sourcy Directory found.");
                    return;
                }

                root = GetRootDirectory(options.ProjectDir);
            }

            if (root is null)
            {
                Debug.WriteLine("No Sourcy Directory found.");
                productionContext.ReportRootNotFound();
                return;
            }

            // Check for UNC/network paths and warn
            CheckForUncPath(productionContext, root.Directory.FullName);

            Initialize(productionContext, root);
        });
    }

    /// <summary>
    /// Attempts to use a custom root path specified via SourcyRootPath MSBuild property.
    /// </summary>
    private static Root? TryGetCustomRoot(SourceProductionContext context, string customPath)
    {
        try
        {
            var trimmedPath = customPath.Trim();
            if (string.IsNullOrEmpty(trimmedPath))
            {
                return null;
            }

            var directory = new DirectoryInfo(trimmedPath);
            if (!directory.Exists)
            {
                context.ReportInvalidCustomRoot(trimmedPath);
                return null;
            }

            context.ReportCustomRootUsed(trimmedPath);
            return new Root(directory);
        }
        catch (Exception)
        {
            context.ReportInvalidCustomRoot(customPath);
            return null;
        }
    }

    /// <summary>
    /// Checks if the root path is a UNC/network path and emits a warning.
    /// </summary>
    private static void CheckForUncPath(SourceProductionContext context, string rootPath)
    {
        try
        {
            // Check for UNC paths (\\server\share or //server/share)
            if (rootPath.StartsWith(@"\\", StringComparison.Ordinal) ||
                rootPath.StartsWith("//", StringComparison.Ordinal))
            {
                context.ReportUncPath(rootPath);
                return;
            }

            // On Windows, check if the drive is a network drive
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && rootPath.Length >= 2 && rootPath[1] == ':')
            {
                var driveLetter = rootPath[0];
                var driveRoot = $"{driveLetter}:\\";

                try
                {
                    var driveInfo = new DriveInfo(driveRoot);
                    if (driveInfo.DriveType == DriveType.Network)
                    {
                        context.ReportUncPath($"Network drive {driveRoot}");
                    }
                }
                catch
                {
                    // DriveInfo can throw for certain paths - ignore
                }
            }
        }
        catch
        {
            // Ignore errors in UNC detection - it's just a helpful warning
        }
    }

    protected abstract void Initialize(SourceProductionContext context, Root root);

    protected static Root? GetRootDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        DirectoryInfo? location;
        try
        {
            location = new DirectoryInfo(path);
            if (!location.Exists)
            {
                return null;
            }
        }
        catch (Exception)
        {
            // Invalid path characters or other issues
            return null;
        }

        var depth = 0;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        while (location is not null && depth < MaxRootSearchDepth)
        {
            try
            {
                var gitPath = Path.Combine(location.FullName, ".git");
                var sourcyRootPath = Path.Combine(location.FullName, ".sourcyroot");

                // Check for .git as directory (normal repo) OR file (git worktree)
                // In worktrees, .git is a file containing: "gitdir: /path/to/actual/git/dir"
                if (DirectoryExistsSafe(gitPath) || IsGitWorktreeFile(gitPath))
                {
                    break;
                }

                if (FileExistsSafe(sourcyRootPath))
                {
                    break;
                }
            }
            catch
            {
                // Path access error - try parent
            }

            location = location.Parent;
            depth++;
        }

        if (location is null || depth >= MaxRootSearchDepth)
        {
            return null;
        }

        return new Root(location);
    }

    /// <summary>
    /// Safe directory existence check that handles exceptions.
    /// </summary>
    private static bool DirectoryExistsSafe(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safe file existence check that handles exceptions.
    /// </summary>
    private static bool FileExistsSafe(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the path is a git worktree file.
    /// In git worktrees, .git is a file (not directory) containing "gitdir: /path/to/git/dir".
    /// </summary>
    private static bool IsGitWorktreeFile(string gitPath)
    {
        try
        {
            if (!File.Exists(gitPath))
            {
                return false;
            }

            // Read first line to check for gitdir reference
            // Format: "gitdir: /path/to/main/repo/.git/worktrees/worktree-name"
            using var reader = new StreamReader(gitPath);
            var firstLine = reader.ReadLine();
            return firstLine != null && firstLine.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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
    
    protected IEnumerable<SourceGeneratedPath> Distinct(Root root, List<FileInfo> files, SourceProductionContext? context = null)
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

                    var (relativePath, fallbackReason) = root.TryMakeRelativePath(fileSystemInfo.FullName);

                    // Report fallback usage if context is provided
                    if (fallbackReason != null && context.HasValue)
                    {
                        context.Value.ReportRelativePathFallback(fileSystemInfo.FullName, fallbackReason);
                    }

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