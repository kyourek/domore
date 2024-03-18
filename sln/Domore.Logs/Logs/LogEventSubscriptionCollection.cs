using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    internal sealed class LogEventSubscriptionCollection {
        private readonly object Locker = new();
        private readonly HashSet<LogEventSubscription> Set = [];
        private readonly Dictionary<Type, LogSeverity> ThresholdCache = [];

        private void Item_SeverityChangedEvent(object sender, EventArgs e) {
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
                        .Select(item => item.InternalThreshold(type))
                        .Where(s => s != LogSeverity.None)
                        .OrderBy(s => s)
                        .FirstOrDefault();
                }
                return severity;
            }
        }

        public int Count =>
            Set.Count;

        public void Add(LogEventSubscription item) {
            lock (Locker) {
                var added = item != null && Set.Add(item);
                if (added) {
                    item.ThresholdChangedEvent += Item_SeverityChangedEvent;
                    ThresholdCache.Clear();
                }
            }
        }

        public void Remove(LogEventSubscription item) {
            lock (Locker) {
                var removed = item != null && Set.Remove(item);
                if (removed) {
                    item.ThresholdChangedEvent -= Item_SeverityChangedEvent;
                    ThresholdCache.Clear();
                }
            }
        }

        public bool ThresholdMet(LogSeverity severity, Type type) {
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

        public void Send(ILogEntry entry) {
            lock (Locker) {
                foreach (var item in Set) {
                    if (item != null) {
                        item.InternalReceive(entry);
                    }
                }
            }
        }
    }
}
