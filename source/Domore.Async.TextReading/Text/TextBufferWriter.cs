using Domore.Buffers;
using System;
using System.Buffers;

namespace Domore.Text;

internal sealed class TextBufferWriter : IBufferWriter<char> {
    private readonly SequenceUpdater<char> TextSequence = new();

    private int Cursor;
    private char[] Buffer;
    private char[] SegmentBuffer;
    private int SegmentStart;
    private SequenceSegment<char> StartSegment;
    private SequenceSegment<char> EndSegment;

    private char[] NewBuffer(int sizeHint) {
        Cursor = 0;
        Buffer = BufferPool.Rent(sizeHint);
        return Buffer;
    }

    private char[] BufferFor(int sizeHint) {
        if (sizeHint < 0) {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }
        if (sizeHint == 0) {
            sizeHint = 1;
        }
        var buffer = Buffer;
        if (buffer != null && buffer.Length - Cursor >= sizeHint) {
            return buffer;
        }
        return NewBuffer(sizeHint);
    }

    public long Written { get; private set; }
    public BufferPool<char> BufferPool { get; }

    public ReadOnlySequence<char> Sequence =>
        TextSequence.Sequence;

    public TextBufferWriter(BufferPool<char> bufferPool) {
        BufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
    }

    public void Complete() {
        TextSequence.Complete();
    }

    void IBufferWriter<char>.Advance(int count) {
        if (count < 0 || count > (Buffer?.Length ?? 0) - Cursor) {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        if (StartSegment == null) {
            StartSegment = EndSegment = new SequenceSegment<char>(Buffer.AsMemory(Cursor, count));
            SegmentBuffer = Buffer;
            SegmentStart = Cursor;
        }
        else if (ReferenceEquals(SegmentBuffer, Buffer)) {
            EndSegment.Grow(Buffer.AsMemory(SegmentStart, Cursor + count - SegmentStart));
        }
        else {
            EndSegment = EndSegment.Append(Buffer.AsMemory(Cursor, count));
            SegmentBuffer = Buffer;
            SegmentStart = Cursor;
        }
        Cursor += count;
        Written += count;
        TextSequence.Update(StartSegment, EndSegment);
    }

    Memory<char> IBufferWriter<char>.GetMemory(int sizeHint) {
        var buffer = BufferFor(sizeHint);
        return buffer.AsMemory(Cursor, buffer.Length - Cursor);
    }

    Span<char> IBufferWriter<char>.GetSpan(int sizeHint) {
        var buffer = BufferFor(sizeHint);
        return buffer.AsSpan(Cursor, buffer.Length - Cursor);
    }
}
