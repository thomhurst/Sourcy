using Microsoft.Extensions.Logging;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.FileSystem;
using ModularPipelines.Modules;

namespace Sourcy.Pipeline.Modules.LocalMachine;

[DependsOn<RunUnitTestsModule>]
[DependsOn<PackagePathsParserModule>]
public class CreateLocalNugetFolderModule : Module<Folder>
{
    protected override Task<Folder?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localNugetRepositoryFolder = new Folder(Path.Combine(localAppData, "ModularPipelines", "LocalNuget"));
        localNugetRepositoryFolder.Create();

        context.Logger.LogInformation("Local NuGet Repository Path: {Path}", localNugetRepositoryFolder.Path);

        return Task.FromResult<Folder?>(localNugetRepositoryFolder);
    }
}
