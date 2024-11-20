using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Sourcy;

internal struct CompilationWrapper(Compilation compilation)
{
    public Compilation Compilation { get; } = compilation;

    public override bool Equals(object? obj)
    {
        return true;
    }

    public override int GetHashCode()
    {
        return 1;
    }
}