using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

/// <summary>
/// Implementations of this class handle file-system events.
/// </summary>
public abstract class FileSystemEventSubscription {
    internal async Task<FileSystemEventResult> ReceiveInternal(TaskScheduler scheduler,
                                                               FileSystemEventArgs e,
                                                               CancellationToken token) {
        var canceled = false;
        var exception = default(Exception);
        try {
            var task = Task.Factory.StartNew(
                cancellationToken: token,
                creationOptions: TaskCreationOptions.DenyChildAttach,
                scheduler: scheduler,
                function: () => {
                    var task = Receive(e, token);
                    return task ?? Task.CompletedTask;
                });
            await task.Unwrap();
        }
        catch (OperationCanceledException ex) when (token.IsCancellationRequested) {
            canceled = true;
            exception = ex;
        }
        catch (Exception ex) {
            exception = ex;
        }
        return new() {
            Canceled = canceled,
            Exception = exception,
            Subscription = this,
        };
    }

    /// <summary>
    /// When overridden in a derived class, handles the file-system event <paramref name="e"/>.
    /// </summary>
    /// <param name="e">The file-system event argument.</param>
    /// <param name="token">The cancellation token for the task.</param>
    /// <returns>A task that completes when the file-system event has been handled.</returns>
    protected abstract Task Receive(FileSystemEventArgs e, CancellationToken token);
}
