#pragma warning disable RS1035

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Sourcy;

[DebuggerDisplay("{Directory,nq}")]
public record Root(DirectoryInfo Directory) : IEqualityComparer<Root>
{
    public IEnumerable<FileInfo> EnumerateFiles(SkippedPathCallback? onSkipped = null)
    {
        return SafeWalk.EnumerateFiles(Directory, onSkipped);
    }

    public IEnumerable<DirectoryInfo> EnumerateDirectories(SkippedPathCallback? onSkipped = null)
    {
        return SafeWalk.EnumerateDirectories(Directory, onSkipped);
    }

    /// <summary>
    /// Makes a path relative to the root directory.
    /// </summary>
    public string MakeRelativePath(string filePath)
    {
        var (relativePath, _) = TryMakeRelativePath(filePath);
        return relativePath;
    }

    /// <summary>
    /// Makes a path relative to the root directory, returning info about whether fallback was used.
    /// </summary>
    /// <returns>A tuple of (relativePath, fallbackReason). fallbackReason is null if primary method succeeded.</returns>
    public (string RelativePath, string? FallbackReason) TryMakeRelativePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return (string.Empty, "Empty path");
        }

        try
        {
            var fileUri = new Uri(filePath);
            var rootUri = new Uri(Directory.FullName.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? Directory.FullName
                : Directory.FullName + Path.DirectorySeparatorChar);

            var relativeUri = rootUri.MakeRelativeUri(fileUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return (relativePath.Replace('/', Path.DirectorySeparatorChar), null);
        }
        catch (UriFormatException ex)
        {
            return (MakeRelativePathFallback(filePath), $"UriFormatException: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return (MakeRelativePathFallback(filePath), $"InvalidOperationException: {ex.Message}");
        }
        catch (ArgumentNullException ex)
        {
            return (MakeRelativePathFallback(filePath), $"ArgumentNullException: {ex.Message}");
        }
    }

    private string MakeRelativePathFallback(string filePath)
    {
        // Simple string-based relative path calculation
        var rootPath = Directory.FullName;

        if (!rootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            rootPath += Path.DirectorySeparatorChar;
        }

        // Use platform-appropriate case comparison via centralized PathUtilities
        if (PathUtilities.PathStartsWith(filePath, rootPath))
        {
            return filePath.Substring(rootPath.Length);
        }

        // If path is not under root, return the file name
        return Path.GetFileName(filePath);
    }

    public bool Equals(Root? x, Root? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null)
        {
            return false;
        }

        if (y is null)
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return x.Directory.FullName.Equals(y.Directory.FullName);
    }

    public int GetHashCode(Root obj)
    {
        return obj.Directory.GetHashCode();
    }
}