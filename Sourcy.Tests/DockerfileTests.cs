using System.Threading.Tasks;

namespace Sourcy.Tests;

public class DockerfileTests
{
    [Test]
    public async Task Can_Retrieve_Dockerfile()
    {
        var root = Sourcy.Docker.Dockerfiles.MongoDB;

        await Assert.That(root.Name).IsEqualTo("Sourcy.DotNet.csproj");
    }
}