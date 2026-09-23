using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Text.Builders;

/// <summary>
/// Splits decoded text into lines. A line ends at "\r\n" or "\n", or at the end of the text,
/// and is reported without its line ending. A "\r" that is not followed by "\n" is kept.
/// </summary>
public sealed class TextLineBuilder : DecodedTextBuilder {
    private readonly StringBuilder LineBuilder = new();

    private bool CarriageReturn;

    private void Line() {
        OnLine?.Invoke(LineBuilder.ToString());
        LineBuilder.Clear();
    }

    /// <inheritdoc/>
    protected sealed override Task Complete(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }
        if (CarriageReturn) {
            CarriageReturn = false;
            LineBuilder.Append('\r');
        }
        if (LineBuilder.Length > 0) {
            Line();
        }
        OnComplete?.Invoke();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected sealed override Task Add(ReadOnlyMemory<char> memory, CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }
        var span = memory.Span;
        var start = 0;
        var length = span.Length;
        for (var i = 0; i < length; i++) {
            var c = span[i];
            if (CarriageReturn) {
                CarriageReturn = false;
                if (c == '\n') {
                    Line();
                    start = i + 1;
                    continue;
                }
                LineBuilder.Append('\r');
            }
            if (c == '\r') {
                LineBuilder.Append(span[start..i]);
                start = i + 1;
                CarriageReturn = true;
                continue;
            }
            if (c == '\n') {
                LineBuilder.Append(span[start..i]);
                start = i + 1;
                Line();
            }
        }
        if (start < length) {
            LineBuilder.Append(span[start..length]);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected sealed override Task Clear(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }
        CarriageReturn = false;
        LineBuilder.Clear();
        OnClear?.Invoke();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets or sets the action called when the lines reported so far should be discarded,
    /// because the lines that follow come from another candidate encoding.
    /// </summary>
    public Action OnClear { get; set; }
    /// <summary>
    /// Gets or sets the action called after the last line, when the decode finishes.
    /// </summary>
    public Action OnComplete { get; set; }
    /// <summary>
    /// Gets or sets the action called with each line.
    /// </summary>
    public Action<string> OnLine { get; set; }

    /// <summary>
    /// Creates a builder that reports lines to the given actions.
    /// </summary>
    /// <param name="onLine">The action called with each line.</param>
    /// <param name="onClear">The action called when the lines reported so far should be discarded.</param>
    /// <param name="onComplete">The action called after the last line.</param>
    public TextLineBuilder(Action<string> onLine = null, Action onClear = null, Action onComplete = null) {
        OnLine = onLine;
        OnClear = onClear;
        OnComplete = onComplete;
    }
}
