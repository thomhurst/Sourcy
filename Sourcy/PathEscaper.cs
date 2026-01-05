#pragma warning disable RS1035

using System;
using System.Runtime.InteropServices;

namespace Sourcy;

/// <summary>
/// Utility class for path operations in source generators.
/// Provides platform-aware path handling, escaping, and comparison.
/// </summary>
internal static class PathUtilities
{
    /// <summary>
    /// Gets whether the current platform uses case-sensitive paths.
    /// Linux is case-sensitive, Windows and macOS are case-insensitive.
    /// </summary>
    public static bool IsCaseSensitive => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Gets the appropriate StringComparer for path comparisons on the current platform.
    /// </summary>
    public static StringComparer PathComparer => IsCaseSensitive
        ? StringComparer.Ordinal
        : StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Gets the appropriate StringComparison for path comparisons on the current platform.
    /// </summary>
    public static StringComparison PathComparison => IsCaseSensitive
        ? StringComparison.Ordinal
        : StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Escapes a path for use in a verbatim string literal (@"...").
    /// In verbatim strings, only quotes need escaping (doubled).
    /// </summary>
    public static string EscapeForVerbatimString(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        return path.Replace("\"", "\"\"");
    }

    /// <summary>
    /// Escapes a string for use in a regular string literal ("...").
    /// </summary>
    public static string EscapeForStringLiteral(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("\0", "\\0");
    }

    /// <summary>
    /// Normalizes a path from git output to the platform-native format.
    /// Git on Windows can return Unix-style paths like "C:/path" or "/c/path" (MSYS style).
    /// </summary>
    public static string NormalizeGitPath(string gitPath)
    {
        if (string.IsNullOrEmpty(gitPath))
        {
            return gitPath;
        }

        // Normalize line endings first
        gitPath = gitPath.Replace("\r\n", "\n").Replace("\r", "\n").Trim();

        // On Windows, convert forward slashes to backslashes
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Handle MSYS-style paths: /c/Users/... -> C:\Users\...
            if (gitPath.Length >= 3 && gitPath[0] == '/' && char.IsLetter(gitPath[1]) && gitPath[2] == '/')
            {
                gitPath = $"{char.ToUpper(gitPath[1])}:{gitPath.Substring(2)}";
            }

            // Convert forward slashes to backslashes
            gitPath = gitPath.Replace('/', '\\');
        }

        return gitPath;
    }

    /// <summary>
    /// Compares two paths using platform-appropriate case sensitivity.
    /// </summary>
    public static bool PathEquals(string path1, string path2)
    {
        return string.Equals(path1, path2, PathComparison);
    }

    /// <summary>
    /// Checks if a path starts with a prefix using platform-appropriate case sensitivity.
    /// </summary>
    public static bool PathStartsWith(string path, string prefix)
    {
        return path.StartsWith(prefix, PathComparison);
    }
}

/// <summary>
/// Alias for backward compatibility - redirects to PathUtilities.
/// </summary>
internal static class PathEscaper
{
    public static string EscapeForVerbatimString(string path) => PathUtilities.EscapeForVerbatimString(path);
    public static string EscapeForStringLiteral(string value) => PathUtilities.EscapeForStringLiteral(value);
}
