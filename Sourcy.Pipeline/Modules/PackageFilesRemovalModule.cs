using ModularPipelines;
using ModularPipelines.Context;

namespace Sourcy.Pipeline.Modules;

public class PackageFilesRemovalModule : Module<bool>
{
    protected override async Task<bool> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packageFiles = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.GetFiles(path => path.Extension is ".nupkg");

        foreach (var packageFile in packageFiles)
        {
            packageFile.Delete();
        }

        return true;
    }
}
