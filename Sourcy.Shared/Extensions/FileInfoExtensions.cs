using System.IO;

namespace Sourcy.Extensions;

public static class FileInfoExtensions
{
    public static string NameWithoutExtension(this FileInfo fileInfo)
    {
        return Path.GetFileNameWithoutExtension(fileInfo.FullName);
    }
}