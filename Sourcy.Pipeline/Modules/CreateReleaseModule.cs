using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.Git.Attributes;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Octokit;

namespace Sourcy.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[DependsOn<PushVersionTagModule>]
[DependsOn<NugetVersionGeneratorModule>]
public class CreateReleaseModule : Module<Release>
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithSkipWhen(async ctx =>
        {
            var pushTagModule = await ctx.GetModule<PushVersionTagModule>();
            return pushTagModule.IsSkipped
                ? SkipDecision.Skip("PushVersionTagModule was skipped")
                : SkipDecision.DoNotSkip;
        })
        .Build();

    protected override async Task<Release?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionModule = await context.GetModule<NugetVersionGeneratorModule>();
        var version = versionModule.ValueOrDefault!;

        var repositoryId = long.Parse(context.GitHub().EnvironmentVariables.RepositoryId!);

        Release? lastRelease = null;
        try
        {
            lastRelease = await context.GitHub().Client.Repository.Release.GetLatest(repositoryId);
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

        var releaseNotes = await context.GitHub().Client.Repository.Release.GenerateReleaseNotes(repositoryId, releaseNotesRequest);

        return await context.GitHub().Client.Repository.Release.Create(
            repositoryId,
            new NewRelease($"v{version}")
            {
                Name = version,
                GenerateReleaseNotes = false,
                Body = releaseNotes.Body,
            });
    }
}
