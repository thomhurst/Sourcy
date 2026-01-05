using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Sourcy.Docker;

[Generator]
internal class DockerSourceGenerator : BaseSourcyGenerator
{
    protected override void Initialize(SourceProductionContext context, Root root)
    {
        try
        {
            var dockerfiles = root.EnumerateFiles()
                .Where(x => x.Name is "Dockerfile")
                .ToList();

            WriteDockerfiles(context, dockerfiles);
        }
        catch (Exception ex)
        {
            context.ReportGenerationError("Docker generator", ex);
        }
    }

    private static void WriteDockerfiles(SourceProductionContext context, List<FileInfo> dockerfiles)
    {
        var sourceBuilder = new StringBuilder();
        var usedIdentifiers = new HashSet<string>();

        sourceBuilder.AppendLine("namespace Sourcy.Docker;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("internal static class Dockerfiles");
        sourceBuilder.AppendLine("{");

        foreach (var dockerfile in dockerfiles)
        {
            try
            {
                var formattedName = IdentifierHelper.ToValidIdentifier(
                    dockerfile.Directory!.Name,
                    usedIdentifiers);

                var escapedPath = PathEscaper.EscapeForVerbatimString(dockerfile.FullName);
                sourceBuilder.AppendLine($"\tpublic static global::System.IO.FileInfo {formattedName} {{ get; }} = new global::System.IO.FileInfo(@\"{escapedPath}\");");
            }
            catch (Exception ex)
            {
                context.ReportGenerationError(dockerfile.Name, ex);
            }
        }

        sourceBuilder.AppendLine("}");

        context.AddSource("DockerFileExtensions.g.cs", GetSourceText(sourceBuilder.ToString()));
    }
}