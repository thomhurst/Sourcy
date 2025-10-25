#pragma warning disable RS1035

using System;
using Microsoft.CodeAnalysis;

namespace Sourcy;

/// <summary>
/// Centralized diagnostic reporting for Sourcy source generators.
/// Provides consistent error, warning, and info messages to users.
/// </summary>
internal static class SourcyDiagnostics
{
    private const string Category = "Sourcy";

    // Diagnostic IDs
    private const string RootNotFoundId = "SOURCY001";
    private const string FileSkippedId = "SOURCY002";
    private const string GenerationErrorId = "SOURCY003";
    private const string GitCommandFailedId = "SOURCY004";
    private const string GitNotAvailableId = "SOURCY005";
    private const string InvalidIdentifierId = "SOURCY006";
    private const string PathTooLongId = "SOURCY007";
    private const string UnauthorizedAccessId = "SOURCY008";
    private const string UriFormatErrorId = "SOURCY009";
    private const string GenerationSuccessId = "SOURCY100";
    private const string FallbackUsedId = "SOURCY101";

    // Diagnostic Descriptors
    public static readonly DiagnosticDescriptor RootNotFound = new(
        id: RootNotFoundId,
        title: "Repository root not found",
        messageFormat: "Could not find repository root. Ensure a .git directory or .sourcyroot file exists in a parent directory.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Sourcy needs to locate the repository root to generate file paths. Create a .sourcyroot marker file if not using git."
    );

    public static readonly DiagnosticDescriptor FileSkipped = new(
        id: FileSkippedId,
        title: "File skipped during generation",
        messageFormat: "File '{0}' was skipped: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A file was intentionally skipped during generation."
    );

    public static readonly DiagnosticDescriptor GenerationError = new(
        id: GenerationErrorId,
        title: "Error during source generation",
        messageFormat: "Error generating code for '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An error occurred while generating source code for a specific item."
    );

    public static readonly DiagnosticDescriptor GitCommandFailed = new(
        id: GitCommandFailedId,
        title: "Git command failed",
        messageFormat: "Git command '{0}' failed after retries: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A git command could not be executed successfully."
    );

    public static readonly DiagnosticDescriptor GitNotAvailable = new(
        id: GitNotAvailableId,
        title: "Git is not available",
        messageFormat: "Git is not installed or not available on PATH. Using fallback values: {0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Git commands cannot be executed. Fallback values will be used instead."
    );

    public static readonly DiagnosticDescriptor InvalidIdentifier = new(
        id: InvalidIdentifierId,
        title: "Invalid identifier sanitized",
        messageFormat: "File name '{0}' contained invalid characters and was sanitized to '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false, // Only enable if users want verbose output
        description: "A file name was automatically sanitized to create a valid C# identifier."
    );

    public static readonly DiagnosticDescriptor PathTooLong = new(
        id: PathTooLongId,
        title: "Path too long",
        messageFormat: "Path '{0}' exceeds system limits and was skipped",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A file or directory path was too long to process."
    );

    public static readonly DiagnosticDescriptor UnauthorizedAccess = new(
        id: UnauthorizedAccessId,
        title: "Access denied",
        messageFormat: "Access denied to '{0}' - file/directory skipped",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A file or directory could not be accessed due to permissions."
    );

    public static readonly DiagnosticDescriptor UriFormatError = new(
        id: UriFormatErrorId,
        title: "Invalid path characters",
        messageFormat: "Path '{0}' contains invalid characters: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A path contains characters that cannot be processed."
    );

    public static readonly DiagnosticDescriptor GenerationSuccess = new(
        id: GenerationSuccessId,
        title: "Generation successful",
        messageFormat: "Successfully generated {0} properties for {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false, // Only enable if users want verbose output
        description: "Source generation completed successfully."
    );

    public static readonly DiagnosticDescriptor FallbackUsed = new(
        id: FallbackUsedId,
        title: "Fallback value used",
        messageFormat: "Using fallback value for '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A fallback value was used when the actual value could not be determined."
    );

    // Helper methods for reporting diagnostics
    public static void ReportRootNotFound(this SourceProductionContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(RootNotFound, Location.None));
    }

    public static void ReportFileSkipped(this SourceProductionContext context, string filePath, string reason)
    {
        context.ReportDiagnostic(Diagnostic.Create(FileSkipped, Location.None, filePath, reason));
    }

    public static void ReportGenerationError(this SourceProductionContext context, string itemName, Exception exception)
    {
        context.ReportDiagnostic(Diagnostic.Create(GenerationError, Location.None, itemName, exception.Message));
    }

    public static void ReportGitCommandFailed(this SourceProductionContext context, string command, string error)
    {
        context.ReportDiagnostic(Diagnostic.Create(GitCommandFailed, Location.None, command, error));
    }

    public static void ReportGitNotAvailable(this SourceProductionContext context, string fallbackInfo)
    {
        context.ReportDiagnostic(Diagnostic.Create(GitNotAvailable, Location.None, fallbackInfo));
    }

    public static void ReportInvalidIdentifier(this SourceProductionContext context, string originalName, string sanitizedName)
    {
        context.ReportDiagnostic(Diagnostic.Create(InvalidIdentifier, Location.None, originalName, sanitizedName));
    }

    public static void ReportPathTooLong(this SourceProductionContext context, string path)
    {
        context.ReportDiagnostic(Diagnostic.Create(PathTooLong, Location.None, path));
    }

    public static void ReportUnauthorizedAccess(this SourceProductionContext context, string path)
    {
        context.ReportDiagnostic(Diagnostic.Create(UnauthorizedAccess, Location.None, path));
    }

    public static void ReportUriFormatError(this SourceProductionContext context, string path, string error)
    {
        context.ReportDiagnostic(Diagnostic.Create(UriFormatError, Location.None, path, error));
    }

    public static void ReportGenerationSuccess(this SourceProductionContext context, int count, string generatorType)
    {
        context.ReportDiagnostic(Diagnostic.Create(GenerationSuccess, Location.None, count, generatorType));
    }

    public static void ReportFallbackUsed(this SourceProductionContext context, string propertyName, string fallbackValue)
    {
        context.ReportDiagnostic(Diagnostic.Create(FallbackUsed, Location.None, propertyName, fallbackValue));
    }
}
