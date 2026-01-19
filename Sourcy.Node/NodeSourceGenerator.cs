using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Sourcy.Node;

[Generator]
internal class NodeSourceGenerator : BaseSourcyGenerator
{
    protected override void Initialize(SourceProductionContext context, Root root)
    {
        try
        {
            // Use path-based comparison for Distinct() since DirectoryInfo compares by reference
            var npmProjects = root.EnumerateFiles()
                .Where(x => x.Name is "package-lock.json")
                .Where(x => !IsInNodeModules(x))
                .Select(x => x.Directory!)
                .DistinctBy(x => x.FullName, PathUtilities.PathComparer)
                .ToList();

            var yarnProjects = root.EnumerateFiles()
                .Where(x => x.Name is "yarn.lock")
                .Where(x => !IsInNodeModules(x))
                .Select(x => x.Directory!)
                .DistinctBy(x => x.FullName, PathUtilities.PathComparer)
                .ToList();

            var pnpmProjects = root.EnumerateFiles()
                .Where(x => x.Name is "pnpm-lock.yaml")
                .Where(x => !IsInNodeModules(x))
                .Select(x => x.Directory!)
                .DistinctBy(x => x.FullName, PathUtilities.PathComparer)
                .ToList();

            WriteProjects(context, npmProjects, "Sourcy.Node.Npm", "NpmProjectExtensions.g.cs");
            WriteProjects(context, yarnProjects, "Sourcy.Node.Yarn", "YarnProjectExtensions.g.cs");
            WriteProjects(context, pnpmProjects, "Sourcy.Node.Pnpm", "PnpmProjectExtensions.g.cs");
        }
        catch (Exception ex)
        {
            context.ReportGenerationError("Node generator", ex);
        }
    }

    private static bool IsInNodeModules(FileInfo fileInfo)
    {
        var parent = fileInfo.Directory;

        while (parent is not null)
        {
            // Case-insensitive comparison for cross-platform consistency
            if (string.Equals(parent.Name, "node_modules", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }

    private static void WriteProjects(
        SourceProductionContext context,
        List<DirectoryInfo> projects,
        string @namespace,
        string filename)
    {
        var sourceBuilder = new StringBuilder();
        var usedIdentifiers = new HashSet<string>();

        sourceBuilder.AppendLine($"namespace {@namespace};");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"Sourcy.Node\", \"1.0.0\")]");
        sourceBuilder.AppendLine("internal static class Projects");
        sourceBuilder.AppendLine("{");

        foreach (var projectDirectory in projects)
        {
            try
            {
                var formattedName = IdentifierHelper.ToValidIdentifier(
                    projectDirectory.Name,
                    usedIdentifiers);

                var escapedPath = PathUtilities.EscapeForVerbatimString(projectDirectory.FullName);
                sourceBuilder.AppendLine($"    public static global::System.IO.DirectoryInfo {formattedName} {{ get; }} = new global::System.IO.DirectoryInfo(@\"{escapedPath}\");");
            }
            catch (Exception ex)
            {
                context.ReportGenerationError(projectDirectory.Name, ex);
            }
        }

        sourceBuilder.AppendLine("}");

        context.AddSource(filename, GetSourceText(sourceBuilder.ToString()));
    }
}