namespace Sourcy.Tests;

public class SafeWalkTests
{
    private static readonly Lazy<System.Reflection.Assembly> GeneratorAssembly = new(LoadGeneratorAssembly);

    [Test]
    public async Task EnumerateFiles_Includes_Files_In_Hidden_Directories()
    {
        var testRoot = CreateTestDirectory();

        try
        {
            var hiddenDirectory = Directory.CreateDirectory(Path.Combine(testRoot, "hidden"));
            hiddenDirectory.Attributes |= FileAttributes.Hidden;

            var projectPath = Path.Combine(hiddenDirectory.FullName, "Hidden.csproj");
            await File.WriteAllTextAsync(projectPath, string.Empty);

            var files = EnumerateFiles(new DirectoryInfo(testRoot));

            await Assert.That(files.Any(x => x.FullName == projectPath)).IsTrue();
        }
        finally
        {
            ResetAttributesAndDelete(testRoot);
        }
    }

    [Test]
    public async Task EnumerateFiles_Does_Not_Treat_NonCyclic_Directory_Links_As_Cycles()
    {
        var testRoot = CreateTestDirectory();

        try
        {
            var rootDirectory = Directory.CreateDirectory(Path.Combine(testRoot, "root"));
            var targetDirectory = Directory.CreateDirectory(Path.Combine(testRoot, "target"));
            var projectPath = Path.Combine(targetDirectory.FullName, "Linked.csproj");
            await File.WriteAllTextAsync(projectPath, string.Empty);

            var linkPath = Path.Combine(rootDirectory.FullName, "linked");
            if (!TryCreateDirectoryLink(linkPath, targetDirectory.FullName))
            {
                return;
            }

            var files = EnumerateFiles(rootDirectory);

            await Assert.That(files.Any(x => x.Name == "Linked.csproj")).IsTrue();
        }
        finally
        {
            ResetAttributesAndDelete(testRoot);
        }
    }

    private static List<FileInfo> EnumerateFiles(DirectoryInfo directory)
    {
        var safeWalkType = GeneratorAssembly.Value.GetType("Sourcy.SafeWalk", throwOnError: true)!;
        var enumerateFiles = safeWalkType.GetMethod(
            "EnumerateFiles",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;

        var result = enumerateFiles.Invoke(null, new object?[] { directory, null });
        return ((IEnumerable<FileInfo>) result!).ToList();
    }

    private static System.Reflection.Assembly LoadGeneratorAssembly()
    {
        var repositoryRoot = FindRepositoryRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        var expectedPath = Path.Combine(
            repositoryRoot,
            "Sourcy.DotNet",
            "bin",
            configuration,
            "netstandard2.0",
            "Sourcy.DotNet.dll");

        if (File.Exists(expectedPath))
        {
            return System.Reflection.Assembly.LoadFrom(expectedPath);
        }

        var binPath = Path.Combine(repositoryRoot, "Sourcy.DotNet", "bin");
        var fallbackPath = Directory.Exists(binPath)
            ? Directory.GetFiles(binPath, "Sourcy.DotNet.dll", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        if (fallbackPath is null)
        {
            throw new FileNotFoundException("Could not locate built Sourcy.DotNet.dll for SafeWalk tests.", expectedPath);
        }

        return System.Reflection.Assembly.LoadFrom(fallbackPath);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sourcy.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }

    private static string CreateTestDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Sourcy.SafeWalk.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static bool TryCreateDirectoryLink(string linkPath, string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static void ResetAttributesAndDelete(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(directory, FileAttributes.Normal);
            }
            catch
            {
                // Best effort cleanup for hidden directories.
            }
        }

        Directory.Delete(path, recursive: true);
    }
}
