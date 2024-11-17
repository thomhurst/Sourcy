using System;
using Microsoft.CodeAnalysis;

namespace Sourcy;

// Make it equal so we don't keep invoking the generator
public class CompilationWrapper(Compilation compilation) : IEquatable<CompilationWrapper>
{
    public Compilation Compilation { get; } = compilation;

    public bool Equals(CompilationWrapper? other)
    {
        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((CompilationWrapper)obj);
    }

    public override int GetHashCode()
    {
        return 1;
    }
}