using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using File = ModularPipelines.FileSystem.FilePath;

namespace Sourcy.Pipeline.Modules;

public class RunUnitTestsModule : Module<CommandResult>
{
    protected override async Task<CommandResult> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        return await Run(context, DotNet.Projects.Sourcy_Tests, cancellationToken);
    }

    private static async Task<CommandResult> Run(IModuleContext context, File unitTestProjectFile, CancellationToken cancellationToken)
    {
        var dotNetTestOptions = new DotNetTestOptions
        {
            Project = unitTestProjectFile.Path
        };

        return await context.Tools.DotNet.TestAsync(dotNetTestOptions, cancellationToken: cancellationToken);
    }
}
