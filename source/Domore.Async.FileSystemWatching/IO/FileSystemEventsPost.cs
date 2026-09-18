using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

internal sealed class FileSystemEventsPost {
    private readonly
#if NET9_0_OR_GREATER
    Lock
#else
    object
#endif
    Locker = new();

    private volatile bool Started;
    private volatile bool Stopped;
    private volatile IReadOnlyList<SubscriptionScheduler> Subscriptions = [];

    private CancellationTokenSource TokenSource;

    private async Task Start(bool restarting, CancellationToken token) {
        try {
            if (restarting) {
                var delay = RestartDelay;
                if (delay > TimeSpan.Zero) {
                    await Task.Delay(delay, token);
                }
            }
            var provider = new FileSystemEventProvider(Path, Options);
            var events = provider.Events(
                token: token,
                ready: _ => {
                    lock (Locker) {
                        if (Stopped != true) {
                            Operational = true;
                        }
                    }
                    return Task.CompletedTask;
                });
            await foreach (var e in events) {
                var tasks = Subscriptions.Select(s => s.Subscription.ReceiveInternal(s.Scheduler, e, token));
                var results = await Task.WhenAll(tasks);
                try {
                    var resultsHandled = ResultHandler?.Invoke(results, token);
                    if (resultsHandled is not null) {
                        await resultsHandled;
                    }
                }
                catch (Exception ex) {
                    var handler = ErrorHandler;
                    if (handler is null) {
                        throw;
                    }
                    var handled = handler(ex, token);
                    if (handled is not null) {
                        var @continue = await handled;
                        if (@continue) {
                            continue;
                        }
                    }
                    break;
                }
            }
        }
        finally {
            Operational = false;
        }
    }

    private async void Start() {
        using (var tokenSource = TokenSource = new CancellationTokenSource()) {
            var token = tokenSource.Token;
            var restarting = false;
            for (; ; ) {
                try {
                    await Start(restarting, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) {
                    break;
                }
                catch (Exception ex) {
                    var handler = ErrorHandler;
                    if (handler is null) {
                        throw;
                    }
                    var handled = handler(ex, default);
                    if (handled is not null) {
                        var @continue = await handled;
                        if (@continue == true) {
                            restarting = true;
                            continue;
                        }
                    }
                }
                break;
            }
        }
    }

    public TimeSpan RestartDelay { get; set; } = TimeSpan.FromSeconds(2.5);

    public bool Operational {
        get => _Operational;
        private set => _Operational = value;
    }
    private volatile bool _Operational;

    public int SubscriptionCount => Subscriptions.Count;
    public string Path { get; }
    public FileSystemEventOptions Options { get; }
    public Func<Exception, CancellationToken, Task<bool>> ErrorHandler { get; }
    public Func<FileSystemEventResult[], CancellationToken, Task> ResultHandler { get; }

    public FileSystemEventsPost(string path,
                                FileSystemEventOptions options = null,
                                Func<FileSystemEventResult[], CancellationToken, Task> resultHandler = null,
                                Func<Exception, CancellationToken, Task<bool>> errorHandler = null) {
        Path = path;
        Options = options;
        ResultHandler = resultHandler;
        ErrorHandler = errorHandler;
    }

    public int Add(FileSystemEventSubscription s, TaskScheduler scheduler) {
        if (s is null) {
            throw new ArgumentNullException(nameof(s));
        }
        if (Started != true) {
            lock (Locker) {
                if (Stopped) {
                    throw new InvalidOperationException("The events have already been stopped.");
                }
                if (Started != true) {
                    Start();
                    Started = true;
                }
            }
        }
        var subs = Subscriptions = [.. Subscriptions.Concat([new(s, scheduler)])];
        return subs.Count;
    }

    public int Remove(FileSystemEventSubscription s) {
        var list = Subscriptions.ToList();
        var index = list.FindLastIndex(i => object.Equals(i.Subscription, s));
        if (index >= 0) {
            list.RemoveAt(index);
        }
        var subs = Subscriptions = list;
        return subs.Count;
    }

    public void Stop() {
        lock (Locker) {
            if (Stopped) {
                return;
            }
            Stopped = true;
        }
        TokenSource?.Cancel();
    }

    private sealed record SubscriptionScheduler(FileSystemEventSubscription Subscription, TaskScheduler Scheduler) {
    }
}
