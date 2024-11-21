using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Sourcy;

[EditorBrowsable(EditorBrowsableState.Never)]
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class EnableSourcyAttribute : Attribute
{
    // ReSharper disable once UnusedParameter.Local
    public EnableSourcyAttribute(string filePath)
    {
    }
}