using System;
using System.IO;

namespace Domore.IO;

/// <summary>
/// Represents configuration options for monitoring file-system events.
/// </summary>
/// <remarks>
/// This class provides a set of options to customize the behavior of file-system event monitoring, such
/// as filtering specific files, including subdirectories, and specifying notification filters.
/// </remarks>
public sealed class FileSystemEventOptions : IEquatable<FileSystemEventOptions> {
    /// <summary>
    /// Gets or sets the string used to filter file-system paths.
    /// </summary>
    public string FileFilter { get; init; }

    /// <summary>
    /// Gets or sets a flag indicating whether or not subdirectories should be included.
    /// </summary>
    public bool IncludeSubdirectories { get; init; }

    /// <summary>
    /// Gets or sets the internal buffer size.
    /// </summary>
    public int? InternalBufferSize { get; init; }

    /// <summary>
    /// Gets or sets the flags used to filter file-system events.
    /// </summary>
    public NotifyFilters? NotifyFilter { get; init; }

    /// <summary>
    /// Determines whether the specified <see cref="FileSystemEventOptions"/> instance is equal to the current instance.
    /// </summary>
    /// <param name="other">
    /// The <see cref="FileSystemEventOptions"/> instance to compare with the current instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the specified <see cref="FileSystemEventOptions"/> instance is equal to the current
    /// instance; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(FileSystemEventOptions other) {
        return other is not null &&
               other.FileFilter == FileFilter &&
               other.IncludeSubdirectories == IncludeSubdirectories &&
               other.InternalBufferSize == InternalBufferSize &&
               other.NotifyFilter == NotifyFilter;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the specified object is of type <see cref="FileSystemEventOptions"/>  and is equal to
    /// the current instance; otherwise, <see langword="false"/>.
    /// </returns>
    public sealed override bool Equals(object obj) {
        return obj is FileSystemEventOptions other &&
               other.Equals(this);
    }

    /// <summary>
    /// Computes a hash code for the current object based on its properties.
    /// </summary>
    /// <returns>
    /// An integer representing the hash code for the current object.
    /// </returns>
    public sealed override int GetHashCode() {
        unchecked {
            var
            hash = 17;
            hash = hash * 23 + IncludeSubdirectories.GetHashCode();
            hash = hash * 23 + (FileFilter?.GetHashCode() ?? 0);
            hash = hash * 23 + (NotifyFilter?.GetHashCode() ?? 0);
            hash = hash * 23 + (InternalBufferSize?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
