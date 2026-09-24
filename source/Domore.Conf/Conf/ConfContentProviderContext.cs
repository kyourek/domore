using System;
using System.Collections.Generic;
using System.IO;

namespace Domore.Conf;

internal sealed class ConfContentProviderContext {
    private static readonly StringComparer PathComparer =
        Path.DirectorySeparatorChar == '\\'
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private readonly List<string> ActiveFilePaths = [];

    internal string ResolvePath(string path) {
        if (Path.IsPathRooted(path)) {
            return path;
        }
        var baseDirectory = ActiveFilePaths.Count > 0
            ? Path.GetDirectoryName(ActiveFilePaths[ActiveFilePaths.Count - 1])
            : BaseDirectory;
        return string.IsNullOrEmpty(baseDirectory)
            ? path
            : Path.Combine(baseDirectory, path);
    }

    internal string EnterFile(string path) {
        var fullPath = Path.GetFullPath(path);
        foreach (var activePath in ActiveFilePaths) {
            if (PathComparer.Equals(activePath, fullPath)) {
                throw new ConfException($"A circular conf file reference was detected: '{fullPath}'.", null);
            }
        }
        ActiveFilePaths.Add(fullPath);
        return fullPath;
    }

    internal void ExitFile() {
        ActiveFilePaths.RemoveAt(ActiveFilePaths.Count - 1);
    }

    public string Special { get; set; }
    public string BaseDirectory { get; set; }
    public bool IncludeEmptyValues { get; set; }
}
