using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

/// <summary>
/// A source of bytes to decode as text.
/// </summary>
public interface IStreamText {
    /// <summary>
    /// Gets the length of the stream in bytes.
    /// </summary>
    long StreamLength { get; }

    /// <summary>
    /// Opens the stream to decode. The stream is disposed when decoding finishes.
    /// </summary>
    /// <returns>The stream, or null if there is nothing to decode.</returns>
    Stream StreamText();

    /// <summary>
    /// Waits until the stream can be opened. Called before <see cref="StreamText"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>
    /// A task whose result, if not null, is disposed when decoding finishes. A null task means
    /// the stream is not available, and nothing is decoded.
    /// </returns>
    Task<IDisposable> StreamReady(CancellationToken cancellationToken);
}
