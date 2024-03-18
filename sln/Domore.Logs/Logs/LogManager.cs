using Domore.Threading;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Domore.Logs {
    internal sealed class LogManager : IDisposable {
        private readonly BackgroundQueue Queue = new();
        private readonly LogSubscriptionCollection Subscriptions = new();
        private readonly ConcurrentDictionary<string, LogServiceProxy> Set = new();
        private readonly ConcurrentDictionary<string, LogSeverity> TypeThreshold = new();
        private LogSeverity DefaultThreshold;

        private void Dispose(bool disposing) {
            if (disposing) {
                Queue.Dispose();
            }
        }

        private void SetThresholdChanged() {
            lock (Set) {
                var names = Set.SelectMany(item => item.Value.Config.Names).Distinct();
                foreach (var name in names) {
                    var severity = TypeThreshold[name] = Set
                        .Select(item => item.Value)
                        .Select(log => log.Config[name].Threshold)
                        .Where(sev => sev.HasValue)
                        .Select(sev => sev.Value)
                        .Where(sev => sev != LogSeverity.None)
                        .OrderBy(sev => sev)
                        .FirstOrDefault();
                    if (severity == LogSeverity.None) {
                        TypeThreshold.TryRemove(name, out _);
                    }
                }
                DefaultThreshold = Set
                    .Select(item => item.Value)
                    .Select(log => log.Config.Default.Threshold)
                    .Where(sev => sev.HasValue)
                    .Select(sev => sev.Value)
                    .Where(sev => sev != LogSeverity.None)
                    .OrderBy(sev => sev)
                    .FirstOrDefault();
            }
        }

        private void Config_DefaultThresholdChanged(object sender, LogTypeThresholdChangedEventArgs e) {
            SetThresholdChanged();
        }

        private void Config_TypeThresholdChanged(object sender, LogTypeThresholdChangedEventArgs e) {
            SetThresholdChanged();
        }

        public event LogEventHandler LogEvent;

        public LogFormatter Formatter { get; } = new LogFormatter();

        public LogSeverity LogEventThreshold { get; set; }

        public LogServiceProxy this[string name] {
            get {
                lock (Set) {
                    if (Set.TryGetValue(name, out var value) == false) {
                        Set[name] = value = new LogServiceProxy(name);
                        Set[name].Config.TypeThresholdChanged += Config_TypeThresholdChanged;
                        Set[name].Config.DefaultThresholdChanged += Config_DefaultThresholdChanged;
                    }
                    return value;
                }
            }
        }

        public void Add(LogSubscriptionProxy subscription) {
            Subscriptions.Add(subscription);
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
            if (TypeThreshold.Count > 0) {
                if (TypeThreshold.TryGetValue(type.Name, out var value)) {
                    return value != LogSeverity.None && value <= severity;
                }
            }
            return DefaultThreshold != LogSeverity.None && DefaultThreshold <= severity;
        }

        public void Log(LogSeverity severity, Type type, object[] data) {
            if (data != null) {
                var entry = new LogEntry(
                    logType: type,
                    logDate: DateTime.UtcNow,
                    logSeverity: severity,
                    logList: Formatter.Format(data));
                if (LogEventThreshold != LogSeverity.None && LogEventThreshold <= severity) {
                    LogEvent?.Invoke(this, new LogEventArgs(entry));
                }
                if (Subscriptions.Count > 0) {
                    Subscriptions.Send(entry);
                }
                Queue.Add(() => {
                    lock (Set) {
                        foreach (var item in Set) {
                            item.Value.Log(entry);
                        }
                    }
                });
            }
        }

        public void Complete() {
            Queue.Complete();
            lock (Set) {
                foreach (var item in Set) {
                    item.Value.Complete();
                }
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
}
