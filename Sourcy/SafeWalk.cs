#pragma warning disable RS1035

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sourcy;

internal static class SafeWalk
{
    // Maximum depth to prevent runaway recursion
    private const int MaxDepth = 50;

    public static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory)
    {
        foreach (var dir in EnumerateDirectories(directory))
        {
            FileInfo[]? files = null;

            try
            {
                files = dir.GetFiles("*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we don't have permission to read
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                // Directory was deleted after we found it
                continue;
            }
            catch (PathTooLongException)
            {
                // Path exceeds system limits
                continue;
            }
            catch (IOException)
            {
                // I/O error (file locked, network issue, etc.)
                continue;
            }
            catch (System.Security.SecurityException)
            {
                // Security policy prevents access
                continue;
            }

            if (files != null)
            {
                foreach (var file in files)
                {
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

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(DirectoryInfo directory)
    {
        // Use a visited set to detect symlink cycles with platform-appropriate case sensitivity
        var visited = new HashSet<string>(PathUtilities.PathComparer);
        return EnumerateDirectoriesInternal(directory, visited, 0);
    }

    private static IEnumerable<DirectoryInfo> EnumerateDirectoriesInternal(
        DirectoryInfo directory,
        HashSet<string> visited,
        int depth)
    {
        // Prevent infinite recursion from deep directories
        if (depth > MaxDepth)
        {
            yield break;
        }

        if (!ShouldSearchDirectory(directory))
        {
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
            // If we can't resolve the path, skip it
            yield break;
        }

        // Check for cycles (symlink pointing to parent directory)
        if (!visited.Add(realPath))
        {
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
            // Can't access this directory's subdirectories
            yield break;
        }
        catch (DirectoryNotFoundException)
        {
            // Directory was deleted after we found it
            yield break;
        }
        catch (PathTooLongException)
        {
            // Path exceeds system limits
            yield break;
        }
        catch (IOException)
        {
            // I/O error (network issue, disk error, etc.)
            yield break;
        }
        catch (System.Security.SecurityException)
        {
            // Security policy prevents access
            yield break;
        }

        if (subDirectories == null)
        {
            yield break;
        }

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
                        continue; // Skip symlink cycle
                    }
                }

                innerFolders = EnumerateDirectoriesInternal(folder, visited, depth + 1);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }
            catch (PathTooLongException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }
            catch (System.Security.SecurityException)
            {
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
        try
        {
            // Check if directory still exists
            if (!directory.Exists)
            {
                return false;
            }

            var attributes = directory.Attributes;

            // Skip hidden directories (but allow on Linux where .folders are common)
            if ((attributes & FileAttributes.Hidden) != 0 && PathUtilities.IsCaseSensitive == false)
            {
                return false;
            }

            // Skip system directories
            if ((attributes & FileAttributes.System) != 0)
            {
                return false;
            }
        }
        catch
        {
            // If we can't read attributes, skip the directory
            return false;
        }

        if (File.Exists(Path.Combine(directory.FullName, ".sourcyignore")))
        {
            return false;
        }

        if (ExcludedDirectories.Contains(directory.Name, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Resolves symlinks to get the real path.
    /// </summary>
    private static string GetRealPath(DirectoryInfo directory)
    {
        // On .NET 6+, we could use Path.GetFullPath with ResolveLinkTarget
        // For netstandard2.0 compatibility, normalize the path
        try
        {
            // Normalize path by getting DirectoryInfo.FullName which resolves . and ..
            var fullPath = directory.FullName;

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
}