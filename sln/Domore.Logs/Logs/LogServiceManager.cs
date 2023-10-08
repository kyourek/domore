using Domore.Threading;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Domore.Logs {
    internal sealed class LogServiceManager : IDisposable {
        private readonly BackgroundQueue Queue = new BackgroundQueue();
        private readonly LogServiceFactory Factory = new LogServiceFactory();
        private readonly ConcurrentDictionary<string, LogServiceProxy> Set = new ConcurrentDictionary<string, LogServiceProxy>();
        private readonly ConcurrentDictionary<string, LogSeverity> TypeSeverity = new ConcurrentDictionary<string, LogSeverity>();
        private LogSeverity DefaultSeverity;

        private void Dispose(bool disposing) {
            if (disposing) {
                Queue.Dispose();
            }
        }

        private void SetSeverityChanged() {
            lock (Set) {
                var names = Set.SelectMany(item => item.Value.Config.Names).Distinct();
                foreach (var name in names) {
                    var severity = TypeSeverity[name] = Set
                        .Select(item => item.Value)
                        .Select(log => log.Config[name].Severity)
                        .Where(sev => sev.HasValue)
                        .Select(sev => sev.Value)
                        .Where(sev => sev != LogSeverity.None)
                        .OrderBy(sev => sev)
                        .FirstOrDefault();
                    if (severity == LogSeverity.None) {
                        TypeSeverity.TryRemove(name, out _);
                    }
                }
                DefaultSeverity = Set
                    .Select(item => item.Value)
                    .Select(log => log.Config.Default.Severity)
                    .Where(sev => sev.HasValue)
                    .Select(sev => sev.Value)
                    .Where(sev => sev != LogSeverity.None)
                    .OrderBy(sev => sev)
                    .FirstOrDefault();
            }
        }

        private void Config_DefaultSeverityChanged(object sender, LogTypeSeverityChangedEventArgs e) {
            SetSeverityChanged();
        }

        private void Config_TypeSeverityChanged(object sender, LogTypeSeverityChangedEventArgs e) {
            SetSeverityChanged();
        }

        public LogServiceProxy this[string name] {
            get {
                lock (Set) {
                    if (Set.TryGetValue(name, out var value) == false) {
                        Set[name] = value = new LogServiceProxy(name);
                        Set[name].Config.TypeSeverityChanged += Config_TypeSeverityChanged;
                        Set[name].Config.DefaultSeverityChanged += Config_DefaultSeverityChanged;
                    }
                    return value;
                }
            }
        }

        public bool Log(LogSeverity severity, Type type) {
            if (type == null) {
                return false;
            }
            if (severity == LogSeverity.None) {
                return false;
            }
            if (TypeSeverity.Count > 0) {
                if (TypeSeverity.TryGetValue(type.Name, out var value)) {
                    return value != LogSeverity.None && value <= severity;
                }
            }
            return DefaultSeverity != LogSeverity.None && DefaultSeverity <= severity;
        }

        public void Log(LogSeverity severity, Type type, object[] data) {
            if (data  != null) {
                var entry = new LogEntry(
                    logType: type,
                    logSeverity: severity,
                    logList: data
                        .Select(d => $"{d}")
                        .ToArray());
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
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LogServiceManager() {
            Dispose(false);
        }
    }
}
