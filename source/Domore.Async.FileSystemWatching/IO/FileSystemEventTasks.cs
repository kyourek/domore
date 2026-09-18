using Domore.IO.FileSystemEventSubscriptions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

/// <summary>
/// Adds callbacks for file-system events.
/// </summary>
public static class FileSystemEventTasks {
    private static readonly FileSystemEventManager Manager = new() {
        OnSubscriptionEventCanceled = (result, token) => {
            return OnSubscriptionEventCanceled?.Invoke(result, token) ?? Task.CompletedTask;
        },
        OnSubscriptionEventComplete = (result, token) => {
            return OnSubscriptionEventComplete?.Invoke(result, token) ?? Task.CompletedTask;
        },
        OnSubscriptionEventError = (result, token) => {
            return OnSubscriptionEventError?.Invoke(result, token) ?? Task.CompletedTask;
        },
        OnUnhandledError = (exception, token) => {
            return OnUnhandledError?.Invoke(exception, token) ?? Task.FromResult(false);
        },
    };

    private static async Task
    Manage(FileSystemEventSubscription subscription,
           Func<FileSystemEventManager, FileSystemEventSubscription, CancellationToken, Task> function,
           CancellationToken token) {
        try {
            var canceled = false;
            var exception = default(Exception);
            try {
                await function(Manager, subscription, token);
            }
            catch (OperationCanceledException ex1) when (token.IsCancellationRequested) {
                canceled = true;
                exception = ex1;
            }
            catch (Exception ex2) {
                exception = ex2;
            }
            var result = new FileSystemEventResult {
                Canceled = canceled,
                Exception = exception,
                Subscription = subscription,
            };
            if (result.Canceled) {
                var handler = OnManagerCanceled;
                var handled = handler?.Invoke(result, token);
                if (handled is not null) {
                    await handled;
                }
                return;
            }
            if (result.Exception is not null) {
                var handler = OnManagerError;
                var handled = handler?.Invoke(result, token);
                if (handled is not null) {
                    await handled;
                }
                return;
            }
        }
        catch (Exception ex) {
            var handler = OnUnhandledError;
            if (handler is null) {
                throw;
            }
            var handled = handler?.Invoke(ex, token);
            if (handled is not null) {
                var result = await handled;
                if (result != true) {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the handler invoked when an unhandled error occurs while managing file-system events.
    /// </summary>
    public static Func<Exception, CancellationToken, Task<bool>> OnUnhandledError { get; set; }

    /// <summary>
    /// Gets or sets the handler invoked when a subscription event completes successfully.
    /// </summary>
    public static Func<FileSystemEventResult, CancellationToken, Task> OnSubscriptionEventComplete { get; set; }

    /// <summary>
    /// Gets or sets the handler invoked when a subscription event is canceled.
    /// </summary>
    public static Func<FileSystemEventResult, CancellationToken, Task> OnSubscriptionEventCanceled { get; set; }

    /// <summary>
    /// Gets or sets the handler invoked when a subscription event fails with an error.
    /// </summary>
    public static Func<FileSystemEventResult, CancellationToken, Task> OnSubscriptionEventError { get; set; }

    /// <summary>
    /// Gets or sets the handler invoked when a manager operation fails with an error.
    /// </summary>
    public static Func<FileSystemEventResult, CancellationToken, Task> OnManagerError { get; set; }

    /// <summary>
    /// Gets or sets the handler invoked when a manager operation is canceled.
    /// </summary>
    public static Func<FileSystemEventResult, CancellationToken, Task> OnManagerCanceled { get; set; }

    /// <summary>
    /// Adds a callback for a file-system event.
    /// </summary>
    /// <param name="path">The path to the directory to be watched.</param>
    /// <param name="options">The options.</param>
    /// <param name="task">The callback.</param>
    /// <returns>
    /// An instance of <see cref="IDisposable"/> that, when disposed, removes the callback.
    /// </returns>
    public static IDisposable Add(string path,
                                  FileSystemEventOptions options,
                                  Func<FileSystemEventArgs, CancellationToken, Task> task) {
        var subscription = new ProxyFileSystemEventSubscription() {
            Agent = task
        };
        var added = Manage(subscription, (m, s, t) => m.Add(s, path, options, t), token: default);
        return new Disposable(added, subscription, path, options);
    }

    /// <summary>
    /// Adds a callback for a file-system event.
    /// </summary>
    /// <param name="path">The path to the directory to be watched.</param>
    /// <param name="task">The callback.</param>
    /// <returns>
    /// An instance of <see cref="IDisposable"/> that, when disposed, removes the callback.
    /// </returns>
    public static IDisposable Add(string path, Func<FileSystemEventArgs, CancellationToken, Task> task) {
        return Add(path, options: null, task);
    }

    private sealed class Disposable : IDisposable {
        private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        DisposeLocker = new();

        private bool Disposed;

        public string Path { get; }
        public Task Added { get; }
        public FileSystemEventOptions Options { get; }
        public FileSystemEventSubscription Subscription { get; }

        public Disposable(Task added, FileSystemEventSubscription subscription, string path, FileSystemEventOptions options) {
            Added = added ?? throw new ArgumentNullException(nameof(added));
            Subscription = subscription;
            Path = path;
            Options = options;
        }

        void IDisposable.Dispose() {
            lock (DisposeLocker) {
                if (Disposed == true) {
                    return;
                }
                Added.ContinueWith(
                    cancellationToken: CancellationToken.None,
                    continuationOptions: TaskContinuationOptions.ExecuteSynchronously,
                    scheduler: TaskScheduler.Default,
                    continuationAction: _ => {
                        _ = Manage(Subscription, (m, s, t) => m.Remove(s, Path, Options, t), token: default);
                    });
                Disposed = true;
            }
        }
    }
}
