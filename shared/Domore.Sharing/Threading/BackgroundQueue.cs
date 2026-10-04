using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Domore.Threading;

internal sealed class BackgroundQueue : IDisposable {
    private const int DefaultCapacity = 1024;

    private Thread Thread;
    private readonly object ThreadLocker = new();
    private readonly BlockingCollection<Action> Collection;
    private long _DroppedCount;

    private void ThreadStart() {
        for (; ; ) {
            var action = default(Action);
            try {
                action = Collection.Take();
            }
            catch (Exception ex) {
                if (ex is ObjectDisposedException) {
                    break;
                }
                if (ex is InvalidOperationException && Collection.IsAddingCompleted) {
                    break;
                }
                if (ex is OperationCanceledException && Collection.IsAddingCompleted) {
                    break;
                }
                throw;
            }
            if (action != null) {
                try {
                    action();
                }
                catch (Exception ex) {
                    try { Console.WriteLine(ex); } catch { }
                    try { Trace.WriteLine(ex); } catch { }
                }
            }
        }
    }

    private bool Complete(TimeSpan? timeout) {
        Collection.CompleteAdding();

        Thread thread;
        lock (ThreadLocker) {
            thread = Thread;
        }
        if (thread == null) {
            return true;
        }
        if (thread == Thread.CurrentThread) {
            return false;
        }
        if (timeout.HasValue) {
            return thread.Join(timeout.Value);
        }
        thread.Join();
        return true;
    }

    public long DroppedCount =>
        Interlocked.Read(ref _DroppedCount);

    public BackgroundQueue() : this(DefaultCapacity) {
    }

    public BackgroundQueue(int capacity) {
        Collection = new BlockingCollection<Action>(new ConcurrentQueue<Action>(), capacity);
    }

    public void Add(Action action) {
        if (action != null) {
            try {
                if (Thread == null) {
                    lock (ThreadLocker) {
                        if (Thread == null) {
                            var thread = new Thread(ThreadStart) {
                                Name = GetType().Name,
                                IsBackground = true
                            };
                            thread.Start();
                            Thread = thread;
                        }
                    }
                }
                /* Drop the newest submission so queued work retains FIFO order. */
                if (Collection.TryAdd(action) == false) {
                    Interlocked.Increment(ref _DroppedCount);
                }
            }
            catch {
            }
        }
    }

    public void Complete() => Complete(null);
    public bool Complete(TimeSpan timeout) => Complete(new TimeSpan?(timeout));

    public void Dispose() {
        Collection.Dispose();
    }
}
