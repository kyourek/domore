using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Domore.Threading {
    internal sealed class BackgroundQueue : IDisposable {
        private Thread Thread;
        private readonly object ThreadLocker = new object();
        private readonly BlockingCollection<Action> Collection = new BlockingCollection<Action>();

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

            if (Thread != null) {
                lock (ThreadLocker) {
                    if (Thread != null) {
                        if (timeout.HasValue) {
                            return Thread.Join(timeout.Value);
                        }
                        Thread.Join();
                        return true;
                    }
                }
            }

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
                    Collection.Add(action);
                }
                catch (Exception ex) {
                    if (ex is ObjectDisposedException) {
                        return;
                    }
                    if (ex is InvalidOperationException && Collection.IsAddingCompleted) {
                        return;
                    }
                    if (ex is OperationCanceledException && Collection.IsAddingCompleted) {
                        return;
                    }
                    throw;
                }
                if (Thread == null) {
                    lock (ThreadLocker) {
                        if (Thread == null) {
                            Thread = new Thread(ThreadStart);
                            Thread.Name = GetType().Name;
                            Thread.IsBackground = true;
                            Thread.Start();
                        }
                    }
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
}
