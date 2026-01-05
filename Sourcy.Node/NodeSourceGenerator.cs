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

            WriteNpmProjects(context, npmProjects);
            WriteYarnProjects(context, yarnProjects);
            WritePnpmProjects(context, pnpmProjects);
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

    private static void WriteNpmProjects(SourceProductionContext context, List<DirectoryInfo> projects)
    {
        var sourceBuilder = new StringBuilder();
        var usedIdentifiers = new HashSet<string>();

        sourceBuilder.AppendLine("namespace Sourcy.Node.Npm;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("internal static class Projects");
        sourceBuilder.AppendLine("{");

        foreach (var projectDirectory in projects)
        {
            try
            {
                var formattedName = IdentifierHelper.ToValidIdentifier(
                    projectDirectory.Name,
                    usedIdentifiers);

                var escapedPath = PathEscaper.EscapeForVerbatimString(projectDirectory.FullName);
                sourceBuilder.AppendLine($"\tpublic static global::System.IO.DirectoryInfo {formattedName} {{ get; }} = new global::System.IO.DirectoryInfo(@\"{escapedPath}\");");
            }
            catch (Exception ex)
            {
                context.ReportGenerationError(projectDirectory.Name, ex);
            }
        }

        sourceBuilder.AppendLine("}");

        context.AddSource("NpmProjectExtensions.g.cs", GetSourceText(sourceBuilder.ToString()));
    }

    private static void WriteYarnProjects(SourceProductionContext context, List<DirectoryInfo> projects)
    {
        var sourceBuilder = new StringBuilder();
        var usedIdentifiers = new HashSet<string>();

        sourceBuilder.AppendLine("namespace Sourcy.Node.Yarn;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("internal static class Projects");
        sourceBuilder.AppendLine("{");

        foreach (var projectDirectory in projects)
        {
            try
            {
                var formattedName = IdentifierHelper.ToValidIdentifier(
                    projectDirectory.Name,
                    usedIdentifiers);

                var escapedPath = PathEscaper.EscapeForVerbatimString(projectDirectory.FullName);
                sourceBuilder.AppendLine($"\tpublic static global::System.IO.DirectoryInfo {formattedName} {{ get; }} = new global::System.IO.DirectoryInfo(@\"{escapedPath}\");");
            }
            catch (Exception ex)
            {
                context.ReportGenerationError(projectDirectory.Name, ex);
            }
        }

        sourceBuilder.AppendLine("}");

        context.AddSource("YarnProjectExtensions.g.cs", GetSourceText(sourceBuilder.ToString()));
    }

    private static void WritePnpmProjects(SourceProductionContext context, List<DirectoryInfo> projects)
    {
        var sourceBuilder = new StringBuilder();
        var usedIdentifiers = new HashSet<string>();

        sourceBuilder.AppendLine("namespace Sourcy.Node.Pnpm;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("internal static class Projects");
        sourceBuilder.AppendLine("{");

        foreach (var projectDirectory in projects)
        {
            try
            {
                var formattedName = IdentifierHelper.ToValidIdentifier(
                    projectDirectory.Name,
                    usedIdentifiers);

                var escapedPath = PathEscaper.EscapeForVerbatimString(projectDirectory.FullName);
                sourceBuilder.AppendLine($"\tpublic static global::System.IO.DirectoryInfo {formattedName} {{ get; }} = new global::System.IO.DirectoryInfo(@\"{escapedPath}\");");
            }
            catch (Exception ex)
            {
                context.ReportGenerationError(projectDirectory.Name, ex);
            }
        }

        sourceBuilder.AppendLine("}");

        context.AddSource("PnpmProjectExtensions.g.cs", GetSourceText(sourceBuilder.ToString()));
    }
}