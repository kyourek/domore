using System;
using System.Threading;
using System.Threading.Tasks;
using TASK = System.Threading.Tasks.Task;

namespace Domore.Threading.Tasks {
    /// <summary>
    /// Caches the result of a <see cref="TASK"/> upon its first successful completion.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the task.</typeparam>
    public class TaskCache<TResult> {
        private static readonly TASK CompletedTask =
#if NET40
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            new Func<TASK>(static async () => { })()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#else
            TASK.CompletedTask
#endif
        ;

        private readonly object Locker = new();

        private bool Cached;
        private bool CacheResets;
        private Task<TResult> Task;

        /// <summary>
        /// Gets a flag that indicates whether or not awaited tasks are continued on the captured context.
        /// </summary>
        public bool ContinueOnCapturedContext { get; }

        /// <summary>
        /// Gets the value of the result of the task that was cached due to successful completion of the task,
        /// or the default value of <typeparamref name="TResult"/> if the task has not yet successfully completed.
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
        /// <param name="continueOnCapturedContext">Whether or not to continue awaited tasks on the captured context.</param>
        /// <param name="factory">The callback used to create the instance of <see cref="System.Threading.Tasks.Task"/> whose result is cached.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is null.</exception>
        public TaskCache(bool continueOnCapturedContext, Func<CancellationToken, Task<TResult>> factory) {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            ContinueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>
        /// Creates a new instance of a task cache.
        /// </summary>
        /// <param name="factory">The callback used to create the instance of <see cref="System.Threading.Tasks.Task"/> whose result is cached.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is null.</exception>
        public TaskCache(Func<CancellationToken, Task<TResult>> factory) : this(false, factory) {
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
            if (Cached && CacheResets == false) {
                return Result;
            }
            var task = default(Task<TResult>);
            lock (Locker) {
                if (Cached) {
                    return Result;
                }
                var t = task = Task;
                if (t is null) {
                    task = Task = Factory(token) ??
                        throw new InvalidOperationException("The returned task from the factory is null.");
                }
            }
            var result = default(TResult);
            try {
                result = await task.ConfigureAwait(ContinueOnCapturedContext);
            }
            catch {
                lock (Locker) {
                    Task = null;
                    throw;
                }
            }
            lock (Locker) {
                if (Cached) {
                    return Result;
                }
                Cached = true;
                Result = result;
            }
            return result;
        }

        /// <summary>
        /// An implementation of <see cref="TaskCache{TResult}"/> that allows resetting state.
        /// </summary>
        public class WithRefresh : TaskCache<TResult> {
            /// <summary>
            /// Creates a new instance of a <see cref="TaskCache{TResult}"/> that allows resetting state.
            /// </summary>
            /// <param name="continueOnCapturedContext">Whether or not to continue awaited tasks on the captured context.</param>
            /// <param name="factory">The callback used to create the instance of <see cref="System.Threading.Tasks.Task"/> whose result is cached.</param>
            public WithRefresh(bool continueOnCapturedContext, Func<CancellationToken, Task<TResult>> factory) : base(continueOnCapturedContext, factory) {
                CacheResets = true;
            }

            /// <summary>
            /// Creates a new instance of a <see cref="TaskCache{TResult}"/> that allows resetting state.
            /// </summary>
            /// <param name="factory">The callback used to create the instance of <see cref="System.Threading.Tasks.Task"/> whose result is cached.</param>
            public WithRefresh(Func<CancellationToken, Task<TResult>> factory) : this(false, factory) {
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
            public async Task<TResult> Refreshed(CancellationToken token) {
                await Refresh(token).ConfigureAwait(ContinueOnCapturedContext);
                return await Ready(token).ConfigureAwait(ContinueOnCapturedContext);
            }

            /// <summary>
            /// Clears the cache, meaning the next call to <see cref="Ready"/> or <see cref="Refreshed"/> 
            /// will call the <see cref="Factory"/> callback again.
            /// </summary>
            public TASK Refresh(CancellationToken token) {
                lock (Locker) {
                    Task = null;
                    Cached = false;
                    Result = default;
                }
                return CompletedTask;
            }
        }
    }
}
