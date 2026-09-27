using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Diagnostics.Shims;

#if !NET
internal static class Process_WaitForExitAsync {
    public static Task Shim(Process process, CancellationToken cancellationToken) {
        static async Task waitForExitAsyncTask(Process process, CancellationToken cancellationToken) {
            if (process is null) {
                throw new ArgumentNullException(nameof(process));
            }
            if (cancellationToken.IsCancellationRequested) {
                cancellationToken.ThrowIfCancellationRequested();
            }
            if (process.HasExited) {
                return;
            }
#if NET40 || NET45
            var completion = new TaskCompletionSource<bool>();
#else
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            void onExited(object sender, EventArgs args) {
                completion.TrySetResult(true);
            }
            process.Exited += onExited;
            try {
                if (process.EnableRaisingEvents != true) {
                    process.EnableRaisingEvents = true;
                }
                if (process.HasExited) {
                    completion.TrySetResult(true);
                }
                using (cancellationToken.Register(() => {
                    completion.TrySetCanceled();
                })) {
                    await completion.Task.ConfigureAwait(false);
                }
            }
            finally {
                process.Exited -= onExited;
            }
        }
        return waitForExitAsyncTask(process, cancellationToken);
    }
}
#endif
