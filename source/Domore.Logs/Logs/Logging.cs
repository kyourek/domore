using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Domore.Logs; 
/// <summary>
/// Provides implementations of <see cref="ILog"/>.
/// </summary>
public sealed class Logging {
    static Logging() {
        try {
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        }
        catch (Exception ex) {
            Notify(ex);
        }
    }

    private static readonly TimeSpan DefaultCompleteTimeout = TimeSpan.FromSeconds(5);
    private static readonly object CompleteLocker = new();
    private static readonly Logging Instance = new();

    private LogManager Manager {
        get {
            var manager = Interlocked.CompareExchange(ref _Manager, null, null);
            if (manager != null) {
                return manager;
            }

            var created = new LogManager();
            return Interlocked.CompareExchange(ref _Manager, created, null) ?? created;
        }
    }
    private LogManager _Manager;

    private Logging() {
    }

    private static void ProcessExit(object sender, EventArgs e) {
        try {
            Complete();
        }
        catch (Exception ex) {
            Notify(ex);
        }
    }

    private static void NotifyCompleted() {
        var completed = Completed;
        if (completed == null) {
            return;
        }
        foreach (EventHandler handler in completed.GetInvocationList()) {
            try {
                handler(null, EventArgs.Empty);
            }
            catch (Exception ex) {
                Notify(ex);
            }
        }
    }

    internal static event EventHandler Completed;

    internal bool Log(Logger logger, LogSeverity severity) {
        try {
            return Manager.Log(severity, logger?.Type);
        }
        catch (Exception ex) {
            Notify(ex);
            return false;
        }
    }

    internal void Log(Logger logger, LogSeverity severity, params object[] data) {
        try {
            Manager.Log(severity, logger?.Type, data);
        }
        catch (Exception ex) {
            Notify(ex);
        }
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
        add => Instance.Manager.LogEvent += value;
        remove => Instance.Manager.LogEvent -= value;
    }

    /// <summary>
    /// Gets or sets the threshold for log events.
    /// </summary>
    public static LogSeverity EventThreshold {
        get => Instance.Manager.LogEventThreshold;
        set => Instance.Manager.LogEventThreshold = value;
    }

    /// <summary>
    /// Adds a subscription to log events.
    /// </summary>
    /// <param name="subscription">The subscription to be added.</param>
    /// <returns>True if the subscription was added. Otherwise, false.</returns>
    public static bool Subscribe(ILogSubscription subscription) {
        return Instance.Manager.Subscribe(subscription);
    }

    /// <summary>
    /// Removes a subscription to log events.
    /// </summary>
    /// <param name="subscription">The subscription to be removed.</param>
    /// <returns>True if the subscription was removed. Otherwise, false.</returns>
    public static bool Unsubscribe(ILogSubscription subscription) {
        return Instance.Manager.Unsubscribe(subscription);
    }

    /// <summary>
    /// Gets an object that may be used to configure log behavior.
    /// </summary>
    public static object Config =>
        new { Log = Instance.Manager };

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
        Instance.Manager.Formatter.Format(type, toString);
    }

    /// <summary>
    /// Completes all logging, waiting up to five seconds for queued service calls.
    /// </summary>
    public static void Complete() {
        Complete(DefaultCompleteTimeout);
    }

    /// <summary>
    /// Completes all logging, waiting up to the specified timeout for queued service calls to drain.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for the service queue to drain.</param>
    /// <returns>
    /// <see langword="true"/> if the service queue drained within the timeout; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool Complete(TimeSpan timeout) {
        lock (CompleteLocker) {
            var manager = Interlocked.Exchange(ref Instance._Manager, null);
            if (manager == null) {
                return true;
            }

            var queueDrained = false;
            try {
                queueDrained = manager.Complete(timeout);
            }
            catch (Exception ex) {
                Notify(ex);
            }
            finally {
                try {
                    manager.Dispose();
                }
                catch (Exception ex) {
                    Notify(ex);
                }
                NotifyCompleted();
            }
            return queueDrained;
        }
    }
}
