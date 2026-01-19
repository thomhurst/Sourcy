using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModularPipelines;
using ModularPipelines.Extensions;
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
    builder.Services.AddModule<UploadPackagesToNugetModule>();
    builder.Services.AddModule<PushVersionTagModule>();
    builder.Services.AddModule<CreateReleaseModule>();
}

builder.Services.AddModule<CreateLocalNugetFolderModule>();
builder.Services.AddModule<AddLocalNugetSourceModule>();
builder.Services.AddModule<UploadPackagesToLocalNuGetModule>();
builder.Services.AddModule<RunUnitTestsModule>();
builder.Services.AddModule<NugetVersionGeneratorModule>();
builder.Services.AddModule<PackProjectsModule>();
builder.Services.AddModule<PackageFilesRemovalModule>();
builder.Services.AddModule<PackagePathsParserModule>();
builder.Services.AddModule<TestNugetPackageModule>();

builder.Options.ExecutionMode = ExecutionMode.WaitForAllModules;

await builder.Build().RunAsync();
