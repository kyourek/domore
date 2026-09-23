using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Domore.Text.Builders;

/// <summary>
/// Exposes decoded text as an asynchronous stream of <see cref="TextStreamItem"/>s.
/// </summary>
public sealed class TextStreamBuilder : DecodedTextBuilder {
    private readonly object ReadLocker = new();
    private readonly ChannelReader<TextStreamItem> Reader;
    private readonly ChannelWriter<TextStreamItem> Writer;

    private bool Reading;

    /// <inheritdoc/>
    protected sealed override async Task Add(ReadOnlyMemory<char> memory, CancellationToken cancellationToken) {
        var s = new string(memory.Span);
        var i = new TextStreamItem(s);
        await Writer.WriteAsync(i, cancellationToken);
    }

    /// <inheritdoc/>
    protected sealed override async Task Clear(CancellationToken cancellationToken) {
        var s = default(string);
        var i = new TextStreamItem(s);
        await Writer.WriteAsync(i, cancellationToken);
    }

    /// <inheritdoc/>
    protected sealed override Task Complete(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }
        Writer.Complete();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected sealed override Task Fail(Exception exception, CancellationToken cancellationToken) {
        Writer.TryComplete(exception);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a builder whose text can be read with <see cref="Read(CancellationToken)"/>.
    /// </summary>
    public TextStreamBuilder() {
        var channel = Channel.CreateUnbounded<TextStreamItem>(new() { SingleWriter = true, SingleReader = true });
        Reader = channel.Reader;
        Writer = channel.Writer;
    }

    /// <summary>
    /// Reads the decoded text as it becomes available. The enumeration ends when the decode
    /// finishes, and throws the exception of the decode if it fails or is canceled.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel reading.</param>
    /// <returns>The items of decoded text.</returns>
    /// <exception cref="InvalidOperationException">The text has already been read.</exception>
    public IAsyncEnumerable<TextStreamItem> Read(CancellationToken cancellationToken) {
        lock (ReadLocker) {
            if (Reading == false) {
                Reading = true;
            }
            else {
                throw new InvalidOperationException();
            }
        }
        return Reader.ReadAllAsync(cancellationToken);
    }
}
