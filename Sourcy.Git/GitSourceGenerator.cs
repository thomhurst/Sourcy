using System;
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
        var root = await Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i))
            .ExecuteAsync(async () => await GetGitOutput(location!, ["rev-parse", "--show-toplevel"]));

        context.AddSource("GitRootExtensions.g.cs", GetSourceText(
            $$"""
              namespace Sourcy;

              internal static partial class Git
              {
                  public static global::System.IO.DirectoryInfo RootDirectory { get; } = new global::System.IO.DirectoryInfo("{{root}}");
              }
              """
        ));
    }

    private static async Task BranchName(SourceProductionContext context, string? location)
    {
        var branch = await Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i))
            .ExecuteAsync(async () => await GetGitOutput(location!, ["rev-parse", "--abbrev-ref", "HEAD"]));

        context.AddSource("GitBranchNameExtensions.g.cs", GetSourceText(
            $$"""
              namespace Sourcy;

              internal static partial class Git
              {
                  public static string BranchName { get; } = "{{branch}}";
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