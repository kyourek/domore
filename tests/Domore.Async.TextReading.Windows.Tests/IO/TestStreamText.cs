using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

internal sealed class TestStreamText : IStreamText {
    private int StreamLengthCallCount;
    private int StreamTextCallCount;
    private readonly TaskCompletionSource ReadyCanceledSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource ReadyStartedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public byte[] Bytes { get; }
    public long? Length { get; init; }
    public bool LengthTaskIsNull { get; init; }
    public Exception LengthException { get; init; }
    public Exception StreamException { get; init; }
    public bool WaitUntilCanceled { get; init; }

    public int StreamLengthCalls => Volatile.Read(ref StreamLengthCallCount);
    public int StreamTextCalls => Volatile.Read(ref StreamTextCallCount);
    public Task ReadyCanceled => ReadyCanceledSource.Task;
    public Task ReadyStarted => ReadyStartedSource.Task;

    public TestStreamText(byte[] bytes) {
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }

    public TestStreamText(string text) : this(Encoding.UTF8.GetBytes(text)) {
    }

    public Task<long> StreamLength(CancellationToken cancellationToken) {
        Interlocked.Increment(ref StreamLengthCallCount);
        if (LengthTaskIsNull) {
            return null;
        }
        if (LengthException is not null) {
            return Task.FromException<long>(LengthException);
        }
        return Task.FromResult(Length ?? Bytes.Length);
    }

    public async Task<IDisposable> StreamReady(CancellationToken cancellationToken) {
        ReadyStartedSource.TrySetResult();
        if (WaitUntilCanceled) {
            try {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            finally {
                ReadyCanceledSource.TrySetResult();
            }
        }
        return null;
    }

    public Stream StreamText() {
        Interlocked.Increment(ref StreamTextCallCount);
        if (StreamException is not null) {
            throw StreamException;
        }
        return new MemoryStream(Bytes);
    }
}
