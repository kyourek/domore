namespace Domore.Diagnostics;

/// <summary>
/// Identifies the process output stream that produced a line.
/// </summary>
public enum ProcessOutputKind {
    /// <summary>
    /// The process's standard error stream.
    /// </summary>
    StandardError,

    /// <summary>
    /// The process's standard output stream.
    /// </summary>
    StandardOutput,
}
