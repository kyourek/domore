using System;

namespace Domore.Logs {
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
            if (data != null) {
                var entry = new LogEntry(
                    logType: type,
                    entryDate: DateTime.UtcNow,
                    entrySeverity: severity,
                    entryList: Formatter.Format(data));
                if (LogEventThreshold != LogSeverity.None && LogEventThreshold <= severity) {
                    LogEvent?.Invoke(this, new LogEventArgs(entry));
                }
                if (Subscriptions.Count > 0) {
                    Subscriptions.Send(entry);
                }
                if (Services.Count > 0) {
                    Services.Send(entry);
                }
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
}
