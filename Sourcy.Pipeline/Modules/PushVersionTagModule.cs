using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Sourcy.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[DependsOn<UploadPackagesToNugetModule>]
[DependsOn<NugetVersionGeneratorModule>]
public class PushVersionTagModule : Module<CommandResult>
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithSkipWhen(async ctx =>
        {
            var uploadModule = await ctx.GetModule<UploadPackagesToNugetModule>();
            return uploadModule.IsSkipped
                ? SkipDecision.Skip("UploadPackagesToNugetModule was skipped")
                : SkipDecision.DoNotSkip;
        })
        .WithIgnoreFailuresWhen(async (ctx, ex) =>
        {
            var versionModule = await ctx.GetModule<NugetVersionGeneratorModule>();
            return ex.Message.Contains($"tag 'v{versionModule.ValueOrDefault}' already exists");
        })
        .Build();

    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionModule = await context.GetModule<NugetVersionGeneratorModule>();
        var version = versionModule.ValueOrDefault!;

        await context.Git().Commands.Tag(new GitTagOptions
        {
            TagName = $"v{version}",
        }, token: cancellationToken);

        return await context.Git().Commands.Push(new GitPushOptions
        {
            Tags = true
        }, token: cancellationToken);
    }
}
