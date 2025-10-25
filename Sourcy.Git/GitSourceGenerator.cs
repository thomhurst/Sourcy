using System;
using System.IO;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.CodeAnalysis;
using Polly;

namespace Sourcy.Git;

[Generator]
internal class GitSourceGenerator : BaseSourcyGenerator
{
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

            rootPath = root;
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
                  public static global::System.IO.DirectoryInfo RootDirectory { get; } = new global::System.IO.DirectoryInfo(@"{{rootPath}}");
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

            branchName = branch;
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
                  public static string BranchName { get; } = "{{branchName}}";
              }
              """
        ));
    }

    private static async Task<string> GetGitOutput(string location, string[] args)
    {
        var bufferedCommandResult = await Cli.Wrap("git")
            .WithArguments(args)
            .WithWorkingDirectory(location!)
            .ExecuteBufferedAsync();

        var output = bufferedCommandResult.StandardOutput.Trim();

        if (string.IsNullOrEmpty(output))
        {
            throw new Exception($"git {string.Join(" ", args)} returned no output.");
        }

        if(!string.IsNullOrWhiteSpace(bufferedCommandResult.StandardError))
        {
            throw new Exception($"git {string.Join(" ", args)} returned an error: {bufferedCommandResult.StandardError}");
        }

        return output;
    }
}