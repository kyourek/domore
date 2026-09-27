using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Domore;

internal sealed class PumpingSynchronizationContext : SynchronizationContext, IDisposable {
    private sealed class PostedCallback {
        public SendOrPostCallback Callback { get; }
        public object State { get; }

        public PostedCallback(SendOrPostCallback callback, object state) {
            Callback = callback;
            State = state;
        }
    }

    private readonly BlockingCollection<PostedCallback> Callbacks = new();
    private int Posts;

    public int OwnerThread { get; } = Thread.CurrentThread.ManagedThreadId;
    public int PostCount => Interlocked.CompareExchange(ref Posts, 0, 0);

    public override void Post(SendOrPostCallback callback, object state) {
        Interlocked.Increment(ref Posts);
        Callbacks.Add(new PostedCallback(callback, state));
    }

    public void PumpUntil(Task task, TimeSpan timeout) {
        var stopwatch = Stopwatch.StartNew();
        while (task.IsCompleted == false) {
            var remaining = timeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero) {
                throw new TimeoutException("The operation did not complete in time.");
            }
            var wait = remaining < TimeSpan.FromMilliseconds(50)
                ? remaining
                : TimeSpan.FromMilliseconds(50);
            if (Callbacks.TryTake(out var callback, wait) == false) {
                continue;
            }
            var previousContext = Current;
            SetSynchronizationContext(this);
            try {
                callback.Callback(callback.State);
            }
            finally {
                SetSynchronizationContext(previousContext);
            }
        }
        task.GetAwaiter().GetResult();
    }

    public void Dispose() {
        Callbacks.Dispose();
    }
}
