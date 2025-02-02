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
        await Task.WhenAll(
            RootDirectory(context, root.Directory.FullName),
            BranchName(context, root.Directory.FullName)
            );
    }

    private static async Task RootDirectory(SourceProductionContext context, string? location)
    {
        var root = await Policy.Handle<Exception>()
            .OrResult<BufferedCommandResult>(x => string.IsNullOrWhiteSpace(x.StandardOutput))
            .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i))
            .ExecuteAsync(async () => await Cli.Wrap("git")
                .WithArguments(["rev-parse", "--show-toplevel"])
                .WithWorkingDirectory(location!)
                .ExecuteBufferedAsync());

        context.AddSource("GitRootExtensions.g.cs", GetSourceText(
            $$"""
              namespace Sourcy;

              internal static partial class Git
              {
                  public static global::System.IO.DirectoryInfo RootDirectory { get; } = new global::System.IO.DirectoryInfo("{{root.StandardOutput.Trim()}}");
              }
              """
        ));
    }
    
    private static async Task BranchName(SourceProductionContext context, string? location)
    {
        var branch = await Policy.Handle<Exception>()
            .OrResult<BufferedCommandResult>(x => string.IsNullOrWhiteSpace(x.StandardOutput))
            .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i))
            .ExecuteAsync(async () => await Cli.Wrap("git")
                .WithArguments(["rev-parse", "--abbrev-ref", "HEAD"])
                .WithWorkingDirectory(location!)
                .ExecuteBufferedAsync());

        if (!string.IsNullOrWhiteSpace(branch.StandardOutput))
        {
            context.AddSource("GitBranchNameExtensions.g.cs", GetSourceText(
                $$"""
                  namespace Sourcy;

                  internal static partial class Git
                  {
                      public static string BranchName { get; } = "{{branch.StandardOutput.Trim()}}";
                  }
                  """
            ));
        }
    }
}