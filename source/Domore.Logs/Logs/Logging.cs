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
    private static readonly object CompleteLocker = new();

    [ThreadStatic]
    private static LogManager UsedManager;
    private static readonly Logging Instance = new();

    private readonly List<LogManager> CompletedManagers = [];
    private readonly Dictionary<LogManager, int> UseManagerCount = [];
    private readonly HashSet<LogManager> DeferredCompletions = [];

    private LogManager Manager;

    private Logging() {
    }

    private LogManager GetManager() {
        lock (ManagerLocker) {
            return GetManagerUnsafe();
        }
    }

    private LogManager GetManagerUnsafe() =>
        Manager ??= new LogManager();

    private T UseManager<T>(Func<LogManager, T> action) {
        LogManager manager;
        lock (ManagerLocker) {
            manager = GetManagerUnsafe();
            UseManagerCount.TryGetValue(manager, out var count);
            UseManagerCount[manager] = count + 1;
        }
        var previousManager = UsedManager;
        UsedManager = manager;
        try {
            return action(manager);
        }
        finally {
            UsedManager = previousManager;
            lock (ManagerLocker) {
                var count = UseManagerCount[manager] - 1;
                if (count == 0) {
                    UseManagerCount.Remove(manager);
                    Monitor.PulseAll(ManagerLocker);
                }
                else {
                    UseManagerCount[manager] = count;
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

    internal bool Log(Logger logger, LogSeverity severity) {
        return UseManager(manager => manager.Log(severity, logger?.Type));
    }

    internal void Log(Logger logger, LogSeverity severity, params object[] data) {
        UseManager(manager => manager.Log(severity, logger?.Type, data));
    }

    internal static void Notify(object obj) {
        try { Debug.WriteLine(obj); } catch { }
        try { Trace.WriteLine(obj); } catch { }
        try { Console.WriteLine(obj); } catch { }
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
    /// Completes all logging.
    /// </summary>
    public static void Complete() {
        LogManager manager;
        bool defer;
        lock (ManagerLocker) {
            manager = UsedManager;
            if (manager is null) {
                foreach (var completed in Instance.CompletedManagers) {
                    if (completed.ThreadIsCurrentThread) {
                        manager = completed;
                        break;
                    }
                }
                manager ??= Instance.Manager;
            }
            // A callback on a retiring manager must not complete a later session.
            if (manager is null || manager != Instance.Manager) {
                return;
            }
            defer = UsedManager is not null || manager.ThreadIsCurrentThread;
            if (defer && Instance.DeferredCompletions.Add(manager) == false) {
                return;
            }
        }
        if (defer) {
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    Complete(manager);
                }
                catch (Exception ex) {
                    Notify(ex);
                }
                finally {
                    lock (ManagerLocker) {
                        Instance.DeferredCompletions.Remove(manager);
                    }
                }
            });
            return;
        }
        Complete(manager);
    }

    private static void Complete(LogManager manager) {
        lock (CompleteLocker) {
            lock (ManagerLocker) {
                if (manager != Instance.Manager) {
                    return;
                }
                Instance.Manager = null;
                Instance.CompletedManagers.Add(manager);
                while (Instance.UseManagerCount.ContainsKey(manager)) {
                    Monitor.Wait(ManagerLocker);
                }
            }
            try {
                using (manager) {
                    manager.Complete();
                }
            }
            finally {
                lock (ManagerLocker) {
                    Instance.CompletedManagers.Remove(manager);
                }
            }
        }
    }
}
