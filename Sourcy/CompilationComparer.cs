using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Sourcy;

public struct CompilationComparer : IEqualityComparer<Compilation>
{
    public bool Equals(Compilation x, Compilation y)
    {
        return true;
    }

    public int GetHashCode(Compilation obj)
    {
        return 1;
    }
}