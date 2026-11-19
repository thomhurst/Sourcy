using EnumerableAsyncProcessor.Extensions;
using Microsoft.Extensions.Logging;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;

namespace Sourcy.Pipeline.Modules.LocalMachine;

[DependsOn<AddLocalNugetSourceModule>]
[DependsOn<PackagePathsParserModule>]
[DependsOn<CreateLocalNugetFolderModule>]
[RunIf<OnLinux>]
public class UploadPackagesToLocalNuGetModule : Module<CommandResult[]>
{
    protected override async Task OnBeforeExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packagePaths = await context.GetModule<PackagePathsParserModule>();

        foreach (var packagePath in packagePaths.ValueOrDefault!)
        {
            context.Logger.LogInformation("[Local Directory] Uploading {File}", packagePath);
        }

        await base.OnBeforeExecuteAsync(context, cancellationToken);
    }

    protected override async Task<CommandResult[]> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var localRepoLocation = await context.GetModule<CreateLocalNugetFolderModule>();
        var packagePaths = await context.GetModule<PackagePathsParserModule>();

        return await packagePaths.ValueOrDefault!
            .SelectAsync(async nugetFile => await context.Tools.DotNet.NuGet.PushAsync(new DotNetNuGetPushOptions([nugetFile.Path])
            {
                Source = localRepoLocation.ValueOrDefault.AssertExists(),
            }, cancellationToken: cancellationToken), cancellationToken: cancellationToken)
            .ProcessOneAtATime();
    }
}
