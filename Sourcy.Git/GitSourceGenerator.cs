using System;
using System.IO;
using System.Runtime.InteropServices;
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
        await RootDirectory(context, root.Directory.FullName);
        await BranchName(context, root.Directory.FullName);
    }

    private static async Task RootDirectory(SourceProductionContext context, string? location)
    {
        string rootPath;

        try
        {
            var root = await Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i))
                .ExecuteAsync(async () => await GetGitOutput(location!, ["rev-parse", "--show-toplevel"]));

            // Normalize git's Unix-style output to platform-native path format
            rootPath = NormalizeGitPath(root);
        }
        catch (Exception ex)
        {
            // Git command failed - use fallback to the directory we started searching from
            rootPath = location ?? ".";

            context.ReportGitCommandFailed("git rev-parse --show-toplevel", ex.Message);
            context.ReportFallbackUsed("Git.RootDirectory", rootPath);
        }

        context.AddSource("GitRootExtensions.g.cs", GetSourceText(
            $$"""
              namespace Sourcy;

              internal static partial class Git
              {
                  public static global::System.IO.DirectoryInfo RootDirectory { get; } = new global::System.IO.DirectoryInfo(@"{{EscapePathForVerbatimString(rootPath)}}");
              }
              """
        ));
    }

    private static async Task BranchName(SourceProductionContext context, string? location)
    {
        string branchName;

        try
        {
            var branch = await Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i))
                .ExecuteAsync(async () => await GetGitOutput(location!, ["rev-parse", "--abbrev-ref", "HEAD"]));

            branchName = branch.Trim();

            // Handle detached HEAD state - try to get a more useful identifier
            if (string.Equals(branchName, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                branchName = await GetDetachedHeadIdentifier(location!);
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
                  public static string BranchName { get; } = "{{EscapeStringLiteral(branchName)}}";
              }
              """
        ));
    }

    /// <summary>
    /// Gets a useful identifier when in detached HEAD state.
    /// Tries git describe first, then falls back to short commit hash.
    /// </summary>
    private static async Task<string> GetDetachedHeadIdentifier(string location)
    {
        try
        {
            // Try to get a tag-based description (e.g., "v1.0.0-5-g1234567")
            var describe = await GetGitOutput(location, ["describe", "--tags", "--always"]);
            return describe.Trim();
        }
        catch
        {
            try
            {
                // Fall back to short commit hash
                var shortHash = await GetGitOutput(location, ["rev-parse", "--short", "HEAD"]);
                return $"detached-{shortHash.Trim()}";
            }
            catch
            {
                return "detached-HEAD";
            }
        }
    }

    private static async Task<string> GetGitOutput(string location, string[] args)
    {
        using var cts = new CancellationTokenSource(GitTimeout);

        BufferedCommandResult bufferedCommandResult;
        try
        {
            bufferedCommandResult = await Cli.Wrap("git")
                .WithArguments(args)
                .WithWorkingDirectory(location!)
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
    /// Escapes a path for use in a verbatim string literal (@"...").
    /// In verbatim strings, only quotes need escaping (doubled).
    /// </summary>
    private static string EscapePathForVerbatimString(string path)
    {
        return path.Replace("\"", "\"\"");
    }

    /// <summary>
    /// Escapes a string for use in a regular string literal ("...").
    /// </summary>
    private static string EscapeStringLiteral(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Normalizes a path from git output to the platform-native format.
    /// Git on Windows can return Unix-style paths like "C:/path" or "/c/path" (MSYS style).
    /// </summary>
    private static string NormalizeGitPath(string gitPath)
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
}