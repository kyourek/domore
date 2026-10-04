using System;
using System.Threading;

namespace Domore.Logs; 
internal sealed class LogManager : IDisposable {
    [ThreadStatic]
    private static int _LogDepth;

    private readonly LogServiceCollection Services = new();
    private readonly LogSubscriptionCollection Subscriptions = new();
    private readonly object CompletionLocker = new();
    private bool _Completed;

    private event LogEventHandler _LogEvent;

    private LogEventHandler CurrentLogEvent {
        get {
            lock (CompletionLocker) {
                return _LogEvent;
            }
        }
    }

    public event LogEventHandler LogEvent {
        add {
            lock (CompletionLocker) {
                if (_Completed == false) {
                    _LogEvent += value;
                }
            }
        }
        remove {
            lock (CompletionLocker) {
                _LogEvent -= value;
            }
        }
    }

    public LogSeverity LogEventThreshold { get; set; }
    public LogFormatter Formatter { get; } = new LogFormatter();

    public LogServiceProxy this[string name] =>
        Services[name];

    public bool Subscribe(ILogSubscription subscription) {
        lock (CompletionLocker) {
            return _Completed == false && Subscriptions.Add(subscription);
        }
    }

    public bool Unsubscribe(ILogSubscription subscription) {
        return Subscriptions.Remove(subscription);
    }

    public bool Log(LogSeverity severity, Type type) {
        if (type == null) {
            return false;
        }
        if (severity == LogSeverity.None) {
            return false;
        }
        var logEvent = CurrentLogEvent;
        if (logEvent != null && LogEventThreshold != LogSeverity.None && LogEventThreshold <= severity) {
            return true;
        }
        if (Subscriptions.Count > 0) {
            if (Subscriptions.Send(severity, type)) {
                return true;
            }
        }
        if (Services.Count > 0) {
            if (Services.Send(severity, type)) {
                return true;
            }
        }
        return false;
    }

    public void Log(LogSeverity severity, Type type, object[] data) {
        if (type == null || severity == LogSeverity.None) {
            return;
        }
        if (Log(severity, type) == false) {
            return;
        }

        var depth = _LogDepth++;
        try {
            var entry = new LogEntry(
                logType: type,
                entryDate: DateTime.UtcNow,
                entrySeverity: severity,
                entryList: Formatter.Format(data));

            if (depth == 0) {
                var logEvent = CurrentLogEvent;
                if (LogEventThreshold != LogSeverity.None && LogEventThreshold <= severity && logEvent != null) {
                    var args = new LogEventArgs(entry);
                    foreach (LogEventHandler handler in logEvent.GetInvocationList()) {
                        try {
                            handler(this, args);
                        }
                        catch (Exception ex) {
                            Logging.Notify(ex);
                        }
                    }
                }
                if (Subscriptions.Count > 0) {
                    Subscriptions.Send(entry);
                }
            }

            /* Keep nested entries flowing to services while skipping synchronous callbacks. */
            if (Services.Count > 0) {
                Services.Send(entry);
            }
        }
        finally {
            _LogDepth--;
        }
    }

    public bool Complete(TimeSpan timeout) {
        lock (CompletionLocker) {
            _Completed = true;
            _LogEvent = null;
        }

        var queueDrained = false;
        try {
            queueDrained = Services.Complete(timeout);
        }
        catch (Exception ex) {
            Logging.Notify(ex);
        }
        try {
            Subscriptions.Complete();
        }
        catch (Exception ex) {
            Logging.Notify(ex);
        }
        try {
            Subscriptions.Clear();
        }
        catch (Exception ex) {
            Logging.Notify(ex);
        }
        return queueDrained;
    }

    public void Dispose() {
        Services.Dispose();
    }
}
