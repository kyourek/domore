using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Domore.Threading;

internal sealed class BackgroundQueue : IDisposable {
    private const int MaxWaitMilliseconds = int.MaxValue;
    private static readonly TimeSpan InfiniteTimeout = TimeSpan.FromMilliseconds(-1);

    private volatile Thread Thread;
    private readonly object ThreadLocker = new();
    private readonly BlockingCollection<Action> Collection = new();
    private readonly Action<Exception> ErrorReporter;

    private bool AddingCompleted;
    private bool Disposed;

    public BackgroundQueue(Action<Exception> errorReporter = null) {
        ErrorReporter = errorReporter;
    }

    public bool ThreadIsCurrentThread =>
        Thread == Thread.CurrentThread;

    private static bool ValidTimeout(TimeSpan timeout) =>
        timeout >= TimeSpan.Zero || timeout == InfiniteTimeout;

    private void Report(Exception ex) {
        if (ErrorReporter is not null) {
            try {
                ErrorReporter(ex);
                return;
            }
            catch {
                // Use the contained stderr fallback if the supplied reporter fails.
            }
        }
        try {
            Console.Error.WriteLine(ex);
        }
        catch {
            // A failing diagnostic writer must not terminate the queue worker.
        }
    }

    private void ThreadStart() {
        for (; ; ) {
            Action action;
            try {
                action = Collection.Take();
            }
            catch (ObjectDisposedException) {
                // Disposal follows a full drain. This is a final containment boundary.
                break;
            }
            catch (ThreadInterruptedException ex) {
                // An interrupt is transient. Keep the worker available to drain any
                // accepted items after the interrupted wait.
                Report(ex);
                continue;
            }
            catch (InvalidOperationException) {
                // CompleteAdding has been called and the accepted queue is empty.
                break;
            }
            catch (Exception ex) {
                Report(ex);
                break;
            }

            if (action is not null) {
                try {
                    action();
                }
                catch (Exception ex) {
                    Report(ex);
                }
            }
        }
    }

    private void StartThreadUnsafe() {
        if (Thread is not null || Collection.Count == 0) {
            return;
        }

        var thread = new Thread(ThreadStart) {
            Name = GetType().Name,
            IsBackground = true
        };
        Thread = thread;
        try {
            thread.Start();
        }
        catch {
            if (thread.IsAlive == false) {
                Thread = null;
            }
            throw;
        }
    }

    private static bool Join(Thread thread, TimeSpan? timeout) {
        if (thread is null) {
            return true;
        }
        if (thread == Thread.CurrentThread) {
            return false;
        }
        if (thread.IsAlive == false) {
            return true;
        }
        if (timeout.HasValue == false || timeout.Value == InfiniteTimeout) {
            for (; ; ) {
                try {
                    thread.Join();
                    return true;
                }
                catch (ThreadInterruptedException) {
                    if (thread.IsAlive == false) {
                        return true;
                    }
                }
            }
        }

        var duration = timeout.Value;
        var stopwatch = Stopwatch.StartNew();
        for (; ; ) {
            var remaining = duration - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero) {
                return thread.IsAlive == false;
            }

            var milliseconds = remaining.TotalMilliseconds >= MaxWaitMilliseconds
                ? MaxWaitMilliseconds
                : Math.Max(1, (int)Math.Ceiling(remaining.TotalMilliseconds));
            try {
                if (thread.Join(milliseconds)) {
                    return true;
                }
            }
            catch (ThreadInterruptedException) {
                if (thread.IsAlive == false) {
                    return true;
                }
            }
            if (stopwatch.Elapsed >= duration) {
                return false;
            }
        }
    }

    private bool Complete(TimeSpan? timeout) {
        Thread thread;
        lock (ThreadLocker) {
            if (Disposed) {
                return true;
            }
            if (AddingCompleted == false) {
                AddingCompleted = true;
                Collection.CompleteAdding();
            }

            // Add publishes to the collection before starting the worker. Completion
            // can win that small window, so it takes ownership of startup when an
            // accepted item is already waiting.
            StartThreadUnsafe();
            thread = Thread;
        }

        return Join(thread, timeout);
    }

    private void DisposeCollection() {
        lock (ThreadLocker) {
            if (Disposed || Thread?.IsAlive == true) {
                return;
            }
            Disposed = true;
            Collection.Dispose();
        }
    }

    public void Add(Action action) {
        if (action is null) {
            return;
        }
        try {
            // Admission and worker startup intentionally use separate operations.
            // Complete also starts the worker if it wins between these operations.
            Collection.Add(action);
        }
        catch (ObjectDisposedException) {
            return;
        }
        catch (InvalidOperationException) {
            return;
        }

        lock (ThreadLocker) {
            StartThreadUnsafe();
        }
    }

    public void Complete() => Complete(null);

    public bool Complete(TimeSpan timeout) {
        if (ValidTimeout(timeout) == false) {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
        return Complete(new TimeSpan?(timeout));
    }

    public void Dispose() {
        if (ThreadIsCurrentThread) {
            // Never dispose the collection while this worker is still reading it.
            ThreadPool.QueueUserWorkItem(_ => Dispose());
            return;
        }
        Complete();
        DisposeCollection();
        GC.SuppressFinalize(this);
    }
}
