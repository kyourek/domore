using System.Collections.Generic;

namespace Domore.Diagnostics;

/// <summary>
/// Provides access to the current and completed output lines of a process.
/// </summary>
public interface IProcessStream {
    /// <summary>
    /// Gets the output item currently being populated, or
    /// <see langword="null"/> when no line is in progress.
    /// </summary>
    /// <remarks>
    /// Change notifications for this property and for
    /// <see cref="IProcessStreamOutput.Line"/> are raised on the thread that
    /// reads the output, not on the synchronization context.
    /// </remarks>
    IProcessStreamOutput CurrentItem { get; }

    /// <summary>
    /// Gets the read-only observable collection of completed output lines.
    /// </summary>
    IList<IProcessStreamOutput> LineItems { get; }
}
