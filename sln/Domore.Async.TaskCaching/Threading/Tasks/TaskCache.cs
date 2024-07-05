using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Threading.Tasks {
    /// <summary>
    /// Caches the result of a <see cref="System.Threading.Tasks.Task"/> upon its first successful completion.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the task.</typeparam>
    public class TaskCache<TResult> {
        private readonly object Locker = new();

        private bool Cached;
        private volatile Task<TResult> Task;

        /// <summary>
        /// Gets the value of the result of the task that was cached due to successful completion of the task,
        /// or the default value of <see cref="TResult"/> if the task has not yet successfully completed.
        /// </summary>
        public TResult Result { get; private set; }

        /// <summary>
        /// Gets the callback used to create the instance of <see cref="System.Threading.Tasks.Task"/> whose
        /// result is cached.
        /// </summary>
        public Func<CancellationToken, Task<TResult>> Factory { get; }

        /// <summary>
        /// Creates a new instance of a task cache.
        /// </summary>
        /// <param name="factory">The callback used to create the instance of <see cref="System.Threading.Tasks.Task"/> whose result is cached.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is null.</exception>
        public TaskCache(Func<CancellationToken, Task<TResult>> factory) {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Returns the result of the task returned from the <see cref="Factory"/> callback upon its first successful completion.
        /// </summary>
        /// <param name="token">The cancellation token passed to the <see cref="Factory"/> callback.</param>
        /// <returns>
        /// The cached instance of <see cref="System.Threading.Tasks.Task"/> that was the result of the first successful completion 
        /// of the task returned from the <see cref="Factory"/> callback, or the most recent instance returned from the callback if
        /// the task completes with a fault or cancellation.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="Factory"/> callback returns null.</exception>
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

        /// <summary>
        /// An implementation of <see cref="TaskCache{TResult}"/> that allows resetting state.
        /// </summary>
        public class WithRefresh : TaskCache<TResult> {
            /// <summary>
            /// Creates a new instance of a <see cref="TaskCache{TResult}"/> that allows resetting state.
            /// </summary>
            /// <param name="factory">The callback used to create the instance of <see cref="System.Threading.Tasks.Task"/> whose result is cached.</param>
            public WithRefresh(Func<CancellationToken, Task<TResult>> factory) : base(factory) {
            }

            /// <summary>
            /// Returns a task that completes after the cache is refreshed and the task returned from the <see cref="Factory"/> callback completes.
            /// </summary>
            /// <param name="token">The cancellation token passed to the <see cref="Factory"/> callback.</param>
            /// <returns>
            /// The cached instance of <see cref="System.Threading.Tasks.Task"/> that was the result of the first successful completion 
            /// of the task returned from the <see cref="Factory"/> callback, or the most recent instance returned from the callback if
            /// the task completes with a fault or cancellation.
            /// </returns>
            public Task<TResult> Refreshed(CancellationToken token) {
                Refresh();
                return Ready(token);
            }

            /// <summary>
            /// Clears the cache, meaning the next call to <see cref="Ready"/> or <see cref="Refreshed"/> will call the <see cref="Factory"/> callback again.
            /// </summary>
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
