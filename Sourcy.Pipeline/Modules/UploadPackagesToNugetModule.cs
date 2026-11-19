using EnumerableAsyncProcessor.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git;
using Sourcy.Pipeline.Settings;

namespace Sourcy.Pipeline.Modules;

[DependsOn<RunUnitTestsModule>]
[DependsOn<PackagePathsParserModule>]
[RunOnlyOnBranch("main")]
public class UploadPackagesToNugetModule(IOptions<NuGetSettings> nugetSettings) : Module<CommandResult[]>
{
    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithSkipWhen(_ => !nugetSettings.Value.ShouldPublish);

    protected override async Task OnBeforeExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packagePaths = await context.GetModule<PackagePathsParserModule>();

        foreach (var packagePath in packagePaths.ValueOrDefault!)
        {
            context.Logger.LogInformation("Uploading {File}", packagePath);
        }

        await base.OnBeforeExecuteAsync(context, cancellationToken);
    }

    protected override async Task<CommandResult[]> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nugetSettings.Value.ApiKey);

        var packagePaths = await context.GetModule<PackagePathsParserModule>();

        return await packagePaths.ValueOrDefault!
            .SelectAsync(async nugetFile => await context.Tools.DotNet.NuGet.PushAsync(new DotNetNuGetPushOptions([nugetFile.Path])
            {
                Source = "https://api.nuget.org/v3/index.json",
                ApiKey = nugetSettings.Value.ApiKey!,
                SkipDuplicate = true,
            }, cancellationToken: cancellationToken), cancellationToken: cancellationToken)
            .ProcessOneAtATime();
    }
}
