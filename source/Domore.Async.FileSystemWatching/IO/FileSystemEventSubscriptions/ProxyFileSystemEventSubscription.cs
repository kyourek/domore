using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO.FileSystemEventSubscriptions;

/// <summary>
/// Represents a file system event subscription that delegates event handling to a user-provided asynchronous agent.
/// </summary>
public sealed class ProxyFileSystemEventSubscription : FileSystemEventSubscription {
    protected internal sealed override Task Receive(FileSystemEventArgs e, CancellationToken token) {
        if (token.IsCancellationRequested) {
            return Task.FromCanceled(token);
        }
        var task = Agent?.Invoke(e, token);
        if (task is not null) {
            return task;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets or sets the asynchronous callback that handles file system events.
    /// </summary>
    public Func<FileSystemEventArgs, CancellationToken, Task> Agent { get; set; }
}
