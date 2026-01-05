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
    private const string ShallowCloneId = "SOURCY102";
    private const string UncPathId = "SOURCY103";
    private const string SubmoduleDetectedId = "SOURCY104";
    private const string CustomRootUsedId = "SOURCY105";
    private const string InvalidCustomRootId = "SOURCY010";
    private const string SymlinkCycleId = "SOURCY011";
    private const string MaxDepthReachedId = "SOURCY012";

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
        messageFormat: "Git is not installed or not available on PATH, using fallback values: {0}",
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

    public static readonly DiagnosticDescriptor ShallowClone = new(
        id: ShallowCloneId,
        title: "Shallow clone detected",
        messageFormat: "Repository is a shallow clone. Some git operations may return incomplete results. Consider running 'git fetch --unshallow' for full history.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The git repository is a shallow clone which may limit some git-related source generation."
    );

    public static readonly DiagnosticDescriptor UncPath = new(
        id: UncPathId,
        title: "UNC/Network path detected",
        messageFormat: "Repository root is on a network path ({0}). This may cause slower build times and potential reliability issues.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Building from a network drive can cause performance and reliability issues."
    );

    public static readonly DiagnosticDescriptor SubmoduleDetected = new(
        id: SubmoduleDetectedId,
        title: "Git submodule detected",
        messageFormat: "Project is within a git submodule with superproject root at {0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The project appears to be within a git submodule."
    );

    public static readonly DiagnosticDescriptor CustomRootUsed = new(
        id: CustomRootUsedId,
        title: "Custom root path used",
        messageFormat: "Using custom root path from SourcyRootPath MSBuild property: {0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A custom root path was specified via the SourcyRootPath MSBuild property."
    );

    public static readonly DiagnosticDescriptor InvalidCustomRoot = new(
        id: InvalidCustomRootId,
        title: "Invalid custom root path",
        messageFormat: "SourcyRootPath '{0}' is invalid or does not exist. Falling back to auto-detection.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The specified SourcyRootPath is invalid, does not exist, or is not accessible."
    );

    public static readonly DiagnosticDescriptor SymlinkCycle = new(
        id: SymlinkCycleId,
        title: "Symlink cycle detected",
        messageFormat: "Skipped symlink cycle at '{0}' pointing to already-visited '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "A symbolic link creates a cycle and was skipped to prevent infinite recursion."
    );

    public static readonly DiagnosticDescriptor MaxDepthReached = new(
        id: MaxDepthReachedId,
        title: "Maximum directory depth reached",
        messageFormat: "Stopped traversing at '{0}' - maximum depth of {1} reached",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Directory traversal was stopped to prevent performance issues with very deep hierarchies."
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

    public static void ReportShallowClone(this SourceProductionContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(ShallowClone, Location.None));
    }

    public static void ReportUncPath(this SourceProductionContext context, string path)
    {
        context.ReportDiagnostic(Diagnostic.Create(UncPath, Location.None, path));
    }

    public static void ReportSubmoduleDetected(this SourceProductionContext context, string superprojectRoot)
    {
        context.ReportDiagnostic(Diagnostic.Create(SubmoduleDetected, Location.None, superprojectRoot));
    }

    public static void ReportCustomRootUsed(this SourceProductionContext context, string customPath)
    {
        context.ReportDiagnostic(Diagnostic.Create(CustomRootUsed, Location.None, customPath));
    }

    public static void ReportInvalidCustomRoot(this SourceProductionContext context, string invalidPath)
    {
        context.ReportDiagnostic(Diagnostic.Create(InvalidCustomRoot, Location.None, invalidPath));
    }

    public static void ReportSymlinkCycle(this SourceProductionContext context, string symlinkPath, string targetPath)
    {
        context.ReportDiagnostic(Diagnostic.Create(SymlinkCycle, Location.None, symlinkPath, targetPath));
    }

    public static void ReportMaxDepthReached(this SourceProductionContext context, string path, int maxDepth)
    {
        context.ReportDiagnostic(Diagnostic.Create(MaxDepthReached, Location.None, path, maxDepth));
    }
}
