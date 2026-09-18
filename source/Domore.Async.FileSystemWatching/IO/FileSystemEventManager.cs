using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

/// <summary>
/// Manages file-system event subscriptions.
/// </summary>
public sealed class FileSystemEventManager {
    private readonly Agent CaseSensitiveAgent;
    private readonly Agent CaseInsensitiveAgent;

    private async Task<PostInfo> Get(string path, FileSystemEventOptions options, CancellationToken token) {
        var caseSensitive = await FileSystemPath.IsCaseSensitive(path, token).ConfigureAwait(false);
        var key = new FileSystemEventsKey(path, caseSensitive, options);
        var agent = caseSensitive
            ? CaseSensitiveAgent
            : CaseInsensitiveAgent;
        return new(agent, agent.Get(key), key);
    }

    private async Task<bool> ErrorHandler(Exception exception, CancellationToken token) {
        var handler = OnUnhandledError;
        var handled = handler?.Invoke(exception, token);
        if (handled is not null) {
            return await handled;
        }
        return false;
    }

    private async Task ResultHandler(FileSystemEventResult[] results, CancellationToken token) {
        if (results?.Length > 0) {
            var complete = results.Where(i => i.Canceled is false && i.Exception is null);
            var completeHandler = OnSubscriptionEventComplete;
            var completeHandlers = complete.Select(i => completeHandler?.Invoke(i, token) ?? Task.CompletedTask);
            var canceled = results.Where(i => i.Canceled);
            var canceledHandler = OnSubscriptionEventCanceled;
            var canceledHandlers = canceled.Select(i => canceledHandler?.Invoke(i, token) ?? Task.CompletedTask);
            var error = results.Where(i => i.Canceled is false && i.Exception is not null);
            var errorHandler = OnSubscriptionEventError;
            var errorHandlers = error.Select(i => errorHandler?.Invoke(i, token) ?? Task.CompletedTask);
            var allHandlers = completeHandlers.Concat(canceledHandlers).Concat(errorHandlers);
            await Task.WhenAll(allHandlers);
        }
    }

    internal TimeSpan ClearDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the handler invoked when an unhandled error occurs while processing file-system events.
    /// </summary>
    public Func<Exception, CancellationToken, Task<bool>> OnUnhandledError { get; set; }

    /// <summary>
    /// Gets or sets the handler invoked when a subscription event completes successfully.
    /// </summary>
    public Func<FileSystemEventResult, CancellationToken, Task> OnSubscriptionEventComplete { get; set; }

    /// <summary>
    /// Gets or sets the handler invoked when a subscription event is canceled.
    /// </summary>
    public Func<FileSystemEventResult, CancellationToken, Task> OnSubscriptionEventCanceled { get; set; }

    /// <summary>
    /// Gets or sets the handler invoked when a subscription event fails with an error.
    /// </summary>
    public Func<FileSystemEventResult, CancellationToken, Task> OnSubscriptionEventError { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemEventManager"/> class.
    /// </summary>
    public FileSystemEventManager() {
        CaseSensitiveAgent = new(ResultHandler, ErrorHandler);
        CaseInsensitiveAgent = new(ResultHandler, ErrorHandler);
    }

    /// <summary>
    /// Adds an event subscription to the specified path with the specified options.
    /// </summary>
    /// <param name="subscription">The event subscription to be added.</param>
    /// <param name="path">The path of the watched directory.</param>
    /// <param name="options">The options for watching the path.</param>
    /// <param name="token">The cancellation token for the task.</param>
    /// <returns>A task that completes when the event subscription has been added.</returns>
    public async Task Add(FileSystemEventSubscription subscription,
                          string path,
                          FileSystemEventOptions options,
                          CancellationToken token) {
        var scheduler = SynchronizationContext.Current is null
            ? TaskScheduler.Default
            : TaskScheduler.FromCurrentSynchronizationContext();
        var item = await Get(path, options, token).ConfigureAwait(false);
        lock (item.Agent) {
            item.Post.Add(subscription, scheduler);
        }
    }

    /// <summary>
    /// Removes an event subscription from the specified path with the specified options.
    /// </summary>
    /// <param name="subscription">The event subscription to be removed.</param>
    /// <param name="path">The path of the watched directory.</param>
    /// <param name="options">The options for watching the path.</param>
    /// <param name="token">The cancellation token for the task.</param>
    /// <returns>A task that completes when the event subscription has been removed.</returns>
    public async Task Remove(FileSystemEventSubscription subscription,
                             string path,
                             FileSystemEventOptions options,
                             CancellationToken token) {
        var item = await Get(path, options, token).ConfigureAwait(false);
        lock (item.Agent) {
            var count = item.Post.Remove(subscription);
            if (count == 0) {
                _ = Task.Run(cancellationToken: default, function: async () => {
                    var delay = ClearDelay;
                    if (delay > TimeSpan.Zero) {
                        await Task.Delay(delay).ConfigureAwait(false);
                    }
                    lock (item.Agent) {
                        if (item.Post.SubscriptionCount == 0) {
                            item.Agent.Clear(item.Key);
                        }
                    }
                });
            }
        }
    }

    private sealed record PostInfo(Agent Agent, FileSystemEventsPost Post, FileSystemEventsKey Key) {
    }

    private sealed class Agent {
        private readonly Dictionary<FileSystemEventsKey, FileSystemEventsPost> Lookup = [];

        private FileSystemEventsPost Create(FileSystemEventsKey key) {
            if (key is null) {
                throw new ArgumentNullException(nameof(key));
            }
            return new(key.Path, key.Options, ResultHandler, ErrorHandler);
        }

        public Func<Exception, CancellationToken, Task<bool>> ErrorHandler { get; }
        public Func<FileSystemEventResult[], CancellationToken, Task> ResultHandler { get; }

        public Agent(Func<FileSystemEventResult[], CancellationToken, Task> resultHandler,
                     Func<Exception, CancellationToken, Task<bool>> errorHandler) {
            ResultHandler = resultHandler;
            ErrorHandler = errorHandler;
        }

        public FileSystemEventsPost Get(FileSystemEventsKey key) {
            lock (Lookup) {
                if (Lookup.TryGetValue(key, out var post) == false) {
                    Lookup[key] = post = Create(key);
                }
                return post;
            }
        }

        public void Clear(FileSystemEventsKey key) {
            lock (Lookup) {
                if (Lookup.TryGetValue(key, out var post)) {
                    Lookup.Remove(key);
                    post.Stop();
                }
            }
        }
    }
}
