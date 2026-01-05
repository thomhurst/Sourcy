#pragma warning disable RS1035

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Sourcy;

[DebuggerDisplay("{Directory,nq}")]
public record Root(DirectoryInfo Directory) : IEqualityComparer<Root>
{
    public IEnumerable<FileInfo> EnumerateFiles()
    {
        return SafeWalk.EnumerateFiles(Directory);
    }

    public IEnumerable<DirectoryInfo> EnumerateDirectories()
    {
        return SafeWalk.EnumerateDirectories(Directory);
    }

    public string MakeRelativePath(string filePath)
    {
        try
        {
            var fileUri = new Uri(filePath);
            var rootUri = new Uri(Directory.FullName.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? Directory.FullName
                : Directory.FullName + Path.DirectorySeparatorChar);

            var relativeUri = rootUri.MakeRelativeUri(fileUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
        catch (UriFormatException)
        {
            // Fallback to string manipulation if Uri parsing fails
            return MakeRelativePathFallback(filePath);
        }
        catch (InvalidOperationException)
        {
            // MakeRelativeUri can throw if URIs can't be compared
            return MakeRelativePathFallback(filePath);
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