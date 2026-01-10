using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

/// <summary>
/// Implementations of this class handle file-system events.
/// </summary>
public abstract class FileSystemEventSubscription {
    /// <summary>
    /// When overridden in a derived class, handles the file-system event <paramref name="e"/>.
    /// </summary>
    /// <param name="e">The file-system event argument.</param>
    /// <param name="token">The cancellation token for the task.</param>
    /// <returns>A task that completes when the file-system event has been handled.</returns>
    protected internal abstract Task Receive(FileSystemEventArgs e, CancellationToken token);
}
