# Sourcy

Sourcy is a Source Generator that gives you static compile-time properties to file paths within your repository.

This can be useful for things like tests, scripts or pipelines where you may need to locate something from the file system.

This can avoid having to deal with relative paths, or defining absolute paths which may change machine-to-machine.

## Getting Started
After installing the Sourcy package(s) you want, properties can be found within the `Sourcy` namespace. So just start typing `Sourcy.` and your IDE should help you with the rest.

Sourcy searches from the root of your repository or project. It'll traverse upwards until it finds a folder either containing a `.git` folder, or a `.sourcyroot` file. That file doesn't need to have any contents, just simply exist. If it can't find these, it will not run.

Currently Sourcy has 4 packages:

## Sourcy.Git
Attempts to find your Git directory by parsing the result of `git rev-parse --show-toplevel`

## Sourcy.DotNet
Searches for Projects with a `.csproj` or `.fsproj` extension

Searches for Solutions with a `.sln`, `.slnx` or `.slnf` extension

## Sourcy.Node
Locates Node projects within your repository by looking for a `package-lock.json` or `yarn.lock` file

## Sourcy.Docker
Locates `Dockerfile`s within your repository

For files you are given a `FileInfo` object, and for directories a `DirectoryInfo` object. This allows you to easy call things like `EnumerateFiles` on.

e.g.
```csharp
var testResultsReport = Sourcy.Git.RootDirectory
            .EnumerateFiles("*.trx", SearchOption.AllDirectories)
            .First();
```
