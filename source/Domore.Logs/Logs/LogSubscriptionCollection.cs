using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs;

internal sealed class LogSubscriptionCollection {
    private readonly Dictionary<Type, LogSeverity> Thresholds = [];
    private readonly Dictionary<ILogSubscription, LogSubscriptionProxy> Lookup = [];

    private int ThresholdGeneration;

    private void Item_ThresholdChanged(object sender, EventArgs e) {
        lock (Lookup) {
            Thresholds.Clear();
            ThresholdGeneration++;
        }
    }

    private LogSeverity Threshold(Type type) {
        if (type is null) {
            return LogSeverity.None;
        }
        LogSubscriptionProxy[] items;
        int generation;
        lock (Lookup) {
            if (Lookup.Count == 0) {
                return LogSeverity.None;
            }
            if (Thresholds.TryGetValue(type, out var severity)) {
                return severity;
            }
            items = [.. Lookup.Values];
            generation = ThresholdGeneration;
        }
        var threshold = items
            .Select(item => item.Threshold(type))
            .Where(severity => severity != LogSeverity.None)
            .OrderBy(severity => severity)
            .FirstOrDefault();
        lock (Lookup) {
            if (generation == ThresholdGeneration && Lookup.Count > 0) {
                Thresholds[type] = threshold;
            }
        }
        return threshold;
    }

    public int Count { get; private set; }

    public void Complete() {
        if (Count == 0) {
            return;
        }
        LogSubscriptionProxy[] items;
        lock (Lookup) {
            items = [.. Lookup.Values];
        }
        var exceptions = new List<Exception>();
        foreach (var item in items) {
            try {
                item.Complete();
            }
            catch (Exception ex) {
                exceptions.Add(ex);
            }
        }
        if (exceptions.Count > 0) {
            throw new AggregateException("One or more log subscriptions failed to complete.", exceptions);
        }
    }

    public bool Add(ILogSubscription item) {
        if (item is null) {
            return false;
        }
        lock (Lookup) {
            if (Lookup.TryGetValue(item, out var proxy) == false) {
                Lookup[item] = proxy = new LogSubscriptionProxy(item);
                Thresholds.Clear();
                ThresholdGeneration++;
                Count = Lookup.Count;
                proxy.ThresholdChanged += Item_ThresholdChanged;
                return true;
            }
        }
        return false;
    }

    public bool Remove(ILogSubscription item) {
        if (item is null) {
            return false;
        }
        LogSubscriptionProxy proxy;
        lock (Lookup) {
            if (Lookup.TryGetValue(item, out proxy) == false) {
                return false;
            }
            Lookup.Remove(item);
            Thresholds.Clear();
            ThresholdGeneration++;
            Count = Lookup.Count;
            proxy.ThresholdChanged -= Item_ThresholdChanged;
        }
        proxy.Complete();
        return true;
    }

    public void Clear() {
        var items = default(LogSubscriptionProxy[]);
        var exceptions = new List<Exception>();
        lock (Lookup) {
            items = [.. Lookup.Values];
            foreach (var item in items) {
                item.ThresholdChanged -= Item_ThresholdChanged;
            }
            Lookup.Clear();
            Thresholds.Clear();
            ThresholdGeneration++;
            Count = Lookup.Count;
        }
        foreach (var item in items) {
            try {
                item.Complete();
            }
            catch (Exception ex) {
                exceptions.Add(ex);
            }
        }
        if (exceptions.Count > 0) {
            throw new AggregateException("One or more log subscriptions failed to complete.", exceptions);
        }
    }

    public bool Send(LogSeverity severity, Type type) {
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
        LogSubscriptionProxy[] items;
        lock (Lookup) {
            items = Lookup.Values.ToArray();
        }
        foreach (var item in items) {
            item.Receive(entry);
        }
    }
}
