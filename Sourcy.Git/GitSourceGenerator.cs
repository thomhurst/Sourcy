#pragma warning disable RS1035

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sourcy.Git;

[Generator]
internal class GitSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
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
            RootDirectory(productionContext, location),
            BranchName(productionContext, location)
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

    private static string? GetLocation(Compilation compilation)
    {
        var assemblyLocations = compilation.Assembly.Locations;

        var fileLocation = assemblyLocations
                               .FirstOrDefault(x => x.Kind is LocationKind.MetadataFile)
                           ?? assemblyLocations.First();

        return Directory.GetParent(fileLocation.GetLineSpan().Path)!.FullName;
    }

    private static SourceText GetSourceText([StringSyntax("c#")] string code)
    {
        return SourceText.From(code, Encoding.UTF8);
    }
}