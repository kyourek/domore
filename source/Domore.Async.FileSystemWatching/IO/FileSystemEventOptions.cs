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
public sealed record FileSystemEventOptions : IEquatable<FileSystemEventOptions> {
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
}
