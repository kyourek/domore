using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    internal sealed class LogSubscriptionCollection {
        private readonly Dictionary<Type, LogSeverity> Thresholds = [];
        private readonly Dictionary<ILogSubscription, LogSubscriptionProxy> Lookup = [];

        private void Item_ThresholdChanged(object sender, EventArgs e) {
            lock (Lookup) {
                Thresholds.Clear();
            }
        }

        private LogSeverity Threshold(Type type) {
            if (Count == 0) {
                return LogSeverity.None;
            }
            if (type == null) {
                return LogSeverity.None;
            }
            lock (Lookup) {
                if (Thresholds.TryGetValue(type, out var severity) == false) {
                    Thresholds[type] = severity = Lookup.Values
                        .Select(item => item.Threshold(type))
                        .Where(s => s != LogSeverity.None)
                        .OrderBy(s => s)
                        .FirstOrDefault();
                }
                return severity;
            }
        }

        public int Count { get; private set; }

        public void Complete() {
            if (Count == 0) {
                return;
            }
            lock (Lookup) {
                foreach (var item in Lookup.Values) {
                    item.Complete();
                }
            }
        }

        public bool Add(ILogSubscription item) {
            if (item == null) {
                return false;
            }
            lock (Lookup) {
                if (Lookup.TryGetValue(item, out var proxy) == false) {
                    Lookup[item] = proxy = new LogSubscriptionProxy(item);
                    Thresholds.Clear();
                    Count = Lookup.Count;
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
            if (Count == 0) {
                return false;
            }
            lock (Lookup) {
                if (Lookup.TryGetValue(item, out var proxy)) {
                    Lookup.Remove(item);
                    Thresholds.Clear();
                    Count = Lookup.Count;
                    proxy.ThresholdChanged -= Item_ThresholdChanged;
                    return true;
                }
            }
            return false;
        }

        public void Clear() {
            if (Count == 0) {
                return;
            }
            lock (Lookup) {
                foreach (var item in Lookup.Values) {
                    item.ThresholdChanged -= Item_ThresholdChanged;
                }
                Lookup.Clear();
                Thresholds.Clear();
                Count = Lookup.Count;
            }
        }

        public bool Send(LogSeverity severity, Type type) {
            if (Count == 0) {
                return false;
            }
            if (severity == LogSeverity.None) {
                return false;
            }
            var threshold = Threshold(type);
            if (threshold == LogSeverity.None) {
                return false;
            }
            return threshold <= severity;
        }

        public void Send(LogEntry entry) {
            if (Count == 0) {
                return;
            }
            lock (Lookup) {
                foreach (var item in Lookup.Values) {
                    if (item != null) {
                        item.Receive(entry);
                    }
                }
            }
        }
    }
}
