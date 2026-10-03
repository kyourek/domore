using System;

namespace Domore.Logs;

internal sealed class LogManager : IDisposable {
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

    public bool ThreadIsCurrentThread =>
        Services.ThreadIsCurrentThread;

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
        if (data is null) {
            return;
        }
        var entry = new LogEntry(
            logType: type,
            entryDate: DateTime.UtcNow,
            entrySeverity: severity,
            entryList: Formatter.Format(data),
            /*
             * Format materializes a fresh array that belongs exclusively to this entry.
             */
            takeOwnership: true);
        var handlers = LogEvent;
        if (handlers is not null) {
            var thresholdMet = LogEventThreshold != LogSeverity.None && LogEventThreshold <= severity;
            if (thresholdMet) {
                var list = handlers.GetInvocationList();
                var args = list.Length > 0 ? new LogEventArgs(entry) : default;
                foreach (var item in list) {
                    if (item is LogEventHandler handler) {
                        try {
                            handler(this, args);
                        }
                        catch (Exception ex) {
                            Logging.Notify(ex);
                        }
                    }
                }
            }
        }
        if (Subscriptions.Count > 0) {
            Subscriptions.Send(entry);
        }
        if (Services.Count > 0) {
            Services.Send(entry);
        }
    }

    public void Complete() {
        LogEvent = null;
        var exceptions = new System.Collections.Generic.List<Exception>();
        try {
            Services.Complete();
        }
        catch (Exception ex) {
            exceptions.Add(ex);
        }
        try {
            Subscriptions.Complete();
        }
        catch (Exception ex) {
            exceptions.Add(ex);
        }
        try {
            Subscriptions.Clear();
        }
        catch (Exception ex) {
            exceptions.Add(ex);
        }
        if (exceptions.Count > 0) {
            throw new AggregateException("One or more logging components failed to complete.", exceptions);
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~LogManager() {
        Dispose(false);
    }
}
