using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace Sourcy.Pipeline.Modules;

[DependsOn<PackProjectsModule>]
public class PackagePathsParserModule : Module<List<File>>
{
    protected override async Task<List<File>?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packPackagesModuleResult = await context.GetModule<PackProjectsModule>();

        const string packageMarker = "Successfully created package '";

        return packPackagesModuleResult.ValueOrDefault!
            .SelectMany(x => x.StandardOutput.Split('\n'))
            .Select(line => line.Trim())
            .Where(line => line.Contains(packageMarker))
            .Select(line => line.Split(packageMarker)[1])
            .Select(path => path.TrimEnd('\'', '.'))
            .Select(path => new File(path))
            .ToList();
    }
}
