using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Domore.Logs; 
internal sealed class LogSubscriptionCollection {
    private readonly object Locker = new();
    private readonly ConcurrentDictionary<Type, ThresholdValue> Thresholds = new();
    private readonly Dictionary<ILogSubscription, LogSubscriptionProxy> Lookup = [];
    private volatile LogSubscriptionProxy[] Items = new LogSubscriptionProxy[0];
    private long ThresholdGeneration;

    private void Item_ThresholdChanged(object sender, EventArgs e) {
        InvalidateThresholds();
    }

    private LogSeverity Threshold(Type type) {
        if (type == null) {
            return LogSeverity.None;
        }
        while (true) {
            var generation = Interlocked.Read(ref ThresholdGeneration);
            var items = Items;
            if (generation != Interlocked.Read(ref ThresholdGeneration)) {
                continue;
            }
            if (items.Length == 0) {
                return LogSeverity.None;
            }
            if (Thresholds.TryGetValue(type, out var cached) && cached.Generation == generation) {
                if (generation == Interlocked.Read(ref ThresholdGeneration)) {
                    return cached.Severity;
                }
                continue;
            }
            var severity = items
                .Select(item => item.Threshold(type))
                .Where(s => s != LogSeverity.None)
                .OrderBy(s => s)
                .FirstOrDefault();
            if (generation == Interlocked.Read(ref ThresholdGeneration)) {
                Thresholds[type] = new ThresholdValue(generation, severity);
            }
            return severity;
        }
    }

    private void InvalidateThresholds() {
        Interlocked.Increment(ref ThresholdGeneration);
        Thresholds.Clear();
    }

    public int Count =>
        Items.Length;

    public void Complete() {
        var items = Items;
        foreach (var item in items) {
            item.Complete();
        }
    }

    public bool Add(ILogSubscription item) {
        if (item == null) {
            return false;
        }
        lock (Locker) {
            if (Lookup.TryGetValue(item, out var proxy) == false) {
                Lookup[item] = proxy = new LogSubscriptionProxy(item);
                proxy.ThresholdChanged += Item_ThresholdChanged;
                Items = Lookup.Values.ToArray();
                InvalidateThresholds();
                return true;
            }
        }
        return false;
    }

    public bool Remove(ILogSubscription item) {
        if (item == null) {
            return false;
        }
        LogSubscriptionProxy removed;
        lock (Locker) {
            if (Lookup.TryGetValue(item, out var proxy)) {
                Lookup.Remove(item);
                proxy.ThresholdChanged -= Item_ThresholdChanged;
                Items = Lookup.Values.ToArray();
                InvalidateThresholds();
                removed = proxy;
            }
            else {
                return false;
            }
        }
        removed.Complete();
        return true;
    }

    public void Clear() {
        LogSubscriptionProxy[] removed;
        lock (Locker) {
            if (Lookup.Count == 0) {
                return;
            }
            removed = Lookup.Values.ToArray();
            foreach (var item in removed) {
                item.ThresholdChanged -= Item_ThresholdChanged;
            }
            Lookup.Clear();
            Items = new LogSubscriptionProxy[0];
            InvalidateThresholds();
        }
        foreach (var item in removed) {
            item.Complete();
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
        if (entry == null) {
            return;
        }
        var items = Items;
        foreach (var item in items) {
            if (item != null) {
                item.Receive(entry);
            }
        }
    }

    private readonly struct ThresholdValue {
        public long Generation { get; }
        public LogSeverity Severity { get; }

        public ThresholdValue(long generation, LogSeverity severity) {
            Generation = generation;
            Severity = severity;
        }
    }
}
