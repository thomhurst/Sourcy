using System.IO;

namespace Sourcy;

public record SourceGeneratedPath
{
    public required FileInfo File { get; init; }
    public required string Name { get; init; }
} 