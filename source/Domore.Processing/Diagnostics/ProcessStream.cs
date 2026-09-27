using Domore.Notification;
#if NET40
using Domore.Diagnostics.Shims;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Diagnostics;

internal sealed class ProcessStream : Notifier, IProcessStream, IDisposable {
    private const int BufferSize = 4096;

    private readonly Task ReadTask;
    private readonly CancellationTokenSource Cts;
    private readonly ConcurrentQueue<CollectionChange> CollectionChanges;
    private readonly ConcurrentQueue<Exception> CollectionExceptions;
    private readonly ObservableCollection<IProcessStreamOutput> Collection;
    private readonly ReadOnlyObservableCollection<IProcessStreamOutput> ReadOnlyCollection;

    private int CollectionChangesScheduled;

    private sealed class CollectionChange {
        public ProcessStreamOutput Item { get; }
        public TaskCompletionSource<bool> Completion { get; }

        public CollectionChange(ProcessStreamOutput item, TaskCompletionSource<bool> completion) {
            Item = item;
            Completion = completion;
        }
    }

    private Task Read(StreamReader reader, ProcessOutputKind kind, CancellationToken cancellationToken) {
        if (reader is null) {
            throw new ArgumentNullException(nameof(reader));
        }
#if NET40
        return Task.Factory.StartNew(
            () => ReadSynchronously(reader, kind, cancellationToken),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
#else
        return ReadAsynchronously(reader, kind, cancellationToken);
#endif
    }

#if !NET40
    private async Task ReadAsynchronously(StreamReader reader,
                                         ProcessOutputKind kind,
                                         CancellationToken cancellationToken) {
        var buffer = new char[BufferSize];
        var item = default(ProcessStreamOutput);
        for (; ; ) {
            var read = default(int);
            try {
                cancellationToken.ThrowIfCancellationRequested();
#if NET
                read = await reader.ReadAsync(buffer, cancellationToken);
#else
                read = await reader.ReadAsync(buffer, 0, buffer.Length);
#endif
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                return;
            }
            if (cancellationToken.IsCancellationRequested) {
                return;
            }
            if (read == 0) {
                AddLine(item);
                return;
            }
            ProcessBuffer(buffer, read, kind, ref item);
        }
    }
#endif

#if NET40
    private void ReadSynchronously(StreamReader reader, ProcessOutputKind kind, CancellationToken cancellationToken) {
        /*
         * Microsoft.Bcl.Async's TextReader.ReadAsync queues each blocking Read on the 
         * shared thread pool. Keep this loop on one LongRunning task per pipe instead, 
         * avoiding a thread-pool work item for every buffer read. 
         */
        var buffer = new char[BufferSize];
        var item = default(ProcessStreamOutput);
        for (; ; ) {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }
            var read = reader.Read(buffer, 0, buffer.Length);
            if (cancellationToken.IsCancellationRequested) {
                return;
            }
            if (read == 0) {
                AddLine(item);
                return;
            }
            ProcessBuffer(buffer, read, kind, ref item);
        }
    }
#endif

    private void ProcessBuffer(char[] buffer, int count, ProcessOutputKind kind, ref ProcessStreamOutput item) {
        var start = 0;
        for (var i = 0; i < count; i++) {
            var c = buffer[i];
            if (c == '\b') {
                Append(buffer, start, i - start, kind, ref item);
                item?.Backspace();
                start = i + 1;
                continue;
            }
            if (c != '\r' && c != '\n') {
                continue;
            }
            Append(buffer, start, i - start, kind, ref item);
            if (c == '\n') {
                var line = item ?? new ProcessStreamOutput(kind);
                AddLine(line);
                item = null;
            }
            start = i + 1;
        }
        Append(buffer, start, count - start, kind, ref item);
    }

    private void Append(char[] buffer,
                        int index,
                        int count,
                        ProcessOutputKind kind,
                        ref ProcessStreamOutput item) {
        if (count == 0) {
            return;
        }
        if (item is null) {
            item = _CurrentItem = new ProcessStreamOutput(kind);
            NotifyPropertyChanged(nameof(CurrentItem));
        }
        item.Append(buffer, index, count);
    }

    private void AddLine(ProcessStreamOutput item) {
        if (item is null) {
            return;
        }
        if (ReferenceEquals(Interlocked.CompareExchange(ref _CurrentItem, null, item), item)) {
            NotifyPropertyChanged(nameof(CurrentItem));
        }
        CollectionChanges.Enqueue(new CollectionChange(item, completion: null));
        PostCollectionChanges();
    }

    private void PostCollectionChanges() {
        if (Interlocked.CompareExchange(ref CollectionChangesScheduled, 1, 0) != 0) {
            return;
        }
        try {
            if (SynchronizationContext is null) {
                if (ThreadPool.QueueUserWorkItem(DrainCollectionChanges) == false) {
                    throw new InvalidOperationException(
                        "The process stream collection update could not be scheduled.");
                }
            }
            else {
                SynchronizationContext.Post(DrainCollectionChanges, state: null);
            }
        }
        catch {
            Interlocked.Exchange(ref CollectionChangesScheduled, 0);
            throw;
        }
    }

    private void DrainCollectionChanges(object state) {
        while (CollectionChanges.TryDequeue(out var change)) {
            if (change.Item is null) {
                var completion = change.Completion;
                var exceptions = new List<Exception>();
                while (CollectionExceptions.TryDequeue(out var exception)) {
                    exceptions.Add(exception);
                }
                if (exceptions.Count == 0) {
                    completion.TrySetResult(true);
                }
                else {
                    completion.TrySetException(exceptions);
                }
                continue;
            }
            try {
                Collection.Add(change.Item);
            }
            catch (Exception exception) {
                CollectionExceptions.Enqueue(exception);
            }
        }
        Interlocked.Exchange(ref CollectionChangesScheduled, 0);
        if (CollectionChanges.IsEmpty == false) {
            PostCollectionChanges();
        }
    }

    private static TaskCompletionSource<bool> NewCollectionCompletion() {
#if !NET46_OR_GREATER
        return new TaskCompletionSource<bool>();
#else
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
    }

    private Task ReadTaskFactory() {
#if NET40
        var tasks = Task_WhenAll.Shim(
            untilCanceled(Read(Process.StandardError, ProcessOutputKind.StandardError, Cts.Token), Cts.Token),
            untilCanceled(Read(Process.StandardOutput, ProcessOutputKind.StandardOutput, Cts.Token), Cts.Token));
#else
        var tasks = Task.WhenAll(
            untilCanceled(Read(Process.StandardError, ProcessOutputKind.StandardError, Cts.Token), Cts.Token),
            untilCanceled(Read(Process.StandardOutput, ProcessOutputKind.StandardOutput, Cts.Token), Cts.Token));
#endif
        /*
         * Pipe reads may not observe cancellation (for example, when a child process
         * still holds the pipe open), so stop waiting for them once canceled.
         */
        static Task untilCanceled(Task read, CancellationToken cancellationToken) {
            var completion = new TaskCompletionSource<bool>();
            var registration = cancellationToken.Register(() => completion.TrySetResult(true));
            read.ContinueWith(task => {
                try {
                    registration.Dispose();
                }
                catch (ObjectDisposedException) {
                }
                if (task.IsFaulted) {
                    completion.TrySetException(task.Exception.InnerExceptions);
                }
                else if (task.IsCanceled) {
                    completion.TrySetCanceled();
                }
                else {
                    completion.TrySetResult(true);
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return completion.Task;
        }
        async Task complete() {
            await tasks;
            var completion = NewCollectionCompletion();
            CollectionChanges.Enqueue(new CollectionChange(null, completion));
            PostCollectionChanges();
            await completion.Task;
        }
        return complete();
    }

    public ReadOnlyObservableCollection<IProcessStreamOutput> LineItems => ReadOnlyCollection;

    public ProcessStreamOutput CurrentItem => _CurrentItem;
    private volatile ProcessStreamOutput _CurrentItem;

    public Process Process { get; }
    public SynchronizationContext SynchronizationContext { get; }

    public ProcessStream(Process process, SynchronizationContext synchronizationContext) {
        Process = process;
        SynchronizationContext = synchronizationContext;
        Cts = new();
        CollectionChanges = new();
        CollectionExceptions = new();
        Collection = [];
        ReadOnlyCollection = new(Collection);
        ReadTask = ReadTaskFactory();
    }

    public Task Cancel() {
        Cts.Cancel();
        return ReadTask;
    }

    public Task Complete() {
        return ReadTask;
    }

    void IDisposable.Dispose() {
        using (Cts) {
        }
    }

    IProcessStreamOutput IProcessStream.CurrentItem => CurrentItem;
    IList<IProcessStreamOutput> IProcessStream.LineItems => LineItems;
}
