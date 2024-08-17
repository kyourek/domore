using System;
using System.Threading;
using System.Threading.Tasks;
using TASK = System.Threading.Tasks.Task;

namespace Domore.Threading.Tasks {
    /// <summary>
    /// Caches the result of a <see cref="System.Threading.Tasks.Task"/> upon its first successful completion.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the task.</typeparam>
    public class TaskCache<TResult> {
        private readonly SemaphoreSlim Locker = new(1, 1);

        private bool Cached;
        private bool CacheResets;
        private Task<TResult> Task;

        private TASK Wait(CancellationToken token) {
#if NET40
            static TASK canceled() {
                var source = new TaskCompletionSource<object>();
                source.SetCanceled();
                return source.Task;
            }
            if (token.IsCancellationRequested) {
                return canceled();
            }
            var taskSource = new TaskCompletionSource<bool>();
            var workQueued = ThreadPool.QueueUserWorkItem(_ => {
                try {
                    Locker.Wait(token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) {
                    taskSource.SetCanceled();
                }
                catch (Exception ex) {
                    taskSource.SetException(ex);
                }
                finally {
                    taskSource.TrySetResult(true);
                }
            });
            if (workQueued == false) {
                throw new InvalidOperationException("Thread pool work could not be queued.");
            }
            return taskSource.Task;
#else
            return Locker.WaitAsync(token);
#endif
        }

        private void Release() {
            Locker.Release();
        }

        private async Task<T> Lock<T>(Func<CancellationToken, Task<T>> task, CancellationToken token) {
            if (null == task) throw new ArgumentNullException(nameof(task));
            await Wait(token).ConfigureAwait(ContinueOnCapturedContext);
            try {
                return await task(token).ConfigureAwait(ContinueOnCapturedContext);
            }
            finally {
                Release();
            }
        }

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
            if (Cached && !CacheResets) {
                return Result;
            }
            async Task<TResult> ready(CancellationToken token) {
                if (Cached) {
                    return Result;
                }
                if (Task == null) {
                    Task = Factory(token) ?? throw new InvalidOperationException("The returned task from the factory is null.");
                }
                try {
                    Result = await Task.ConfigureAwait(ContinueOnCapturedContext);
                }
                catch {
                    Task = null;
                    throw;
                }
                Cached = true;
                return Result;
            }
            return await Lock(ready, token).ConfigureAwait(ContinueOnCapturedContext);
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
            /// Clears the cache, meaning the next call to <see cref="Ready"/> or <see cref="Refreshed"/> will call the <see cref="Factory"/> callback again.
            /// </summary>
            public TASK Refresh(CancellationToken token) {
                return Lock(token: token, task: _ => {
                    Task = null;
                    Cached = false;
                    Result = default;
#if NET40
                    static Task<object> complete() {
                        var source = new TaskCompletionSource<object>();
                        source.SetResult(default);
                        return source.Task;
                    }
                    return complete();
#else
                    return TASK.FromResult(default(object));
#endif
                });
            }
        }
    }
}
