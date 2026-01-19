using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace Sourcy.Pipeline.Modules;

public class RunUnitTestsModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        return await Run(context, DotNet.Projects.Sourcy_Tests, cancellationToken);
    }

    private static async Task<CommandResult> Run(IModuleContext context, File unitTestProjectFile, CancellationToken cancellationToken)
    {
        var dotNetTestOptions = new DotNetTestOptions
        {
            Project = unitTestProjectFile.Path
        };

        return await context.DotNet().Test(dotNetTestOptions, cancellationToken: cancellationToken);
    }
}
