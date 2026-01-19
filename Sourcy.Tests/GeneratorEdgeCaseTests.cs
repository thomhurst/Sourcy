namespace Sourcy.Tests;

/// <summary>
/// Tests for edge cases and advanced scenarios in source generators.
/// </summary>
public class GeneratorEdgeCaseTests
{
    [Test]
    public async Task Git_RootDirectory_IsNotNull()
    {
        await Assert.That(Git.RootDirectory).IsNotNull();
    }

    [Test]
    public async Task Git_RootDirectory_Exists()
    {
        await Assert.That(Git.RootDirectory.Exists).IsTrue();
    }

    [Test]
    public async Task Git_BranchName_IsNotEmpty()
    {
        await Assert.That(Git.BranchName).IsNotEmpty();
    }

    [Test]
    public async Task DotNet_Projects_AllExist()
    {
        // Verify all generated project references point to existing files
        var projects = new[]
        {
            DotNet.Projects.Sourcy,
            DotNet.Projects.Sourcy_Core,
            DotNet.Projects.Sourcy_DotNet,
            DotNet.Projects.Sourcy_Docker,
            DotNet.Projects.Sourcy_Git,
            DotNet.Projects.Sourcy_Node,
            DotNet.Projects.Sourcy_Pipeline,
            DotNet.Projects.Sourcy_Tests,
        };

        foreach (var project in projects)
        {
            await Assert.That(project.Exists).IsTrue();
        }
    }

    [Test]
    public async Task DotNet_Projects_HaveCorrectExtensions()
    {
        var projects = new[]
        {
            DotNet.Projects.Sourcy,
            DotNet.Projects.Sourcy_Core,
            DotNet.Projects.Sourcy_DotNet,
        };

        foreach (var project in projects)
        {
            await Assert.That(project.Extension).IsEqualTo(".csproj");
        }
    }

    [Test]
    public async Task DotNet_Solutions_Exist()
    {
        await Assert.That(DotNet.Solutions.Sourcy.Exists).IsTrue();
    }

    [Test]
    public async Task DotNet_Solutions_HaveCorrectExtension()
    {
        await Assert.That(DotNet.Solutions.Sourcy.Extension).IsEqualTo(".sln");
    }

    [Test]
    public async Task Docker_Dockerfiles_Exist()
    {
        await Assert.That(Docker.Dockerfiles.MongoDB.Exists).IsTrue();
    }

    [Test]
    public async Task Docker_Dockerfiles_HaveCorrectName()
    {
        await Assert.That(Docker.Dockerfiles.MongoDB.Name).IsEqualTo("Dockerfile");
    }

    [Test]
    public async Task Git_RootDirectory_ContainsExpectedFiles()
    {
        var rootFiles = Git.RootDirectory.GetFiles("*.sln");

        await Assert.That(rootFiles.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Git_RootDirectory_ContainsExpectedDirectories()
    {
        var directories = Git.RootDirectory.GetDirectories();

        await Assert.That(directories.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task DotNet_Projects_CanBeReadAsFileInfo()
    {
        var project = DotNet.Projects.Sourcy;

        // Verify FileInfo methods work correctly
        await Assert.That(project.Name).IsNotEmpty();
        await Assert.That(project.DirectoryName).IsNotNull();
        await Assert.That(project.FullName).Contains("Sourcy");
    }

    [Test]
    public async Task Git_BranchName_DoesNotContainInvalidCharacters()
    {
        var branchName = Git.BranchName;

        // Branch names should not contain line breaks or null characters
        await Assert.That(branchName).DoesNotContain("\n");
        await Assert.That(branchName).DoesNotContain("\r");
        await Assert.That(branchName).DoesNotContain("\0");
    }
}
