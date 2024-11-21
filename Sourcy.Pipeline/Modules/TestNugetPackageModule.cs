using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Sourcy.Pipeline.Modules.LocalMachine;

namespace Sourcy.Pipeline.Modules;

[DependsOn<NugetVersionGeneratorModule>]
[DependsOn<UploadPackagesToLocalNuGetModule>]
public class TestNugetPackageModule : Module<CommandResult[]>
{
    private readonly string[] _frameworks = ["net8.0", "net9.0"];

    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var version = await GetModule<NugetVersionGeneratorModule>();

        var project = Sourcy.DotNet.Projects.Sourcy_NuGet_Tests;

        return await _frameworks.SelectAsync(framework =>
                SubModule(framework, () =>
                    context.DotNet().Run(new DotNetRunOptions
                    {
                        Project = project.FullName,
                        Framework = framework,
                        Properties =
                        [
                            new KeyValue("SourcyVersion", version.Value!)
                        ]
                    }, cancellationToken)
                )
            , cancellationToken: cancellationToken).ProcessOneAtATime();
    }
}