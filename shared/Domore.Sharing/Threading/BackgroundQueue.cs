using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Domore.Threading;

internal sealed class BackgroundQueue : IDisposable {
    private Thread Thread;
    private readonly object ThreadLocker = new();
    private readonly BlockingCollection<Action> Collection = new();

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

    private void Dispose(bool disposing) {
        if (disposing) {
            Collection.Dispose();
        }
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
                Collection.Add(action);
            }
            catch {
            }
        }
    }

    public void Complete() => Complete(null);
    public bool Complete(TimeSpan timeout) => Complete(new TimeSpan?(timeout));

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BackgroundQueue() {
        Dispose(false);
    }
}
