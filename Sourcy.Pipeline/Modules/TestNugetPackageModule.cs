using EnumerableAsyncProcessor.Extensions;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using Sourcy.Pipeline.Modules.LocalMachine;

namespace Sourcy.Pipeline.Modules;

[DependsOn<NugetVersionGeneratorModule>]
[DependsOn<UploadPackagesToLocalNuGetModule>]
public class TestNugetPackageModule : Module<CommandResult[]>
{
    private readonly string[] _frameworks = ["net8.0"];

    protected override async Task<CommandResult[]> ExecuteAsync(IModuleContext context,
        CancellationToken cancellationToken)
    {
        var version = await context.GetModule<NugetVersionGeneratorModule>();

        var project = DotNet.Projects.Sourcy_NuGet_Tests;

        return await _frameworks.SelectAsync(framework =>
                context.RunSubModuleAsync(framework, token =>
                    context.Tools.DotNet.RunAsync(new DotNetRunOptions
                    {
                        Project = project.FullName,
                        Framework = framework,
                        Properties =
                        [
                            new KeyValue("SourcyVersion", version.ValueOrDefault!),
                            new KeyValue("SourcyDiagnostics", "true")
                        ]
                    }, cancellationToken: token)
                , cancellationToken)
            , cancellationToken: cancellationToken).ProcessOneAtATime();
    }
}
