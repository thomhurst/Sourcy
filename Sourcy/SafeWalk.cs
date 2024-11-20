#pragma warning disable RS1035

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sourcy;

internal static class SafeWalk
{
    public static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory)
    {
        return EnumerateDirectories(directory)
            .SelectMany(x => x.EnumerateFiles("*", SearchOption.TopDirectoryOnly));
    }

    private static readonly string[] ExcludedDirectories = [ "node_modules", ".git"];
    
    public static IEnumerable<DirectoryInfo> EnumerateDirectories(DirectoryInfo directory)
    {
        if (!ShouldSearchDirectory(directory))
        {
            yield break;
        }
        
        yield return directory;

        var innerFolders = new List<DirectoryInfo>();
        
        foreach (var folder in directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
        {
            try
            {
                innerFolders.AddRange(EnumerateDirectories(folder!));
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }

            foreach (var innerFolder in innerFolders)
            {
                yield return innerFolder;
            }

            innerFolders.Clear();
        }
    }

    private static bool ShouldSearchDirectory(DirectoryInfo directory)
    {
        if ((directory.Attributes & FileAttributes.Hidden) != 0)
        {
            return false;
        }
        
        if (File.Exists(Path.Combine(directory.FullName, ".sourcyignore")))
        {
            return false;
        }

        if (ExcludedDirectories.Contains(directory.Name, StringComparer.InvariantCultureIgnoreCase))
        {
            return false;
        }

        return true;
    }
}