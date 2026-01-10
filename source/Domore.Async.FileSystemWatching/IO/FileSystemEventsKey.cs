using System;

namespace Domore.IO;

internal sealed class FileSystemEventsKey : IEquatable<FileSystemEventsKey> {
    public string Path { get; }
    public FileSystemEventOptions Options { get; }

    public FileSystemEventsKey(string path, FileSystemEventOptions options = null) {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Options = options ?? new();
    }

    public bool Equals(FileSystemEventsKey other) {
        return other is not null &&
               other.Path.Equals(Path) &&
               other.Options.Equals(Options);
    }

    public sealed override bool Equals(object obj) {
        return obj is FileSystemEventsKey other &&
               other.Equals(this);
    }

    public sealed override int GetHashCode() {
        unchecked {
            var
            hash = 17;
            hash = hash * 23 + Path.GetHashCode();
            hash = hash * 23 + Options.GetHashCode();
            return hash;
        }
    }
}
