using System;

namespace Domore.IO;

/// <summary>
/// Represents an exception that is thrown when the initialization of a <see cref="System.IO.FileSystemWatcher"/> fails.
/// </summary>
/// <remarks>
/// This exception typically occurs when there is an issue configuring or starting a <see
/// cref="System.IO.FileSystemWatcher"/>, such as invalid parameters or underlying system errors. The <see
/// cref="Exception.InnerException"/> property may provide  additional details about the root cause of the
/// failure.
/// </remarks>
/// <param name="message">The exception message.</param>
/// <param name="innerException">The inner exception that caused this exception.</param>
public sealed class FileSystemWatcherInitializationException(string message, Exception innerException) : Exception(message, innerException) {
}
