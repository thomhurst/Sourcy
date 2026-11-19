using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Exceptions;
using ModularPipelines.FileSystem;

namespace Sourcy.Pipeline.Modules.LocalMachine;

[DependsOn<CreateLocalNugetFolderModule>]
public class AddLocalNugetSourceModule : Module<CommandResult>
{
    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithIgnoreFailuresWhen((_, ex) =>
            ex is CommandException commandException &&
            commandException.Result.StandardOutput.Contains("The name specified has already been added to the list of available package sources"));

    protected override async Task<CommandResult> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var localNugetPathResult = await context.GetModule<CreateLocalNugetFolderModule>();

        return await context.Tools.DotNet.NuGet.Add.SourceAsync(new DotNetNuGetAddSourceOptions(localNugetPathResult.ValueOrDefault.AssertExists())
        {
            Name = "ModularPipelinesLocalNuGet",
        }, cancellationToken: cancellationToken);
    }
}
