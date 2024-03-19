using Domore.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    internal sealed class LogServiceCollection : IDisposable {
        private readonly object Locker = new();
        private readonly BackgroundQueue Queue = new();
        private readonly Dictionary<string, LogServiceProxy> Set = [];
        private readonly Dictionary<string, LogSeverity> TypeThreshold = [];
        private LogSeverity DefaultThreshold;

        private void Dispose(bool disposing) {
            if (disposing) {
                Queue.Dispose();
            }
        }

        private void SetThresholdChanged() {
            lock (Locker) {
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
                        TypeThreshold.Remove(name);
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

        public LogServiceProxy this[string name] {
            get {
                lock (Locker) {
                    if (Set.TryGetValue(name, out var value) == false) {
                        Set[name] = value = new LogServiceProxy(name);
                        Set[name].Config.TypeThresholdChanged += Config_TypeThresholdChanged;
                        Set[name].Config.DefaultThresholdChanged += Config_DefaultThresholdChanged;
                    }
                    return value;
                }
            }
        }

        public int Count =>
            Set.Count;

        public bool Log(LogSeverity severity, Type type) {
            lock (Locker) {
                if (TypeThreshold.Count > 0) {
                    if (TypeThreshold.TryGetValue(type.Name, out var value)) {
                        return value != LogSeverity.None && value <= severity;
                    }
                }
                return DefaultThreshold != LogSeverity.None && DefaultThreshold <= severity;
            }
        }

        public void Log(LogEntry entry) {
            Queue.Add(() => {
                lock (Locker) {
                    foreach (var item in Set) {
                        item.Value.Log(entry);
                    }
                }
            });
        }

        public void Complete() {
            Queue.Complete();
            lock (Locker) {
                foreach (var item in Set) {
                    item.Value.Complete();
                }
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LogServiceCollection() {
            Dispose(false);
        }
    }
}
