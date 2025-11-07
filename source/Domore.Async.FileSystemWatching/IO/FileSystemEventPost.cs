using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

internal sealed class FileSystemEventPost : IDisposable {
    private readonly object Locker = new();

    private bool Started;
    private bool Disposed;
    private CancellationTokenSource TokenSource;
    private IReadOnlyList<FileSystemEventSubscription> Subscriptions = [];

    private async void Start() {
        var provider = new FileSystemEventProvider(Path, Options);
        using (var tokenSource = TokenSource = new CancellationTokenSource()) {
            var token = tokenSource.Token;
            var events = provider.Events(
                token: token,
                ready: async _ => Operational = await Task.FromResult(true));
            await foreach (var e in events) {
                var tasks = Subscriptions.Select(s => s.Receive(e, token));
                await Task.WhenAll(tasks);
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

    public FileSystemEventPost(string path, FileSystemEventOptions options = null) {
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
                    throw new ObjectDisposedException(nameof(FileSystemEventPost));
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

    ~FileSystemEventPost() {
        Dispose(false);
    }
}
