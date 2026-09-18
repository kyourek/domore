using System;

using PATH = System.IO.Path;

namespace Domore.IO;

internal sealed class FileSystemEventsKey : IEquatable<FileSystemEventsKey> {
    private string CanonicalPath { get; }

    private static string CanonicalPathFactory(string path, bool caseSensitive) {
        var fullPath = PATH.GetFullPath(path) switch {
            var s when caseSensitive => s,
            var s => s.ToUpperInvariant(),
        };
        return fullPath.TrimEnd(PATH.DirectorySeparatorChar, PATH.AltDirectorySeparatorChar);
    }

    public bool CaseSensitive { get; }
    public string Path { get; }
    public FileSystemEventOptions Options { get; }

    public FileSystemEventsKey(string path, bool caseSensitive, FileSystemEventOptions options = null) {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Options = options ?? new();
        CaseSensitive = caseSensitive;
        CanonicalPath = CanonicalPathFactory(Path, CaseSensitive);
    }

    public bool Equals(FileSystemEventsKey other) {
        if (other is null) {
            return false;
        }
        if (CaseSensitive.Equals(other.CaseSensitive) != true) {
            return false;
        }
        if (CanonicalPath.Equals(other.CanonicalPath) != true) {
            return false;
        }
        if (Options.Equals(other.Options) != true) {
            return false;
        }
        return true;
    }

    public sealed override bool Equals(object obj) {
        return obj is FileSystemEventsKey other && Equals(other);
    }

    public sealed override int GetHashCode() {
        unchecked {
            var
            hash = 17;
            hash = hash * 23 + Options.GetHashCode();
            hash = hash * 23 + CaseSensitive.GetHashCode();
            hash = hash * 23 + CanonicalPath.GetHashCode();
            return hash;
        }
    }
}
