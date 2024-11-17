using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.CodeAnalysis;

namespace Sourcy.Git;

[Generator]
internal class GitSourceGenerator : BaseSourcyGenerator
{
    protected override void InitializeInternal(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (productionContext, compilation) =>
        {
            ExecuteAsync(productionContext, compilation).GetAwaiter().GetResult();
        });
    }

    private static async Task ExecuteAsync(SourceProductionContext productionContext, Compilation compilation)
    {
        var location = GetLocation(compilation);

        await Task.WhenAll(
            RootDirectory(productionContext, location.FullName),
            BranchName(productionContext, location.FullName)
            );
    }

    private static async Task RootDirectory(SourceProductionContext productionContext, string? location)
    {
        var root = await Cli.Wrap("git")
            .WithArguments(["rev-parse", "--show-toplevel"])
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(location!)
            .ExecuteBufferedAsync();

        if (root.IsSuccess && !string.IsNullOrWhiteSpace(root.StandardOutput))
        {
            productionContext.AddSource("GitRootExtensions.g.cs", GetSourceText(
                $$"""
                  namespace Sourcy;

                  internal static partial class Git
                  {
                      public static global::System.IO.DirectoryInfo RootDirectory { get; } = new global::System.IO.DirectoryInfo("{{root.StandardOutput.Trim()}}");
                  }
                  """
            ));
        }
    }
    
    private static async Task BranchName(SourceProductionContext productionContext, string? location)
    {
        var root = await Cli.Wrap("git")
            .WithArguments(["rev-parse", "--abbrev-ref", "HEAD"])
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(location!)
            .ExecuteBufferedAsync();

        if (root.IsSuccess && !string.IsNullOrWhiteSpace(root.StandardOutput))
        {
            productionContext.AddSource("GitBranchNameExtensions.g.cs", GetSourceText(
                $$"""
                  namespace Sourcy;

                  internal static partial class Git
                  {
                      public static string BranchName { get; } = "{{root.StandardOutput.Trim()}}";
                  }
                  """
            ));
        }
    }
}