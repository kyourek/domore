using System;

namespace Domore.IO;

/// <summary>
/// Represents the outcome of an operation performed for a single <see cref="FileSystemEventSubscription"/>,
/// such as handling a file-system event or adding and removing the subscription.
/// </summary>
/// <remarks>
/// A result is considered successful when <see cref="Canceled"/> is <see langword="false"/> and
/// <see cref="Exception"/> is <see langword="null"/>.
/// </remarks>
public sealed record FileSystemEventResult {
    /// <summary>
    /// Gets a flag indicating whether or not the operation was canceled before it completed.
    /// </summary>
    public bool Canceled { get; init; }

    /// <summary>
    /// Gets the exception that caused the operation to fail, or <see langword="null"/> if the operation did not fail.
    /// </summary>
    /// <remarks>
    /// When <see cref="Canceled"/> is <see langword="true"/>, this property holds the
    /// <see cref="OperationCanceledException"/> that was observed.
    /// </remarks>
    public Exception Exception { get; init; }

    /// <summary>
    /// Gets the subscription for which the operation was performed.
    /// </summary>
    public FileSystemEventSubscription Subscription { get; init; }
}
