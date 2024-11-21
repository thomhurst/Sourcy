namespace Sourcy.NuGet.Tests;

public class GitTests
{
    [Test]
    public async Task Can_Retrieve_Root_Directory()
    {
        var root = Sourcy.Git.RootDirectory;

        await Assert.That(root.Name).IsEqualTo("Sourcy");
    }
    
    [Test]
    public async Task Can_Retrieve_BranchName()
    {
        var branchName = Sourcy.Git.BranchName;

        await Assert.That(branchName)
            .IsNotNullOrWhitespace()
            .And
            .EndsWith(GitVersionInformation.BranchName);
    }
}