using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace Sourcy.Pipeline.Modules;

[DependsOn<PackageFilesRemovalModule>]
[DependsOn<NugetVersionGeneratorModule>]
[DependsOn<RunUnitTestsModule>]
public class PackProjectsModule : Module<CommandResult[]>
{
    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var packageVersion = await GetModule<NugetVersionGeneratorModule>();

        IEnumerable<FileInfo> projects =
            [
                Sourcy.DotNet.Projects.Sourcy,
                Sourcy.DotNet.Projects.Sourcy_DotNet,
                Sourcy.DotNet.Projects.Sourcy_Git,
                Sourcy.DotNet.Projects.Sourcy_Node,
                Sourcy.DotNet.Projects.Sourcy_Docker,
            ];

        return await projects.SelectAsync(project =>
                Pack(context, project, packageVersion.Value!, cancellationToken))
            .ProcessOneAtATime();
    }

    private static async Task<CommandResult> Pack(IPipelineContext context, File projectFile, string packageVersion, CancellationToken cancellationToken)
    {
        return await context.DotNet().Pack(new DotNetPackOptions
        {
            ProjectSolution = projectFile.Path,
            Configuration = Configuration.Release,
            IncludeSource = true,
            Properties = new List<KeyValue>
            {
                ("PackageVersion", packageVersion),
                ("Version", packageVersion),
            },
        }, cancellationToken);
    }
}