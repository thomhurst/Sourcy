using Microsoft.CodeAnalysis;

namespace Sourcy;

internal readonly struct CompilationWrapper(Compilation compilation)
{
    public Compilation Compilation { get; } = compilation;

    public override bool Equals(object? obj)
    {
        return obj is Compilation;
    }

    public override int GetHashCode()
    {
        return compilation.GetHashCode();
    }
}