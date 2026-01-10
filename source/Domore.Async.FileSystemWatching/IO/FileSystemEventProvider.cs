using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Domore.IO;

/// <summary>
/// Provides a mechanism to monitor a specified directory for file system changes, such as file creation, 
/// modification, or deletion.
/// </summary>
/// <remarks>
/// The <see cref="FileSystemEventProvider"/> class allows you to observe file system events in a specified 
/// directory. It supports asynchronous enumeration of events, making it suitable for scenarios where
/// non-blocking operations are required. The events are raised based on the options specified during the creation of
/// the provider, such as filters for file types, notification filters, and whether to include subdirectories. The
/// provider uses a <see cref="FileSystemWatcher"/> internally to monitor the file system.  This class is sealed and
/// cannot be inherited.
/// </remarks>
public sealed class FileSystemEventProvider {
    private const NotifyFilters NotifyAll = NotifyFilters.Attributes |
                                            NotifyFilters.CreationTime |
                                            NotifyFilters.DirectoryName |
                                            NotifyFilters.FileName |
                                            NotifyFilters.LastAccess |
                                            NotifyFilters.LastWrite |
                                            NotifyFilters.Security |
                                            NotifyFilters.Size;

    /// <summary>
    /// Gets the path of the directory watched for file-system events.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the options with which the file-system event provider was created.
    /// </summary>
    public FileSystemEventOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemEventProvider"/> class,  which monitors a specified
    /// directory for file system events.
    /// </summary>
    /// <param name="path">The path of the directory to monitor.</param>
    /// <param name="options">
    /// Optional configuration settings for monitoring file system events. If null, default options are used.
    /// </param>
    public FileSystemEventProvider(string path, FileSystemEventOptions options = null) {
        Path = path;
        Options = options;
    }

    /// <summary>
    /// Asynchronously monitors the <see cref="Path"/> and yields events representing changes to the file system.
    /// </summary>
    /// <param name="ready">
    /// An optional callback that is invoked when the file-system watcher is ready. The callback receives a 
    /// <see cref="CancellationToken"/> and can be used to perform additional setup or synchronization. 
    /// If <paramref name="ready"/> is <see langword="null"/>, no callback is invoked.
    /// </param>
    /// <param name="token">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// If cancellation is requested, the method stops monitoring and completes the enumeration.</param>
    /// <returns>
    /// An asynchronous stream of <see cref="FileSystemEventArgs"/> representing-file system events such as file
    /// creation, modification, deletion, or renaming. The enumeration ends when the watcher is disposed or an error
    /// occurs.
    /// </returns>
    /// <exception cref="FileSystemWatcherInitializationException">
    /// Thrown if the <see cref="FileSystemWatcher"/> fails to initialize due to invalid configuration or other errors
    /// during setup.
    /// </exception>
    public async IAsyncEnumerable<FileSystemEventArgs> 
    Events(Func<CancellationToken, Task> ready = null, [EnumeratorCancellation] CancellationToken token = default) {
        var path = Path;
        var channelOptions = new UnboundedChannelOptions { SingleReader = true, SingleWriter = false };
        var channel = Channel.CreateUnbounded<FileSystemEventArgs>(channelOptions);
        var reader = channel.Reader;
        var writer = channel.Writer;
        void disposedHandler(object sender, EventArgs e) {
            writer.TryComplete();
        }
        void errorHandler(object sender, ErrorEventArgs e) {
            writer.TryComplete(e?.GetException());
        }
        async void eventHandler(object sender, FileSystemEventArgs e) {
            try {
                await writer.WriteAsync(e, token).ConfigureAwait(false);
            }
            catch (Exception ex) {
                writer.TryComplete(ex);
            }
        }
        FileSystemWatcher createWatcher() {
            var watcher = default(FileSystemWatcher);
            try {
                watcher = new();
                watcher.Changed += eventHandler;
                watcher.Created += eventHandler;
                watcher.Deleted += eventHandler;
                watcher.Renamed += eventHandler;
                watcher.Error += errorHandler;
                watcher.Disposed += disposedHandler;
                watcher.Path = path;
                watcher.Filter = Options?.FileFilter ?? "";
                watcher.NotifyFilter = Options?.NotifyFilter ?? NotifyAll;
                watcher.IncludeSubdirectories = Options?.IncludeSubdirectories ?? false;
                watcher.InternalBufferSize = Options?.InternalBufferSize ?? 65536;
                watcher.EnableRaisingEvents = true;
                return watcher;
            }
            catch (Exception e1) {
                var innerException = e1;
                try {
                    watcher?.Dispose();
                }
                catch (Exception e2) {
                    innerException = new AggregateException(e1, e2);
                }
                throw new FileSystemWatcherInitializationException(
                    nameof(FileSystemWatcherInitializationException), 
                    innerException);
            }
        }
        using (var watcher = await Task.Run(createWatcher, token)) {
            var readyTask = ready?.Invoke(token);
            if (readyTask is not null) {
                await readyTask;
            }
            for (; ; ) {
                var reading = await reader.WaitToReadAsync(token).ConfigureAwait(false);
                if (reading == false) {
                    break;
                }
                if (reader.TryRead(out var e)) {
                    yield return e;
                }
            }
        }
    }
}
