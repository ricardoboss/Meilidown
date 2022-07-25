﻿using System.Text.RegularExpressions;

namespace TRENZ.Docs.API.Models.Sources;

public abstract class AbstractFilesystemSource : ISource
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract SourceType Type { get; }

    /// <inheritdoc />
    public abstract string Root { get; }

    /// <inheritdoc />
    public abstract string Path { get; }

    /// <inheritdoc />
    public IEnumerable<SourceFile> FindFiles(Regex pattern)
    {
        var root = System.IO.Path.Combine(Root, Path);
        return IterateDirectory(pattern, root, root);
    }

    /// <inheritdoc />
    public abstract Task UpdateAsync(CancellationToken cancellationToken = default);

    private IEnumerable<SourceFile> IterateDirectory(Regex pattern, string path, string root)
    {
        foreach (var file in Directory.EnumerateFileSystemEntries(path, "**", new EnumerationOptions
                 {
                     AttributesToSkip = FileAttributes.System,
                     IgnoreInaccessible = true,
                     MatchCasing = MatchCasing.CaseInsensitive,
                     MatchType = MatchType.Win32,
                     RecurseSubdirectories = true,
                 }).Where(f => pattern.IsMatch(System.IO.Path.GetFileName(f))))
        {
            if (File.Exists(file))
            {
                yield return new(this, System.IO.Path.GetRelativePath(root, file));

                continue;
            }

            if (!Directory.Exists(file))
                continue;

            foreach (var f in IterateDirectory(pattern, file, root))
            {
                yield return f;
            }
        }
    }
}