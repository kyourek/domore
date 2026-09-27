namespace Domore.Diagnostics;

/// <summary>
/// Describes a line read from a process's standard output or standard error.
/// </summary>
public interface IProcessStreamOutput {
    /// <summary>
    /// Gets the text accumulated for this output line.
    /// </summary>
    string Line { get; }

    /// <summary>
    /// Gets the stream from which this line was read.
    /// </summary>
    ProcessOutputKind Kind { get; }
}
