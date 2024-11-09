using System.Threading.Tasks;

namespace Sourcy.Tests;

public class DotNetTests
{
    [Test]
    public async Task Can_Retrieve_DotNet_Project()
    {
        var root = Sourcy.DotNet.Projects.Sourcy_DotNet;

        await Assert.That(root.Name).IsEqualTo("Sourcy.DotNet.csproj");
    }
    
    [Test]
    public async Task Can_Retrieve_Tests_Project()
    {
        var root = Sourcy.DotNet.Projects.Sourcy_Tests;

        await Assert.That(root.Name).IsEqualTo("Sourcy.Tests.csproj");
    }
    
    [Test]
    public async Task Can_Retrieve_Git_Project()
    {
        var root = Sourcy.DotNet.Projects.Sourcy_Git;

        await Assert.That(root.Name).IsEqualTo("Sourcy.Git.csproj");
    }
    
    [Test]
    public async Task Can_Retrieve_Solution()
    {
        var root = Sourcy.DotNet.Solutions.Sourcy;

        await Assert.That(root.Name).IsEqualTo("Sourcy.sln");
    }
}