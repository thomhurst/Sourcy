# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Sourcy is a C# Source Generator library that provides compile-time static properties for file paths within a repository. It generates strongly-typed paths to files and directories, avoiding relative path issues and machine-specific absolute paths.

The project consists of multiple NuGet packages:
- **Sourcy.Core**: Core functionality and base classes (`EnableSourcyAttribute`, base generator logic)
- **Sourcy**: Meta-package combining all generators
- **Sourcy.Git**: Generates Git root directory and branch name properties via `git` CLI
- **Sourcy.DotNet**: Locates `.csproj`/`.fsproj`/`.vbproj` projects and `.sln`/`.slnx`/`.slnf` solutions
- **Sourcy.Node**: Finds Node projects by locating `package-lock.json` or `yarn.lock` files
- **Sourcy.Docker**: Locates `Dockerfile`s in the repository
- **Sourcy.Pipeline**: ModularPipelines-based build/test/package/publish automation
- **Sourcy.Tests**: TUnit test suite that verifies all source generators

## Build and Test Commands

```bash
# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run the pipeline (build, test, pack, publish to local NuGet)
cd Sourcy.Pipeline
dotnet run -c Release

# Run a single test class
dotnet test --filter "FullyQualifiedName~DotNetTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~DotNetTests.Can_Find_Projects"
```

## Architecture

### Source Generator Design

All generators inherit from `BaseSourcyGenerator` (in Sourcy/BaseSourcyGenerator.cs) which implements `IIncrementalGenerator`. The base class:
- Locates the repository root by searching upward for `.git` folder or `.sourcyroot` file
- Provides utilities for generating source code with `GetSourceText()`
- Implements name collision handling via `Distinct()` method for duplicate file names

Each generator implements `Initialize(SourceProductionContext context, Root root)`:
- **Sourcy.Git**: Executes `git` commands via CliWrap with Polly retry logic to get root directory and branch name
- **Sourcy.DotNet**: Enumerates all project/solution files and generates static properties in `Sourcy.DotNet.Projects` and `Sourcy.DotNet.Solutions` classes
- **Sourcy.Docker**: Similar pattern for Dockerfiles
- **Sourcy.Node**: Similar pattern for Node projects

Generated code is placed in the `Sourcy` namespace (or sub-namespaces like `Sourcy.DotNet`) and provides `FileInfo` or `DirectoryInfo` objects.

### Packaging and Dependencies

- Uses **Central Package Management** (Directory.Packages.props)
- Source generator packages use `netstandard2.0` targeting
- Test project uses `net8.0`
- Generators mark dependencies as `PrivateAssets="all"` and include them in the analyzer output path
- See Sourcy.Git.csproj for example of bundling dependencies (CliWrap, Polly) with generators using `GetDependencyTargetPaths` MSBuild target

### Pipeline Architecture

Sourcy.Pipeline uses ModularPipelines framework with these key modules:
1. **NugetVersionGeneratorModule**: Generates version numbers (GitVersion)
2. **RunUnitTestsModule**: Runs test suite
3. **PackProjectsModule**: Packs all NuGet packages
4. **PackageFilesRemovalModule**: Cleans up old packages
5. **PackagePathsParserModule**: Parses package output paths
6. **CreateLocalNugetFolderModule** / **AddLocalNugetSourceModule** / **UploadPackagesToLocalNuGetModule**: Sets up local NuGet feed for testing
7. **TestNugetPackageModule**: Tests the generated packages
8. **UploadPackagesToNugetModule**: Publishes to nuget.org (only in non-dev environments)

Configuration via appsettings.json, user secrets, and environment variables (see Settings/NuGetSettings.cs).

## Important Conventions

### Source Generator Constraints
- Generators target `netstandard2.0` (Roslyn requirement)
- Must be incremental generators (`IIncrementalGenerator`)
- Cannot perform async operations in `Initialize` - Sourcy.Git works around this by calling `.GetAwaiter().GetResult()`
- All dependencies must be bundled with the generator in the `analyzers/dotnet/cs` package path

### Testing
- Tests use TUnit framework (not xUnit or NUnit)
- Tests reference generators as analyzers: `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`
- Tests verify generated static properties exist and point to correct file system locations

### Name Collision Handling
When multiple files share the same name (e.g., `src/Api/Api.csproj` and `tests/Api.Tests/Api.csproj`):
- Sourcy generates unique names based on relative paths
- Example: `Api` becomes `src__Api` and `tests__Api_Tests`
- Implementation in `BaseSourcyGenerator.Distinct()` method

## CI/CD

GitHub Actions workflow (.github/workflows/dotnet.yml):
- Runs on push/PR to main branch
- Executes `dotnet run -c Release` in Sourcy.Pipeline directory
- Caches NuGet packages
- Publishes packages when `publish-packages` input is true (workflow_dispatch only)
- Uses GitVersion for semantic versioning
