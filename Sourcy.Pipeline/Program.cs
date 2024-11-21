using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModularPipelines.Extensions;
using ModularPipelines.Host;
using Sourcy.Pipeline.Modules;
using Sourcy.Pipeline.Modules.LocalMachine;
using Sourcy.Pipeline.Settings;

await PipelineHostBuilder.Create()
    .ConfigureAppConfiguration((_, builder) =>
    {
        builder.AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, collection) =>
    {
        collection.Configure<NuGetSettings>(context.Configuration.GetSection("NuGet"));

        if (!context.HostingEnvironment.IsDevelopment())
        {
            collection.AddModule<UploadPackagesToNugetModule>();
        }
        
    })
    .AddModule<CreateLocalNugetFolderModule>()
    .AddModule<AddLocalNugetSourceModule>()
    .AddModule<UploadPackagesToLocalNuGetModule>()
    .AddModule<RunUnitTestsModule>()
    .AddModule<NugetVersionGeneratorModule>()
    .AddModule<PackProjectsModule>()
    .AddModule<PackageFilesRemovalModule>()
    .AddModule<PackagePathsParserModule>()
    .AddModule<TestNugetPackageModule>()
    .ExecutePipelineAsync();
