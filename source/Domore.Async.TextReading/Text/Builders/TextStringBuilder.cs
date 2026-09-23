using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Text.Builders;

/// <summary>
/// Collects decoded text into a string.
/// </summary>
public sealed class TextStringBuilder : DecodedTextBuilder {
    private string StringComplete;
    private readonly StringBuilder StringBuilder = new();

    /// <inheritdoc/>
    protected sealed override Task Complete(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }
        StringComplete = StringBuilder.ToString();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected sealed override Task Add(ReadOnlyMemory<char> memory, CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }
        StringBuilder.Append(memory);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected sealed override Task Clear(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }
        StringBuilder.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the text built so far, or all of it once the decode has finished.
    /// </summary>
    public sealed override string ToString() {
        return StringComplete ?? StringBuilder.ToString();
    }
}
