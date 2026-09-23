using System;
using System.Threading;
using System.Threading.Tasks;
using TASK = System.Threading.Tasks.Task;

namespace Domore.Threading.Tasks;

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

    private volatile Box Cache;
    private volatile int Generation;
    private Task<TResult> Task;

    /// <summary>
    /// Holds the cached result so that it can be published with a single volatile reference write.
    /// </summary>
    private sealed class Box {
        public TResult Value { get; }

        public Box(TResult value) {
            Value = value;
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
    public TResult Result => Cache is Box box ? box.Value : default;

    /// <summary>
    /// Gets the callback used to create the instance of the task whose result is cached.
    /// </summary>
    public Func<CancellationToken, Task<TResult>> Factory { get; }

    /// <summary>
    /// Creates a new instance of a task cache.
    /// </summary>
    /// <param name="continueOnCapturedContext">Whether or not to continue awaited tasks on the captured context.</param>
    /// <param name="factory">The callback used to create the instance of the task whose result is cached.</param>
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
    /// <param name="token">
    /// The cancellation token passed to the <see cref="Factory"/> callback. Canceling this token cancels the underlying
    /// operation itself, and the canceled task is discarded so that the next call invokes the callback again.
    /// </param>
    /// <remarks>
    /// An instance is expected to be private to a single consumer. The task returned from the <see cref="Factory"/> callback
    /// is created with the token of the caller that starts it and is shared by any callers that arrive while it is still
    /// running, so canceling that first token cancels the operation for all of them. Callers that arrive later observe the
    /// resulting <see cref="OperationCanceledException"/> even though their own tokens were not canceled.
    /// </remarks>
    /// <returns>
    /// The cached instance of <see cref="System.Threading.Tasks.Task"/> that was the result of the first successful completion 
    /// of the task returned from the <see cref="Factory"/> callback, or the most recent instance returned from the callback if
    /// the task completes with a fault or cancellation.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Factory"/> callback returns null.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the underlying operation is canceled.</exception>
    public async Task<TResult> Ready(CancellationToken token) {
        var cache = Cache;
        if (cache is not null) {
            return cache.Value;
        }
        var task = default(Task<TResult>);
        var generation = 0;
        lock (Locker) {
            cache = Cache;
            if (cache is not null) {
                return cache.Value;
            }
            generation = Generation;
            task = Task ??= Factory(token) ??
                throw new InvalidOperationException("The returned task from the factory is null.");
        }
        var result = default(TResult);
        try {
            result = await task.ConfigureAwait(ContinueOnCapturedContext);
        }
        catch {
            lock (Locker) {
                if (ReferenceEquals(Task, task)) {
                    Task = null;
                }
            }
            throw;
        }
        lock (Locker) {
            cache = Cache;
            if (cache is not null) {
                return cache.Value;
            }
            if (Generation == generation) {
                Cache = new Box(result);
            }
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
                Cache = null;
                Generation++;
            }
            return CompletedTask;
        }
    }
}
