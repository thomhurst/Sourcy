namespace Sourcy.NuGet.Tests;

public class GitTests
{
    [Test]
    public async Task Can_Retrieve_Root_Directory()
    {
        var root = Git.RootDirectory;

        await Assert.That(root.Name).IsEqualTo("Sourcy");
    }
    
    [Test]
    public async Task Can_Retrieve_BranchName()
    {
        var branchName = Git.BranchName;

        await Assert.That(string.IsNullOrWhiteSpace(branchName)).IsFalse();
        await Assert.That(branchName).EndsWith(GitVersionInformation.BranchName);
    }
}
