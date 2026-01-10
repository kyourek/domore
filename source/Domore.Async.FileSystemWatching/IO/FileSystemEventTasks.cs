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
    private static readonly FileSystemEventManager Manager = new();

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
        _ = Manager.Add(subscription, path, options, default);
        return new Disposable(subscription, path, options);
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
        public string Path { get; }
        public FileSystemEventOptions Options { get; }
        public FileSystemEventSubscription Subscription { get; }

        public Disposable(FileSystemEventSubscription subscription, string path, FileSystemEventOptions options) {
            Subscription = subscription;
            Path = path;
            Options = options;
        }

        void IDisposable.Dispose() {
            _ = Manager.Remove(Subscription, Path, Options, default);
        }
    }
}
