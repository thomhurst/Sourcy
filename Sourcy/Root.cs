#pragma warning disable RS1035

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Sourcy;

[DebuggerDisplay("{Directory,nq}")]
public record Root(DirectoryInfo Directory)
{
    public IEnumerable<FileInfo> EnumerateFiles()
    {
        return SafeWalk.EnumerateFiles(Directory);
    }
    
    public IEnumerable<DirectoryInfo> EnumerateDirectories()
    {
        return SafeWalk.EnumerateDirectories(Directory);
    }
}