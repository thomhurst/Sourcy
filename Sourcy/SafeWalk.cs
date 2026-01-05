#pragma warning disable RS1035

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sourcy;

/// <summary>
/// Describes why a path was skipped during enumeration.
/// </summary>
public enum SkipReason
{
    UnauthorizedAccess,
    DirectoryNotFound,
    PathTooLong,
    IoError,
    SecurityException,
    SymlinkCycle,
    MaxDepthReached,
    ExcludedDirectory,
    HiddenOrSystem,
    CloudPlaceholder
}

/// <summary>
/// Information about a skipped path during enumeration.
/// </summary>
public readonly struct SkippedPath
{
    public string Path { get; }
    public SkipReason Reason { get; }
    public string? TargetPath { get; } // For symlink cycles
    public int? Depth { get; } // For max depth

    public SkippedPath(string path, SkipReason reason, string? targetPath = null, int? depth = null)
    {
        Path = path;
        Reason = reason;
        TargetPath = targetPath;
        Depth = depth;
    }
}

/// <summary>
/// Callback delegate for reporting skipped paths during enumeration.
/// </summary>
public delegate void SkippedPathCallback(SkippedPath skippedPath);

internal static class SafeWalk
{
    // Maximum depth to prevent runaway recursion
    private const int MaxDepth = 50;

    public static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory, SkippedPathCallback? onSkipped = null)
    {
        foreach (var dir in EnumerateDirectories(directory, onSkipped))
        {
            FileInfo[]? files = null;

            try
            {
                files = dir.GetFiles("*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                onSkipped?.Invoke(new SkippedPath(dir.FullName, SkipReason.UnauthorizedAccess));
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                onSkipped?.Invoke(new SkippedPath(dir.FullName, SkipReason.DirectoryNotFound));
                continue;
            }
            catch (PathTooLongException)
            {
                onSkipped?.Invoke(new SkippedPath(dir.FullName, SkipReason.PathTooLong));
                continue;
            }
            catch (IOException)
            {
                onSkipped?.Invoke(new SkippedPath(dir.FullName, SkipReason.IoError));
                continue;
            }
            catch (System.Security.SecurityException)
            {
                onSkipped?.Invoke(new SkippedPath(dir.FullName, SkipReason.SecurityException));
                continue;
            }

            if (files != null)
            {
                // Sort files for deterministic output across builds
                Array.Sort(files, (a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));

                foreach (var file in files)
                {
                    // Skip cloud placeholder files (OneDrive, iCloud, Dropbox)
                    if (IsCloudPlaceholder(file))
                    {
                        onSkipped?.Invoke(new SkippedPath(file.FullName, SkipReason.CloudPlaceholder));
                        continue;
                    }

                    yield return file;
                }
            }
        }
    }

    // Extended list of directories to exclude for better cross-platform support
    private static readonly string[] ExcludedDirectories =
    [
        // Version control
        "node_modules", ".git", ".hg", ".svn", ".bzr",
        // Build outputs
        "bin", "obj", "packages", "TestResults",
        // IDE/Editor
        ".vs", ".vscode", ".idea",
        // Package managers
        ".npm", ".nuget", ".cargo", ".rustup",
        // Virtual environments
        ".venv", "venv", "__pycache__",
        // Windows system
        "$RECYCLE.BIN", "System Volume Information",
        // macOS
        ".Trash", ".Spotlight-V100", ".fseventsd"
    ];

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(DirectoryInfo directory, SkippedPathCallback? onSkipped = null)
    {
        // Use a visited set to detect symlink cycles with platform-appropriate case sensitivity
        var visited = new HashSet<string>(PathUtilities.PathComparer);
        return EnumerateDirectoriesInternal(directory, visited, 0, onSkipped);
    }

    private static IEnumerable<DirectoryInfo> EnumerateDirectoriesInternal(
        DirectoryInfo directory,
        HashSet<string> visited,
        int depth,
        SkippedPathCallback? onSkipped)
    {
        // Prevent infinite recursion from deep directories
        if (depth > MaxDepth)
        {
            onSkipped?.Invoke(new SkippedPath(directory.FullName, SkipReason.MaxDepthReached, depth: MaxDepth));
            yield break;
        }

        var (shouldSearch, skipReason) = ShouldSearchDirectoryWithReason(directory);
        if (!shouldSearch)
        {
            if (skipReason.HasValue)
            {
                onSkipped?.Invoke(new SkippedPath(directory.FullName, skipReason.Value));
            }
            yield break;
        }

        // Resolve symlinks to detect cycles - get the real path
        string realPath;
        try
        {
            realPath = GetRealPath(directory);
        }
        catch
        {
            onSkipped?.Invoke(new SkippedPath(directory.FullName, SkipReason.IoError));
            yield break;
        }

        // Check for cycles (symlink pointing to parent directory)
        if (!visited.Add(realPath))
        {
            onSkipped?.Invoke(new SkippedPath(directory.FullName, SkipReason.SymlinkCycle, targetPath: realPath));
            yield break;
        }

        yield return directory;

        DirectoryInfo[]? subDirectories = null;

        try
        {
            subDirectories = directory.GetDirectories("*", SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            onSkipped?.Invoke(new SkippedPath(directory.FullName, SkipReason.UnauthorizedAccess));
            yield break;
        }
        catch (DirectoryNotFoundException)
        {
            onSkipped?.Invoke(new SkippedPath(directory.FullName, SkipReason.DirectoryNotFound));
            yield break;
        }
        catch (PathTooLongException)
        {
            onSkipped?.Invoke(new SkippedPath(directory.FullName, SkipReason.PathTooLong));
            yield break;
        }
        catch (IOException)
        {
            onSkipped?.Invoke(new SkippedPath(directory.FullName, SkipReason.IoError));
            yield break;
        }
        catch (System.Security.SecurityException)
        {
            onSkipped?.Invoke(new SkippedPath(directory.FullName, SkipReason.SecurityException));
            yield break;
        }

        if (subDirectories == null)
        {
            yield break;
        }

        // Sort directories for deterministic output across builds
        Array.Sort(subDirectories, (a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));

        foreach (var folder in subDirectories)
        {
            IEnumerable<DirectoryInfo>? innerFolders = null;

            try
            {
                // Check if this is a symlink/junction - skip if it would cause a cycle
                if (IsSymbolicLinkOrJunction(folder))
                {
                    var targetPath = GetRealPath(folder);
                    if (visited.Contains(targetPath))
                    {
                        onSkipped?.Invoke(new SkippedPath(folder.FullName, SkipReason.SymlinkCycle, targetPath: targetPath));
                        continue;
                    }

                    // Mark the symlink target as visited to avoid revisiting it via other symlinks
                    visited.Add(targetPath);
                }

                innerFolders = EnumerateDirectoriesInternal(folder, visited, depth + 1, onSkipped);
            }
            catch (UnauthorizedAccessException)
            {
                onSkipped?.Invoke(new SkippedPath(folder.FullName, SkipReason.UnauthorizedAccess));
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                onSkipped?.Invoke(new SkippedPath(folder.FullName, SkipReason.DirectoryNotFound));
                continue;
            }
            catch (PathTooLongException)
            {
                onSkipped?.Invoke(new SkippedPath(folder.FullName, SkipReason.PathTooLong));
                continue;
            }
            catch (IOException)
            {
                onSkipped?.Invoke(new SkippedPath(folder.FullName, SkipReason.IoError));
                continue;
            }
            catch (System.Security.SecurityException)
            {
                onSkipped?.Invoke(new SkippedPath(folder.FullName, SkipReason.SecurityException));
                continue;
            }

            if (innerFolders != null)
            {
                foreach (var innerFolder in innerFolders)
                {
                    yield return innerFolder;
                }
            }
        }
    }

    private static bool ShouldSearchDirectory(DirectoryInfo directory)
    {
        var (shouldSearch, _) = ShouldSearchDirectoryWithReason(directory);
        return shouldSearch;
    }

    private static (bool ShouldSearch, SkipReason? Reason) ShouldSearchDirectoryWithReason(DirectoryInfo directory)
    {
        try
        {
            // Check if directory still exists
            if (!directory.Exists)
            {
                return (false, SkipReason.DirectoryNotFound);
            }

            var attributes = directory.Attributes;

            // Skip hidden directories (but allow on Linux where .folders are common)
            if ((attributes & FileAttributes.Hidden) != 0 && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return (false, SkipReason.HiddenOrSystem);
            }

            // Skip system directories
            if ((attributes & FileAttributes.System) != 0)
            {
                return (false, SkipReason.HiddenOrSystem);
            }
        }
        catch (UnauthorizedAccessException)
        {
            return (false, SkipReason.UnauthorizedAccess);
        }
        catch (System.Security.SecurityException)
        {
            return (false, SkipReason.SecurityException);
        }
        catch
        {
            // If we can't read attributes, skip the directory
            return (false, SkipReason.IoError);
        }

        if (File.Exists(Path.Combine(directory.FullName, ".sourcyignore")))
        {
            // Explicitly ignored - no reason needed as this is intentional
            return (false, null);
        }

        if (ExcludedDirectories.Contains(directory.Name, StringComparer.OrdinalIgnoreCase))
        {
            return (false, SkipReason.ExcludedDirectory);
        }

        return (true, null);
    }

    /// <summary>
    /// Resolves symlinks to get the real/canonical path.
    /// On Unix, this follows symlinks to their ultimate target.
    /// On Windows, this resolves junctions and symlinks.
    /// </summary>
    private static string GetRealPath(DirectoryInfo directory)
    {
        try
        {
            var fullPath = directory.FullName;

            // If this is a symlink/reparse point, try to resolve the target
            if (IsSymbolicLinkOrJunction(directory))
            {
                var resolvedPath = ResolveSymlinkTarget(directory);
                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    fullPath = resolvedPath!;
                }
            }

            // On case-insensitive platforms, normalize to lowercase for consistent comparison
            if (!PathUtilities.IsCaseSensitive)
            {
                fullPath = fullPath.ToLowerInvariant();
            }

            return fullPath;
        }
        catch
        {
            return directory.FullName;
        }
    }

    /// <summary>
    /// Attempts to resolve a symlink to its target path.
    /// Uses .NET 6+ APIs via reflection, with fallback for older runtimes.
    /// </summary>
    private static string? ResolveSymlinkTarget(DirectoryInfo directory)
    {
        try
        {
            // Try .NET 6+ API via reflection: DirectoryInfo.ResolveLinkTarget(returnFinalTarget: true)
            var resolveLinkTargetMethod = typeof(DirectoryInfo).GetMethod(
                "ResolveLinkTarget",
                new[] { typeof(bool) });

            if (resolveLinkTargetMethod != null)
            {
                var result = resolveLinkTargetMethod.Invoke(directory, new object[] { true });
                if (result is FileSystemInfo targetInfo)
                {
                    return targetInfo.FullName;
                }
            }

            // Fallback: Try to get the link target on Unix systems using /proc or readlink
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return ResolveUnixSymlink(directory.FullName);
            }

            // On Windows, the full path from DirectoryInfo already resolves most cases
            // For remaining edge cases, we accept the limitation on older .NET versions
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a symlink on Linux using /proc/self/fd trick or realpath-like behavior.
    /// </summary>
    private static string? ResolveUnixSymlink(string path)
    {
        try
        {
            // On Linux, we can use Directory.GetCurrentDirectory() trick with temporary chdir
            // But that's not thread-safe. Instead, try to read the link iteratively.

            var currentPath = path;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            const int maxIterations = 40; // Prevent infinite loops

            for (int i = 0; i < maxIterations; i++)
            {
                if (!visited.Add(currentPath))
                {
                    // Cycle detected
                    return currentPath;
                }

                var info = new DirectoryInfo(currentPath);
                if (!IsSymbolicLinkOrJunction(info))
                {
                    // Not a symlink, we've resolved it
                    return Path.GetFullPath(currentPath);
                }

                // Try to read the link target using .NET 6+ LinkTarget property
                var linkTargetProperty = typeof(FileSystemInfo).GetProperty("LinkTarget");
                if (linkTargetProperty != null)
                {
                    var target = linkTargetProperty.GetValue(info) as string;
                    if (!string.IsNullOrEmpty(target))
                    {
                        // If relative, make it absolute relative to the symlink's directory
                        if (!Path.IsPathRooted(target!))
                        {
                            var parentDir = Path.GetDirectoryName(currentPath);
                            if (parentDir != null)
                            {
                                target = Path.GetFullPath(Path.Combine(parentDir, target!));
                            }
                        }
                        currentPath = target!;
                        continue;
                    }
                }

                // Can't resolve further, return what we have
                return Path.GetFullPath(currentPath);
            }

            // Max iterations reached, return current path
            return Path.GetFullPath(currentPath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a directory is a symbolic link or junction point.
    /// </summary>
    private static bool IsSymbolicLinkOrJunction(DirectoryInfo directory)
    {
        try
        {
            return (directory.Attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a file is a cloud placeholder (OneDrive, iCloud, Dropbox, etc.).
    /// Cloud placeholders are files that exist in the cloud but aren't downloaded locally.
    /// Accessing them can trigger downloads or fail unexpectedly.
    /// </summary>
    private static bool IsCloudPlaceholder(FileInfo file)
    {
        try
        {
            var attributes = file.Attributes;

            // FileAttributes.Offline indicates the file data is not immediately available
            // This is commonly used by cloud sync providers for placeholder files
            if ((attributes & FileAttributes.Offline) != 0)
            {
                return true;
            }

            // ReparsePoint can indicate cloud placeholders on Windows (OneDrive, etc.)
            // Combined with SparseFile, this often indicates a cloud placeholder
            if ((attributes & FileAttributes.ReparsePoint) != 0 &&
                (attributes & FileAttributes.SparseFile) != 0)
            {
                return true;
            }

            return false;
        }
        catch
        {
            // If we can't read attributes, assume it's not a placeholder
            // The file might fail later but we'll handle that separately
            return false;
        }
    }
}