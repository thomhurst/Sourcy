using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Sourcy.DotNet;
using File = ModularPipelines.FileSystem.File;

namespace Sourcy.Pipeline.Modules;

[DependsOn<PackageFilesRemovalModule>]
[DependsOn<NugetVersionGeneratorModule>]
[DependsOn<RunUnitTestsModule>]
public class PackProjectsModule : Module<CommandResult[]>
{
    protected override async Task<CommandResult[]?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packageVersion = await context.GetModule<NugetVersionGeneratorModule>();

        IEnumerable<FileInfo> projects =
            [
                Projects.Sourcy,
                Projects.Sourcy_Core,
                Projects.Sourcy_DotNet,
                Projects.Sourcy_Git,
                Projects.Sourcy_Node,
                Projects.Sourcy_Docker,
            ];

        return await projects.SelectAsync(project =>
                Pack(context, project, packageVersion.ValueOrDefault!, cancellationToken))
            .ProcessOneAtATime();
    }

    private static async Task<CommandResult> Pack(IModuleContext context, File projectFile, string packageVersion, CancellationToken cancellationToken)
    {
        return await context.DotNet().Pack(new DotNetPackOptions
        {
            ProjectSolution = projectFile.Path,
            Configuration = "Release",
            IncludeSource = projectFile == Projects.Sourcy_Core,
            Properties = new List<KeyValue>
            {
                ("PackageVersion", packageVersion),
                ("Version", packageVersion),
                ("IsPack", "true")
            },
        }, cancellationToken: cancellationToken);
    }
}
