using System;
using System.Threading;

namespace Domore.Logs; 
internal sealed class LogManager : IDisposable {
    [ThreadStatic]
    private static int _LogDepth;

    private readonly LogServiceCollection Services = new();
    private readonly LogSubscriptionCollection Subscriptions = new();

    private void Dispose(bool disposing) {
        if (disposing) {
            Services.Dispose();
        }
    }

    public event LogEventHandler LogEvent;

    public LogSeverity LogEventThreshold { get; set; }
    public LogFormatter Formatter { get; } = new LogFormatter();

    public LogServiceProxy this[string name] =>
        Services[name];

    public bool Subscribe(ILogSubscription subscription) {
        return Subscriptions.Add(subscription);
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
        if (LogEvent != null && LogEventThreshold != LogSeverity.None && LogEventThreshold <= severity) {
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
        if (type == null || severity == LogSeverity.None || data == null) {
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
                var logEvent = LogEvent;
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

    public void Complete() {
        LogEvent = null;
        Services.Complete();
        Subscriptions.Complete();
        Subscriptions.Clear();
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~LogManager() {
        Dispose(false);
    }
}
