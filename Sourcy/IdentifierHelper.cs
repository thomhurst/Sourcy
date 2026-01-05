#pragma warning disable RS1035

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sourcy;

/// <summary>
/// Utility class for converting arbitrary strings into valid C# identifiers.
/// Ensures generated code compiles without errors by handling special characters,
/// keywords, leading numbers, and other edge cases.
/// </summary>
internal static class IdentifierHelper
{
    private const int MaxIdentifierLength = 511; // C# identifier length limit

    // C# keywords that need to be escaped with @ prefix
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while", "add", "alias", "ascending", "async", "await", "by",
        "descending", "dynamic", "equals", "from", "get", "global", "group", "into", "join",
        "let", "nameof", "on", "orderby", "partial", "remove", "select", "set", "value", "var",
        "when", "where", "yield", "record", "init", "with", "and", "or", "not", "nint", "nuint"
    };

    /// <summary>
    /// Converts an arbitrary string into a valid C# identifier.
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <param name="usedIdentifiers">Optional set of already-used identifiers to ensure uniqueness</param>
    /// <returns>A valid, unique C# identifier</returns>
    public static string ToValidIdentifier(string input, HashSet<string>? usedIdentifiers = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return GenerateFallbackName(usedIdentifiers);
        }

        var sanitized = SanitizeString(input);

        // Handle leading numbers
        if (char.IsDigit(sanitized[0]))
        {
            sanitized = "_" + sanitized;
        }

        // Handle C# keywords
        if (CSharpKeywords.Contains(sanitized))
        {
            sanitized = "@" + sanitized;
        }

        // Limit length
        if (sanitized.Length > MaxIdentifierLength)
        {
            sanitized = TruncateAndHash(sanitized, MaxIdentifierLength);
        }

        // Ensure uniqueness
        if (usedIdentifiers != null)
        {
            sanitized = EnsureUniqueness(sanitized, usedIdentifiers);
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes a string by replacing invalid characters with underscores
    /// and collapsing multiple underscores into one.
    /// </summary>
    private static string SanitizeString(string input)
    {
        var sb = new StringBuilder(input.Length);
        bool lastWasUnderscore = false;

        foreach (char c in input)
        {
            if (IsValidIdentifierChar(c))
            {
                sb.Append(c);
                lastWasUnderscore = false;
            }
            else if (!lastWasUnderscore)
            {
                sb.Append('_');
                lastWasUnderscore = true;
            }
        }

        // Trim trailing underscores
        while (sb.Length > 0 && sb[sb.Length - 1] == '_')
        {
            sb.Length--;
        }

        // Ensure we have at least one character
        if (sb.Length == 0)
        {
            sb.Append('_');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Checks if a character is valid in a C# identifier.
    /// </summary>
    private static bool IsValidIdentifierChar(char c)
    {
        // Valid: letters, digits, underscore
        // Also allow connecting characters and formatting characters for Unicode support
        return char.IsLetterOrDigit(c) ||
               c == '_' ||
               char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.ConnectorPunctuation;
    }

    /// <summary>
    /// Truncates a string to the specified length and appends a hash to maintain uniqueness.
    /// Uses a stable hash algorithm that produces consistent results across runs and processes.
    /// </summary>
    private static string TruncateAndHash(string input, int maxLength)
    {
        if (input.Length <= maxLength)
        {
            return input;
        }

        // Reserve 8 characters for hash
        int truncateLength = maxLength - 9; // -9 for underscore + 8 hex digits
        string truncated = input.Substring(0, truncateLength);

        // Generate a stable hash that is consistent across runs
        // Using FNV-1a algorithm which is simple, fast, and deterministic
        uint hash = GetStableHash(input);
        string hashString = hash.ToString("X8");

        return $"{truncated}_{hashString}";
    }

    /// <summary>
    /// Computes a stable hash using FNV-1a algorithm.
    /// Unlike GetHashCode(), this produces the same result across different runs and processes.
    /// Note: This hashes UTF-16 char values directly, so different Unicode normalizations
    /// of the same logical string may produce different hashes. This is acceptable for
    /// file path identifiers which are typically ASCII or consistently encoded by the OS.
    /// </summary>
    private static uint GetStableHash(string input)
    {
        // FNV-1a constants for 32-bit
        const uint fnvPrime = 16777619;
        const uint fnvOffsetBasis = 2166136261;

        uint hash = fnvOffsetBasis;

        foreach (char c in input)
        {
            hash ^= c;
            hash *= fnvPrime;
        }

        return hash;
    }

    /// <summary>
    /// Ensures the identifier is unique by appending a numeric suffix if needed.
    /// </summary>
    private static string EnsureUniqueness(string identifier, HashSet<string> usedIdentifiers)
    {
        if (usedIdentifiers.Add(identifier))
        {
            return identifier;
        }

        // Find an available suffix
        int suffix = 2;
        string candidate;

        do
        {
            candidate = $"{identifier}_{suffix}";
            suffix++;

            // Safety check to prevent infinite loop
            if (suffix > 10000)
            {
                candidate = $"{identifier}_{Guid.NewGuid():N}";
                break;
            }
        }
        while (!usedIdentifiers.Add(candidate));

        return candidate;
    }

    /// <summary>
    /// Generates a fallback name when the input is empty or invalid.
    /// </summary>
    private static string GenerateFallbackName(HashSet<string>? usedIdentifiers)
    {
        const string fallback = "GeneratedItem";

        if (usedIdentifiers == null)
        {
            return fallback;
        }

        return EnsureUniqueness(fallback, usedIdentifiers);
    }

    /// <summary>
    /// Sanitizes a file path-based name for use as a C# identifier.
    /// This is specifically for the Distinct() method pattern used in generators.
    /// </summary>
    public static string SanitizePathToIdentifier(string relativePath, string? extension = null)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return "Item";
        }

        var sanitized = relativePath;

        // Remove extension if provided
        if (!string.IsNullOrEmpty(extension))
        {
            sanitized = sanitized.Replace(extension, string.Empty);
        }

        // Replace path separators and dots
        sanitized = sanitized
            .Replace(@"\", "__")
            .Replace("/", "__")
            .Replace('.', '_');

        // Now apply standard identifier sanitization
        return ToValidIdentifier(sanitized);
    }
}
