using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModularPipelines;
using ModularPipelines.Options;
using Sourcy.Pipeline.Modules;
using Sourcy.Pipeline.Modules.LocalMachine;
using Sourcy.Pipeline.Settings;

var builder = Pipeline.CreateBuilder();

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

builder.Services.Configure<NuGetSettings>(builder.Configuration.GetSection("NuGet"));

if (!builder.Environment.IsDevelopment())
{
    builder.AddModule<UploadPackagesToNugetModule>();
    builder.AddModule<PushVersionTagModule>();
    builder.AddModule<CreateReleaseModule>();
}

builder.AddModule<CreateLocalNugetFolderModule>();
builder.AddModule<AddLocalNugetSourceModule>();
builder.AddModule<UploadPackagesToLocalNuGetModule>();
builder.AddModule<RunUnitTestsModule>();
builder.AddModule<NugetVersionGeneratorModule>();
builder.AddModule<PackProjectsModule>();
builder.AddModule<PackageFilesRemovalModule>();
builder.AddModule<PackagePathsParserModule>();
builder.AddModule<TestNugetPackageModule>();

builder.ConfigureOptions(options => options with { FailureMode = FailureMode.ContinueOnFailure });

await builder.RunAsync();
