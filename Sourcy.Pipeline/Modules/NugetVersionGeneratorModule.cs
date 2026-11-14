using Microsoft.Extensions.Logging;
using ModularPipelines;
using ModularPipelines.Context;

namespace Sourcy.Pipeline.Modules;

public class NugetVersionGeneratorModule : Module<string>
{
    protected override async Task<string> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var gitVersionInformation = await context.Tools.Git.Versioning.GetVersioningInformationAsync(cancellationToken);

        context.Logger.LogInformation("NuGet Version to Package: {Version}", gitVersionInformation.SemVer);

        if (gitVersionInformation.BranchName == "main")
        {
            return gitVersionInformation.SemVer!;
        }

        return $"{gitVersionInformation.Major}.{gitVersionInformation.Minor}.{gitVersionInformation.Patch}-{gitVersionInformation.PreReleaseLabel}-{gitVersionInformation.CommitsSinceVersionSource}";
    }
}
