using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Domore.Logs;

/// <summary>
/// Provides implementations of <see cref="ILog"/>.
/// </summary>
public sealed class Logging {
    private static readonly object ManagerLocker = new();
    private static readonly TimeSpan InfiniteTimeout = TimeSpan.FromMilliseconds(-1);

    [ThreadStatic]
    private static LogManager UsedManager;
    private static readonly Logging Instance = new();

    private readonly Dictionary<LogManager, int> UseManagerCount = [];
    private readonly Dictionary<LogManager, Retirement> RetiringManagers = [];

    private LogManager Manager;

    private Logging() {
    }

    static Logging() {
        AppDomain.CurrentDomain.ProcessExit += ProcessExit;
    }

    private sealed class Retirement {
        public readonly object Locker = new();
        public LogManager Manager;
        public Exception Error;
        public int Started;
        public Thread Worker;
        public bool IsCompleted;

        public Retirement(LogManager manager) {
            Manager = manager;
        }
    }

    private LogManager GetManager() {
        lock (ManagerLocker) {
            return GetManagerUnsafe();
        }
    }

    private LogManager GetManagerUnsafe() =>
        Manager ??= new LogManager();

    private T UseManager<T>(Func<LogManager, T> action) {
        LogManager manager = null;
        var previousManager = UsedManager;
        var leased = false;
        try {
            lock (ManagerLocker) {
                manager = GetManagerUnsafe();
                UseManagerCount.TryGetValue(manager, out var count);
                UseManagerCount[manager] = count + 1;
                leased = true;
            }
            UsedManager = manager;
            using (LogCallbackGuard.EnterManager(manager)) {
                return action(manager);
            }
        }
        finally {
            UsedManager = previousManager;
            if (leased) {
                lock (ManagerLocker) {
                    if (UseManagerCount.TryGetValue(manager, out var count)) {
                        if (count <= 1) {
                            UseManagerCount.Remove(manager);
                        }
                        else {
                            UseManagerCount[manager] = count - 1;
                        }
                    }
                    Monitor.PulseAll(ManagerLocker);
                }
            }
        }
    }

    private void UseManager(Action<LogManager> action) {
        UseManager(manager => {
            action(manager);
            return 0;
        });
    }

    internal static void Configure(Action<object> configure) {
        if (configure is null) {
            throw new ArgumentNullException(nameof(configure));
        }
        Instance.UseManager(manager => configure(new { Log = manager }));
    }

    internal bool Log(Logger logger, LogSeverity severity) {
        try {
            if (LogCallbackGuard.IsActive) {
                return false;
            }
            var type = logger?.Type;
            if (type is null) {
                return false;
            }
            return UseManager(manager => manager.Log(severity, type));
        }
        catch (Exception ex) {
            Notify(ex);
            return false;
        }
    }

    internal void Log(Logger logger, LogSeverity severity, params object[] data) {
        try {
            if (LogCallbackGuard.IsActive) {
                return;
            }
            var type = logger?.Type;
            if (type is null) {
                return;
            }
            UseManager(manager => manager.Log(severity, type, data));
        }
        catch (Exception ex) {
            Notify(ex);
        }
    }

    internal static void Notify(object obj) {
        try {
            if (LogCallbackGuard.IsDiagnosing) {
                return;
            }
            using (LogCallbackGuard.EnterDiagnostic()) {
                try {
                    Console.Error.WriteLine(obj);
                }
                catch {
                    // Diagnostic output must not interfere with logging.
                }
            }
        }
        catch {
            // Guard setup and diagnostic output are both best-effort.
        }
    }

    internal static void CompleteService(LogManager manager, ILogService service) {
        // Replacement can invoke this on a configuration thread while holding a proxy lock.
        // Preserve the owner so reentrant shutdown defers and cannot retire a later session.
        var previousManager = UsedManager;
        UsedManager = manager ?? previousManager;
        try {
            using (LogCallbackGuard.EnterManager(manager))
            using (LogCallbackGuard.Enter()) {
                service?.Complete();
            }
        }
        finally {
            UsedManager = previousManager;
        }
    }

    /// <summary>
    /// Raised when a log event occurs.
    /// </summary>
    public static event LogEventHandler Event {
        add => Instance.UseManager(manager => manager.LogEvent += value);
        remove => Instance.UseManager(manager => manager.LogEvent -= value);
    }

    /// <summary>
    /// Gets or sets the threshold for log events.
    /// </summary>
    public static LogSeverity EventThreshold {
        get => Instance.UseManager(manager => manager.LogEventThreshold);
        set => Instance.UseManager((Action<LogManager>)(manager => manager.LogEventThreshold = value));
    }

    /// <summary>
    /// Adds a subscription to log events.
    /// </summary>
    /// <param name="subscription">The subscription to be added.</param>
    /// <returns>True if the subscription was added. Otherwise, false.</returns>
    public static bool Subscribe(ILogSubscription subscription) {
        return Instance.UseManager(manager => manager.Subscribe(subscription));
    }

    /// <summary>
    /// Removes a subscription to log events.
    /// </summary>
    /// <param name="subscription">The subscription to be removed.</param>
    /// <returns>True if the subscription was removed. Otherwise, false.</returns>
    public static bool Unsubscribe(ILogSubscription subscription) {
        return Instance.UseManager(manager => manager.Unsubscribe(subscription));
    }

    /// <summary>
    /// Gets an object that may be used to configure log behavior.
    /// </summary>
    public static object Config =>
        new { Log = Instance.GetManager() };

    /// <summary>
    /// Gets an instance of <see cref="ILog"/> for the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type whose log is returned.</param>
    /// <returns>The instance of <see cref="ILog"/> for the <paramref name="type"/>.</returns>
    public static ILog For(Type type) {
        return new Logger(type, Instance);
    }

    /// <summary>
    /// Provides a callback used to format instances of <paramref name="type"/> in log messages.
    /// </summary>
    /// <param name="type">The type of instances to be formatted.</param>
    /// <param name="toString">The callback called to format instances of <paramref name="type"/>.</param>
    public static void Format(Type type, Func<object, string[]> toString) {
        Instance.UseManager(manager => manager.Formatter.Format(type, toString));
    }

    /// <summary>
    /// Gets or sets the maximum number of items expanded from an enumerable log argument.
    /// The default is 100. Values must be positive.
    /// </summary>
    public static int EnumerableItemLimit {
        get => Instance.UseManager(manager => manager.Formatter.EnumerableItemLimit);
        set => Instance.UseManager(manager => manager.Formatter.EnumerableItemLimit = value);
    }

    /// <summary>
    /// Completes all logging.
    /// </summary>
    public static void Complete() {
        Instance.CompleteCore(InfiniteTimeout);
    }

    /// <summary>
    /// Completes all logging, waiting up to <paramref name="timeout"/> for pending
    /// manager retirements. Retirements continue safely in the background after a
    /// timeout.
    /// </summary>
    /// <param name="timeout">A nonnegative timeout, or -1 millisecond for an infinite wait.</param>
    /// <returns>True if all retirements completed before this call returned.</returns>
    public static bool Complete(TimeSpan timeout) {
        if (timeout < TimeSpan.Zero && timeout != InfiniteTimeout) {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
        return Instance.CompleteCore(timeout);
    }

    private static void ProcessExit(object sender, EventArgs e) {
        try {
            Instance.CompleteCore(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex) {
            // Do not let an arbitrary Console.Error writer extend the process-exit
            // budget. Report asynchronously once the bounded shutdown has returned.
            try {
                ThreadPool.QueueUserWorkItem(_ => Notify(ex));
            }
            catch {
                // Process shutdown is already in progress.
            }
        }
    }

    private bool CompleteCore(TimeSpan timeout) {
        var stopwatch = Stopwatch.StartNew();
        var records = new List<Retirement>();
        var deferWait = false;

        lock (ManagerLocker) {
            var owner = UsedManager ?? LogCallbackGuard.CurrentManager;
            var current = Manager;

            // Flowed manager context identifies callbacks and child tasks even when
            // ThreadStatic UsedManager is absent and callback depth has unwound.
            if (owner is not null && owner != current) {
                return false;
            }
            if (LogCallbackGuard.IsActive && owner is null) {
                return false;
            }

            deferWait = owner is not null || LogCallbackGuard.IsActive || current?.ThreadIsCurrentThread == true;

            foreach (var retirement in RetiringManagers.Values) {
                records.Add(retirement);
            }

            if (current is not null) {
                Manager = null;
                var retirement = new Retirement(current);
                RetiringManagers.Add(current, retirement);
                records.Add(retirement);
            }
        }

        foreach (var retirement in records) {
            StartRetirement(retirement);
        }

        if (deferWait) {
            return false;
        }
        if (WaitForRetirements(records, timeout, stopwatch) == false) {
            return false;
        }

        var errors = new List<Exception>();
        foreach (var retirement in records) {
            if (retirement.Error is AggregateException aggregate) {
                errors.AddRange(aggregate.Flatten().InnerExceptions);
            }
            else if (retirement.Error is not null) {
                errors.Add(retirement.Error);
            }
        }
        if (errors.Count > 0) {
            throw new AggregateException("One or more logging managers failed to complete.", errors);
        }
        return true;
    }

    private static bool WaitForRetirements(List<Retirement> records, TimeSpan timeout, Stopwatch stopwatch) {
        var infinite = timeout == InfiniteTimeout;
        foreach (var retirement in records) {
            lock (retirement.Locker) {
                while (retirement.IsCompleted == false) {
                    if (infinite) {
                        try {
                            Monitor.Wait(retirement.Locker);
                        }
                        catch (ThreadInterruptedException) {
                            // Interruption does not cancel an explicit full flush.
                        }
                        continue;
                    }

                    var remaining = timeout - stopwatch.Elapsed;
                    if (remaining <= TimeSpan.Zero) {
                        return false;
                    }

                    var milliseconds = remaining.TotalMilliseconds >= int.MaxValue
                        ? int.MaxValue
                        : Math.Max(1, (int)Math.Ceiling(remaining.TotalMilliseconds));
                    try {
                        Monitor.Wait(retirement.Locker, milliseconds);
                    }
                    catch (ThreadInterruptedException) {
                        // Preserve the original deadline across interrupted waits.
                    }
                    if (retirement.IsCompleted == false && stopwatch.Elapsed >= timeout) {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private void StartRetirement(Retirement retirement) {
        if (Interlocked.CompareExchange(ref retirement.Started, 1, 0) != 0) {
            return;
        }

        Thread thread = null;
        try {
            thread = new Thread(() => Retire(retirement)) {
                Name = "Logging retirement",
                IsBackground = true
            };
            retirement.Worker = thread;
            thread.Start();
            return;
        }
        catch (Exception startError) {
            if (thread?.IsAlive == true) {
                return;
            }
            retirement.Worker = null;
            try {
                if (ThreadPool.QueueUserWorkItem(_ => Retire(retirement))) {
                    return;
                }
            }
            catch {
                // Leave the record pending so a later completion can retry startup.
            }

            Interlocked.Exchange(ref retirement.Started, 0);
            throw new InvalidOperationException("Could not start safe logging retirement.", startError);
        }
    }

    private void Retire(Retirement retirement) {
        var manager = retirement.Manager;
        var errors = new List<Exception>();
        Exception retryFailure = null;
        var retryFailureReported = false;
        var completionAttempted = false;
        while (true) {
            var disposed = false;
            try {
                using (LogCallbackGuard.EnterManager(manager)) {
                    WaitForManagerUses(manager);
                    if (completionAttempted == false) {
                        completionAttempted = true;
                        try {
                            manager.Complete();
                        }
                        catch (Exception ex) {
                            errors.Add(ex);
                        }
                    }
                    try {
                        manager.Dispose();
                        disposed = true;
                    }
                    catch (Exception ex) {
                        retryFailure ??= ex;
                    }
                }
            }
            catch (Exception ex) {
                // Keep the retirement record live and retry. In particular, never
                // proceed to disposal if manager ownership or lease inspection failed.
                retryFailure ??= ex;
            }

            if (disposed) {
                try {
                    var allErrors = new List<Exception>(errors);
                    if (retryFailure is not null) {
                        allErrors.Add(retryFailure);
                    }
                    if (allErrors.Count == 1) {
                        retirement.Error = allErrors[0];
                    }
                    else if (allErrors.Count > 1) {
                        retirement.Error = new AggregateException("One or more logging components failed during retirement.", allErrors);
                    }
                    lock (ManagerLocker) {
                        RetiringManagers.Remove(manager);
                        retirement.Manager = null;
                        retirement.Worker = null;
                    }
                    lock (retirement.Locker) {
                        retirement.IsCompleted = true;
                        Monitor.PulseAll(retirement.Locker);
                    }
                }
                catch (Exception ex) {
                    retryFailure ??= ex;
                    disposed = false;
                }
                if (disposed) {
                    if (retirement.Error is not null) {
                        try {
                            using (LogCallbackGuard.EnterManager(manager)) {
                                Notify(retirement.Error);
                            }
                        }
                        catch { }
                    }
                    return;
                }
            }

            if (retryFailure is not null && retryFailureReported == false) {
                try {
                    using (LogCallbackGuard.EnterManager(manager)) {
                        Notify(retryFailure);
                    }
                }
                catch { }
                retryFailureReported = true;
            }
            try { Thread.Sleep(10); } catch { }
        }
    }

    private void WaitForManagerUses(LogManager manager) {
        while (true) {
            try {
                lock (ManagerLocker) {
                    if (UseManagerCount.ContainsKey(manager) == false) {
                        return;
                    }
                    Monitor.Wait(ManagerLocker);
                }
            }
            catch (ThreadInterruptedException) {
                // Retry. A lease must reach zero before any manager resource is closed.
            }
            catch (Exception ex) {
                try { Notify(ex); } catch { }
                try { Thread.Sleep(10); } catch { }
            }
        }
    }
}
