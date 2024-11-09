using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Enums;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace Sourcy.Pipeline.Modules;

public class RunUnitTestsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var projectFile = context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == "TomLonghurst.Selenium.PlaywrightWebDriver.Tests.csproj")
            .AssertExists();
        
        var results = new List<CommandResult>
        {
            await Run(context, cancellationToken, projectFile, true),
            await Run(context, cancellationToken, projectFile, false)
        };

        return results;
    }

    private static async Task<CommandResult> Run(IPipelineContext context, CancellationToken cancellationToken,
        File unitTestProjectFile, bool isV3)
    {
        var dotNetTestOptions = new DotNetTestOptions
        {
            ProjectSolutionDirectoryDllExe = unitTestProjectFile.Path,
            CommandLogging = CommandLogging.Input | CommandLogging.Error,
        };

        if (isV3)
        {
            dotNetTestOptions.Properties = new[] { new KeyValue("SeleniumVersion", "3") };
        }
        
        return await context.DotNet().Test(dotNetTestOptions, cancellationToken);
    }
}
