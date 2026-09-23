using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Text;

/// <summary>
/// Receives decoded text as it becomes available.
/// </summary>
/// <remarks>
/// While several candidate encodings are decoding, progress may come from any of them. When
/// it switches to another candidate, <see cref="Clear(CancellationToken)"/> is called and the
/// text of that candidate is added again from the start.
/// </remarks>
public abstract class DecodedTextBuilder {
    private long Cursor;
    private object State;

    private async Task Sequence(object state, ReadOnlySequence<char> sequence, CancellationToken cancellationToken) {
        if (State != state) {
            State = state;
            Cursor = 0;
            await Clear(cancellationToken).ConfigureAwait(false);
        }
        if (sequence.Length > Cursor) {
            var slice = sequence.Slice(Cursor);
            var length = slice.Length;
            if (length > 0) {
                foreach (var memory in slice) {
                    await Add(memory, cancellationToken).ConfigureAwait(false);
                }
                Cursor += length;
            }
        }
    }

    internal async Task Decode(DecodedText decoded, CancellationToken cancellationToken) {
        if (null == decoded) throw new ArgumentNullException(nameof(decoded));
        await Task
            .Run(
                cancellationToken: cancellationToken,
                function: () => Sequence(decoded.State, decoded.Sequence, cancellationToken))
            .ConfigureAwait(false);
    }

    internal Task Complete(DecodedText _, CancellationToken cancellationToken) {
        return Complete(cancellationToken) ?? Task.CompletedTask;
    }

    internal Task Failed(Exception exception) {
        return Fail(exception, CancellationToken.None) ?? Task.CompletedTask;
    }

    internal static DecodedTextBuilder Combine(IEnumerable<DecodedTextBuilder> builders) {
        return new Aggregate(builders);
    }

    /// <summary>
    /// Discards all text added so far, because the text that follows comes from another
    /// candidate encoding.
    /// </summary>
    /// <param name="cancellationToken">The token passed to the decode.</param>
    protected abstract Task Clear(CancellationToken cancellationToken);

    /// <summary>
    /// Adds the next piece of decoded text.
    /// </summary>
    /// <param name="memory">
    /// The text that follows the text added so far. It is valid only until the returned task
    /// completes.
    /// </param>
    /// <param name="cancellationToken">The token passed to the decode.</param>
    protected abstract Task Add(ReadOnlyMemory<char> memory, CancellationToken cancellationToken);

    /// <summary>
    /// Called when decoding finishes without throwing, including when no candidate encoding
    /// succeeded.
    /// </summary>
    /// <param name="cancellationToken">The token passed to the decode.</param>
    protected virtual Task Complete(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called instead of <see cref="Complete(CancellationToken)"/> when decoding throws,
    /// including when it is canceled.
    /// </summary>
    /// <param name="exception">The exception thrown by the decode.</param>
    /// <param name="cancellationToken">A token that is never canceled.</param>
    protected virtual Task Fail(Exception exception, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    private sealed class Aggregate : DecodedTextBuilder {
        private Task All(Func<DecodedTextBuilder, CancellationToken, Task> task, CancellationToken cancellationToken) {
            if (null == task) throw new ArgumentNullException(nameof(task));
            return Task.WhenAll(List.Select(item => task(item, cancellationToken)));
        }

        protected sealed override Task Clear(CancellationToken cancellationToken) {
            return All((i, ct) => i.Clear(ct), cancellationToken);
        }

        protected sealed override Task Add(ReadOnlyMemory<char> memory, CancellationToken cancellationToken) {
            return All((i, ct) => i.Add(memory, ct), cancellationToken);
        }

        protected sealed override Task Complete(CancellationToken cancellationToken) {
            return All((i, ct) => i.Complete(ct), cancellationToken);
        }

        protected sealed override Task Fail(Exception exception, CancellationToken cancellationToken) {
            return All((i, ct) => i.Fail(exception, ct), cancellationToken);
        }

        public IReadOnlyList<DecodedTextBuilder> List { get; }

        public Aggregate(IEnumerable<DecodedTextBuilder> items) {
            if (null == items) throw new ArgumentNullException(nameof(items));
            List = items.Where(item => item != null).ToList();
        }
    }
}
