using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO.FileSystemEventSubscriptions {
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

        public Func<FileSystemEventArgs, CancellationToken, Task> Agent { get; set; }
    }
}
