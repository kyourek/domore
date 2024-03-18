using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    internal sealed class LogSubscriptionCollection {
        private readonly object Locker = new();
        private readonly HashSet<LogSubscriptionProxy> Set = [];
        private readonly Dictionary<Type, LogSeverity> ThresholdCache = [];
        private readonly Dictionary<ILogSubscription, LogSubscriptionProxy> Lookup = [];

        private void Item_ThresholdChanged(object sender, EventArgs e) {
            lock (Locker) {
                ThresholdCache.Clear();
            }
        }

        private LogSeverity Threshold(Type type) {
            if (type == null) {
                return LogSeverity.None;
            }
            lock (Locker) {
                if (ThresholdCache.TryGetValue(type, out var severity) == false) {
                    ThresholdCache[type] = severity = Set
                        .Select(item => item.Threshold(type))
                        .Where(s => s != LogSeverity.None)
                        .OrderBy(s => s)
                        .FirstOrDefault();
                }
                return severity;
            }
        }

        public int Count =>
            Set.Count;

        public void Complete() {
        }

        public bool Add(ILogSubscription item) {
            if (item == null) {
                return false;
            }
            lock (Locker) {
                if (Lookup.TryGetValue(item, out var proxy) == false) {
                    Lookup[item] = proxy = new LogSubscriptionProxy(item);
                    Set.Add(proxy);
                    ThresholdCache.Clear();
                    proxy.ThresholdChanged += Item_ThresholdChanged;
                    return true;
                }
            }
            return false;
        }

        public bool Remove(ILogSubscription item) {
            if (item == null) {
                return false;
            }
            lock (Locker) {
                if (Lookup.TryGetValue(item, out var proxy)) {
                    Lookup.Remove(item);
                    Set.Remove(proxy);
                    ThresholdCache.Clear();
                    proxy.ThresholdChanged -= Item_ThresholdChanged;
                    return true;
                }
            }
            return false;
        }

        public bool Send(LogSeverity severity, Type type) {
            if (severity == LogSeverity.None) {
                return false;
            }
            if (Count == 0) {
                return false;
            }
            var threshold = Threshold(type);
            if (threshold == LogSeverity.None) {
                return false;
            }
            return threshold <= severity;
        }

        public void Send(LogEntry entry) {
            lock (Locker) {
                foreach (var item in Set) {
                    if (item != null) {
                        item.Receive(entry);
                    }
                }
            }
        }
    }
}
