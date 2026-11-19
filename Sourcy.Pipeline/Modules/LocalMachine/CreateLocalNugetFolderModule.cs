using Microsoft.Extensions.Logging;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.FileSystem;

namespace Sourcy.Pipeline.Modules.LocalMachine;

[DependsOn<RunUnitTestsModule>]
[DependsOn<PackagePathsParserModule>]
public class CreateLocalNugetFolderModule : Module<FolderPath>
{
    protected override Task<FolderPath> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localNugetRepositoryFolder = new FolderPath(Path.Combine(localAppData, "ModularPipelines", "LocalNuget"));
        localNugetRepositoryFolder.Create();

        context.Logger.LogInformation("Local NuGet Repository Path: {Path}", localNugetRepositoryFolder.Path);

        return Task.FromResult(localNugetRepositoryFolder);
    }
}
