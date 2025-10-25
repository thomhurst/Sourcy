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

    private static readonly string[] ExcludedDirectories = [ "node_modules", ".git"];

    public static IEnumerable<DirectoryInfo> EnumerateDirectories(DirectoryInfo directory)
    {
        if (!ShouldSearchDirectory(directory))
        {
            yield break;
        }

        yield return directory;

        var innerFolders = new List<DirectoryInfo>();

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