using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.CodeAnalysis;
using Polly;

namespace Sourcy.Git;

[Generator]
internal class GitSourceGenerator : BaseSourcyGenerator
{
    // Timeout for git commands - generous but prevents infinite hangs
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(30);

    protected override void Initialize(SourceProductionContext context, Root root)
    {
        ExecuteAsync(context, root).GetAwaiter().GetResult();
    }

    private static async Task ExecuteAsync(SourceProductionContext context, Root root)
    {
        var location = root.Directory.FullName;

        // Cache for git command results within this execution
        var gitCache = new Dictionary<string, string>(StringComparer.Ordinal);

        // First, check if git is available
        if (!await IsGitAvailable(context, location))
        {
            // Generate fallback values and return
            GenerateFallbackSource(context, location);
            return;
        }

        // Check for shallow clone and emit informational diagnostic
        await CheckShallowClone(context, location, gitCache);

        // Check for submodule and emit informational diagnostic
        await CheckSubmodule(context, location, gitCache);

        // Generate the actual source
        await RootDirectory(context, location, gitCache);
        await BranchName(context, location, gitCache);
    }

    /// <summary>
    /// Checks if git is available and working in the given directory.
    /// </summary>
    private static async Task<bool> IsGitAvailable(SourceProductionContext context, string location)
    {
        try
        {
            // Simple check: try to get git version
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var result = await Cli.Wrap("git")
                .WithArguments(["--version"])
                .WithWorkingDirectory(location)
                .WithValidation(CommandResultValidation.None)
                .WithStandardInputPipe(PipeSource.Null) // Prevent credential prompts from hanging
                .ExecuteBufferedAsync(cts.Token);

            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.StandardOutput))
            {
                return true;
            }

            context.ReportGitNotAvailable("Git command returned unexpected result");
            return false;
        }
        catch (OperationCanceledException)
        {
            context.ReportGitNotAvailable("Git command timed out - git may be hanging or unavailable");
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            context.ReportGitNotAvailable("Git executable not found. Ensure git is installed and available on PATH.");
            return false;
        }
        catch (Exception ex)
        {
            context.ReportGitNotAvailable($"Git check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Generates fallback source when git is not available.
    /// </summary>
    private static void GenerateFallbackSource(SourceProductionContext context, string location)
    {
        context.ReportFallbackUsed("Git.RootDirectory", location);
        context.ReportFallbackUsed("Git.BranchName", "unknown");

        context.AddSource("GitRootExtensions.g.cs", GetSourceText(
            $$"""
              namespace Sourcy;

              [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Sourcy.Git", "1.0.0")]
              internal static partial class Git
              {
                  public static global::System.IO.DirectoryInfo RootDirectory { get; } = new global::System.IO.DirectoryInfo(@"{{PathUtilities.EscapeForVerbatimString(location)}}");
              }
              """
        ));

        context.AddSource("GitBranchNameExtensions.g.cs", GetSourceText(
            """
              namespace Sourcy;

              internal static partial class Git
              {
                  public static string BranchName { get; } = "unknown";
              }
              """
        ));
    }

    /// <summary>
    /// Checks if the repository is a shallow clone and emits a diagnostic.
    /// </summary>
    private static async Task CheckShallowClone(SourceProductionContext context, string location, Dictionary<string, string> cache)
    {
        try
        {
            var result = await GetGitOutputCached(location, ["rev-parse", "--is-shallow-repository"], cache);
            if (string.Equals(result.Trim(), "true", StringComparison.OrdinalIgnoreCase))
            {
                context.ReportShallowClone();
            }
        }
        catch (Exception ex)
        {
            // Shallow clone detection is informational - report as unexpected error for edge case debugging
            context.ReportUnexpectedError("CheckShallowClone", ex);
        }
    }

    /// <summary>
    /// Checks if the repository is a git submodule and emits a diagnostic.
    /// </summary>
    private static async Task CheckSubmodule(SourceProductionContext context, string location, Dictionary<string, string> cache)
    {
        try
        {
            // Note: git rev-parse --show-superproject-working-tree returns empty output when NOT a submodule,
            // which is expected behavior. We use GetGitOutputAllowEmpty to handle this gracefully.
            var result = await GetGitOutputAllowEmptyCached(location, ["rev-parse", "--show-superproject-working-tree"], cache);
            var superproject = result.Trim();

            if (!string.IsNullOrEmpty(superproject))
            {
                context.ReportSubmoduleDetected(PathUtilities.NormalizeGitPath(superproject));
            }
        }
        catch (Exception ex)
        {
            // Submodule detection is informational - report as unexpected error for edge case debugging
            context.ReportUnexpectedError("CheckSubmodule", ex);
        }
    }

    private static async Task RootDirectory(SourceProductionContext context, string location, Dictionary<string, string> cache)
    {
        string rootPath;

        try
        {
            var root = await Policy
                .Handle<TimeoutException>()
                .Or<IOException>()
                .Or<InvalidOperationException>()
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i))
                .ExecuteAsync(async () => await GetGitOutputCached(location, ["rev-parse", "--show-toplevel"], cache));

            // Normalize git's Unix-style output to platform-native path format
            rootPath = PathUtilities.NormalizeGitPath(root);
        }
        catch (Exception ex)
        {
            // Git command failed - use fallback to the directory we started searching from
            rootPath = location;

            context.ReportGitCommandFailed("git rev-parse --show-toplevel", ex.Message);
            context.ReportFallbackUsed("Git.RootDirectory", rootPath);
        }

        context.AddSource("GitRootExtensions.g.cs", GetSourceText(
            $$"""
              namespace Sourcy;

              [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Sourcy.Git", "1.0.0")]
              internal static partial class Git
              {
                  public static global::System.IO.DirectoryInfo RootDirectory { get; } = new global::System.IO.DirectoryInfo(@"{{PathUtilities.EscapeForVerbatimString(rootPath)}}");
              }
              """
        ));
    }

    private static async Task BranchName(SourceProductionContext context, string location, Dictionary<string, string> cache)
    {
        string branchName;

        try
        {
            var branch = await Policy
                .Handle<TimeoutException>()
                .Or<IOException>()
                .Or<InvalidOperationException>()
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i))
                .ExecuteAsync(async () => await GetGitOutputCached(location, ["rev-parse", "--abbrev-ref", "HEAD"], cache));

            branchName = branch.Trim();

            // Handle detached HEAD state - try to get a more useful identifier
            if (string.Equals(branchName, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                branchName = await GetDetachedHeadIdentifier(location, cache);
            }
        }
        catch (Exception ex)
        {
            // Git command failed - use fallback
            branchName = "unknown";

            context.ReportGitCommandFailed("git rev-parse --abbrev-ref HEAD", ex.Message);
            context.ReportFallbackUsed("Git.BranchName", branchName);
        }

        context.AddSource("GitBranchNameExtensions.g.cs", GetSourceText(
            $$"""
              namespace Sourcy;

              internal static partial class Git
              {
                  public static string BranchName { get; } = "{{PathUtilities.EscapeForStringLiteral(branchName)}}";
              }
              """
        ));
    }

    /// <summary>
    /// Gets a useful identifier when in detached HEAD state.
    /// Tries git describe first, then falls back to short commit hash.
    /// </summary>
    private static async Task<string> GetDetachedHeadIdentifier(string location, Dictionary<string, string> cache)
    {
        try
        {
            // Try to get a tag-based description (e.g., "v1.0.0-5-g1234567")
            var describe = await GetGitOutputCached(location, ["describe", "--tags", "--always"], cache);
            return describe.Trim();
        }
        catch
        {
            try
            {
                // Fall back to short commit hash
                var shortHash = await GetGitOutputCached(location, ["rev-parse", "--short", "HEAD"], cache);
                return $"detached-{shortHash.Trim()}";
            }
            catch
            {
                return "detached-HEAD";
            }
        }
    }

    /// <summary>
    /// Gets git output with caching to avoid redundant calls within the same execution.
    /// </summary>
    private static async Task<string> GetGitOutputCached(string location, string[] args, Dictionary<string, string> cache)
    {
        // Use explicit string key for stable, collision-free caching
        var cacheKey = $"{location}\0{string.Join("\0", args)}";

        if (cache.TryGetValue(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        var result = await GetGitOutput(location, args);
        cache[cacheKey] = result;
        return result;
    }

    /// <summary>
    /// Gets git output with caching, allowing empty output (for commands like --show-superproject-working-tree
    /// that return empty when the condition is not met).
    /// </summary>
    private static async Task<string> GetGitOutputAllowEmptyCached(string location, string[] args, Dictionary<string, string> cache)
    {
        // Use explicit string key with suffix for stable, collision-free caching
        var cacheKey = $"{location}\0{string.Join("\0", args)}\0allow-empty";

        if (cache.TryGetValue(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        var result = await GetGitOutputAllowEmpty(location, args);
        cache[cacheKey] = result;
        return result;
    }

    private static async Task<string> GetGitOutput(string location, string[] args)
    {
        using var cts = new CancellationTokenSource(GitTimeout);

        BufferedCommandResult bufferedCommandResult;
        try
        {
            bufferedCommandResult = await Cli.Wrap("git")
                .WithArguments(args)
                .WithWorkingDirectory(location)
                .WithStandardInputPipe(PipeSource.Null) // Prevent credential prompts from hanging
                .ExecuteBufferedAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"git {string.Join(" ", args)} timed out after {GitTimeout.TotalSeconds} seconds");
        }

        var output = bufferedCommandResult.StandardOutput.Trim();

        if (string.IsNullOrEmpty(output))
        {
            throw new Exception($"git {string.Join(" ", args)} returned no output.");
        }

        // Only treat stderr as error if exit code is non-zero
        // Git sometimes writes warnings to stderr even on success
        if (bufferedCommandResult.ExitCode != 0 && !string.IsNullOrWhiteSpace(bufferedCommandResult.StandardError))
        {
            throw new Exception($"git {string.Join(" ", args)} failed with exit code {bufferedCommandResult.ExitCode}: {bufferedCommandResult.StandardError}");
        }

        return output;
    }

    /// <summary>
    /// Gets git output, allowing empty output for commands that may legitimately return nothing.
    /// </summary>
    private static async Task<string> GetGitOutputAllowEmpty(string location, string[] args)
    {
        using var cts = new CancellationTokenSource(GitTimeout);

        BufferedCommandResult bufferedCommandResult;
        try
        {
            bufferedCommandResult = await Cli.Wrap("git")
                .WithArguments(args)
                .WithWorkingDirectory(location)
                .WithStandardInputPipe(PipeSource.Null) // Prevent credential prompts from hanging
                .ExecuteBufferedAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"git {string.Join(" ", args)} timed out after {GitTimeout.TotalSeconds} seconds");
        }

        // Only treat stderr as error if exit code is non-zero
        // Git sometimes writes warnings to stderr even on success
        if (bufferedCommandResult.ExitCode != 0 && !string.IsNullOrWhiteSpace(bufferedCommandResult.StandardError))
        {
            throw new Exception($"git {string.Join(" ", args)} failed with exit code {bufferedCommandResult.ExitCode}: {bufferedCommandResult.StandardError}");
        }

        return bufferedCommandResult.StandardOutput.Trim();
    }
}