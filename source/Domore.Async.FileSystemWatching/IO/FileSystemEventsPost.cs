using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

internal sealed class FileSystemEventsPost : IDisposable {
    private readonly object Locker = new();

    private bool Started;
    private bool Disposed;
    private CancellationTokenSource TokenSource;
    private IReadOnlyList<FileSystemEventSubscription> Subscriptions = [];

    private async Task Start(CancellationToken token) {
        var provider = new FileSystemEventProvider(Path, Options);
        var events = provider.Events(
            token: token,
            ready: async _ => {
                lock (Locker) {
                    if (Disposed) {
                        return;
                    }
                    Operational = true;
                }
                await Task.CompletedTask;
            });
        await foreach (var e in events) {
            var tasks = Subscriptions.Select(s => s.Receive(e, token));
            await Task.WhenAll(tasks);
        }
    }

    private async void Start() {
        using (var tokenSource = TokenSource = new CancellationTokenSource()) {
            var token = tokenSource.Token;
            try {
                await Start(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) {
            }
        }
    }

    private void Dispose(bool disposing) {
        if (disposing) {
            lock (Locker) {
                Disposed = true;
                Operational = false;
            }
            using (TokenSource) {
                try {
                    TokenSource?.Cancel();
                }
                catch (ObjectDisposedException) {
                }
            }
        }
    }

    public bool Operational { get; private set; }

    public int SubscriptionCount => Subscriptions.Count;
    public string Path { get; }
    public FileSystemEventOptions Options { get; }

    public FileSystemEventsPost(string path, FileSystemEventOptions options = null) {
        Path = path;
        Options = options;
    }

    public int Add(FileSystemEventSubscription s) {
        if (s is null) {
            throw new ArgumentNullException(nameof(s));
        }
        if (Started == false) {
            lock (Locker) {
                if (Disposed) {
                    throw new ObjectDisposedException(nameof(FileSystemEventsPost));
                }
                if (Started == false) {
                    Start();
                    Started = true;
                }
            }
        }
        var subs = Subscriptions = [.. Subscriptions.Concat([s])];
        return subs.Count;
    }

    public int Remove(FileSystemEventSubscription s) {
        var subs = Subscriptions = [.. Subscriptions.Except([s])];
        return subs.Count;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~FileSystemEventsPost() {
        Dispose(false);
    }
}
