using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Threading.Tasks {
    public class TaskCache<TResult> {
        private readonly object Locker = new();

        private bool Cached;
        private volatile Task<TResult> Task;

        public TResult Result { get; private set; }
        public Func<CancellationToken, Task<TResult>> Factory { get; }

        public TaskCache(Func<CancellationToken, Task<TResult>> factory) {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<TResult> Ready(CancellationToken token) {
            if (Cached == false) {
                if (Task == null) {
                    lock (Locker) {
                        if (Task == null) {
                            Task = Factory(token) ?? throw new InvalidOperationException("The returned task from the factory is null.");
                        }
                    }
                }
                try {
                    Result = await Task.ConfigureAwait(false);
                }
                catch {
                    lock (Locker) {
                        Task = null;
                    }
                    throw;
                }
            }
            Cached = true;
            return Result;
        }

        public class WithRefresh : TaskCache<TResult> {
            public WithRefresh(Func<CancellationToken, Task<TResult>> factory) : base(factory) {
            }

            public Task<TResult> Refreshed(CancellationToken token) {
                Refresh();
                return Ready(token);
            }

            public void Refresh() {
                lock (Locker) {
                    Task = null;
                    Cached = false;
                    Result = default;
                }
            }
        }
    }
}
