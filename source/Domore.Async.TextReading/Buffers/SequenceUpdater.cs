using System;
using System.Buffers;

namespace Domore.Buffers;

internal abstract class SequenceUpdater {
}

internal sealed class SequenceUpdater<T> : SequenceUpdater {
    private readonly object Locker = new();

    private bool _Completed;
    private ReadOnlySequence<T> _Sequence;

    public event SequenceUpdatedEventHandler<T> Updated;

    public bool Completed {
        get { lock (Locker) return _Completed; }
    }

    public ReadOnlySequence<T> Sequence {
        get { lock (Locker) return _Sequence; }
    }

    public void Update(SequenceSegment<T> startSegment, SequenceSegment<T> endSegment) {
        if (null == startSegment) throw new ArgumentNullException(nameof(startSegment));
        if (null == endSegment) throw new ArgumentNullException(nameof(endSegment));
        var sequence = new ReadOnlySequence<T>(startSegment,
                                               startSegment.StartIndex,
                                               endSegment,
                                               endSegment.EndIndex);
        lock (Locker) {
            _Sequence = sequence;
        }
        Updated?.Invoke(this, new(false, sequence));
    }

    public void Complete() {
        ReadOnlySequence<T> sequence;
        lock (Locker) {
            _Completed = true;
            sequence = _Sequence;
        }
        Updated?.Invoke(this, new(true, sequence));
    }
}
