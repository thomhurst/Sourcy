using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.Exceptions;
using ModularPipelines.Git;
using ModularPipelines.Git.Options;

namespace Sourcy.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[DependsOn<UploadPackagesToNugetModule>]
[DependsOn<NugetVersionGeneratorModule>]
public class PushVersionTagModule : Module<CommandResult>
{
    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithSkipWhen(async (ctx, _) =>
        {
            var uploadModule = await ctx.GetModule<UploadPackagesToNugetModule>();
            return uploadModule is ModuleResult<CommandResult[]>.Skipped
                ? SkipDecision.Skip("UploadPackagesToNugetModule was skipped")
                : SkipDecision.DoNotSkip;
        })
        .WithIgnoreFailuresWhen(async (ctx, ex, _) =>
        {
            var versionModule = await ctx.GetModule<NugetVersionGeneratorModule>();
            return ex is CommandException commandException &&
                   commandException.Result.StandardError.Contains($"tag 'v{versionModule.ValueOrDefault}' already exists");
        });

    protected override async Task<CommandResult> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionModule = await context.GetModule<NugetVersionGeneratorModule>();
        var version = versionModule.ValueOrDefault!;

        await context.Tools.Git.Commands.Branches.TagAsync(new GitTagOptions
        {
            TagName = $"v{version}",
        }, cancellationToken: cancellationToken);

        return await context.Tools.Git.Commands.Remotes.PushAsync(new GitPushOptions
        {
            Tags = true
        }, cancellationToken: cancellationToken);
    }
}
