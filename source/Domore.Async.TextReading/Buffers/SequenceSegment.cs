using System;
using System.Buffers;

namespace Domore.Buffers;

internal sealed class SequenceSegment<T> : ReadOnlySequenceSegment<T> {
    private SequenceSegment() {
    }

    public int StartIndex => 0;
    public int EndIndex => Memory.Length;

    public SequenceSegment(ReadOnlyMemory<T> memory) {
        Memory = memory;
        RunningIndex = 0;
    }

    public SequenceSegment<T> Append(ReadOnlyMemory<T> memory) {
        var segment = new SequenceSegment<T> {
            Memory = memory,
            RunningIndex = RunningIndex + Memory.Length
        };
        Next = segment;
        return segment;
    }

    /// <summary>
    /// Replaces the memory of this segment with <paramref name="memory"/>, which must start with
    /// the current memory. Sequences that already end at this segment are unaffected, because
    /// they are bounded by the end index they were created with.
    /// </summary>
    public void Grow(ReadOnlyMemory<T> memory) {
        if (Next is not null) {
            throw new InvalidOperationException();
        }
        if (memory.Length < Memory.Length) {
            throw new ArgumentOutOfRangeException(nameof(memory));
        }
        Memory = memory;
    }
}
