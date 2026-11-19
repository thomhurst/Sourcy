using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.Git;
using Octokit;

namespace Sourcy.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[DependsOn<PushVersionTagModule>]
[DependsOn<NugetVersionGeneratorModule>]
public class CreateReleaseModule : Module<Release>
{
    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithSkipWhen(async (ctx, _) =>
        {
            var pushTagModule = await ctx.GetModule<PushVersionTagModule>();
            return pushTagModule is ModuleResult<CommandResult>.Skipped
                ? SkipDecision.Skip("PushVersionTagModule was skipped")
                : SkipDecision.DoNotSkip;
        });

    protected override async Task<Release> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionModule = await context.GetModule<NugetVersionGeneratorModule>();
        var version = versionModule.ValueOrDefault!;

        var repositoryId = long.Parse(context.Tools.GitHub.EnvironmentVariables.RepositoryId!);

        Release? lastRelease = null;
        try
        {
            lastRelease = await context.Tools.GitHub.Client.Repository.Release.GetLatest(repositoryId);
        }
        catch (NotFoundException)
        {
            // No previous release exists
        }

        var releaseNotesRequest = new GenerateReleaseNotesRequest($"v{version}");

        if (lastRelease != null)
        {
            releaseNotesRequest.PreviousTagName = lastRelease.TagName;
        }

        var releaseNotes = await context.Tools.GitHub.Client.Repository.Release.GenerateReleaseNotes(repositoryId, releaseNotesRequest);

        return await context.Tools.GitHub.Client.Repository.Release.Create(
            repositoryId,
            new NewRelease($"v{version}")
            {
                Name = version,
                GenerateReleaseNotes = false,
                Body = releaseNotes.Body,
            });
    }
}
