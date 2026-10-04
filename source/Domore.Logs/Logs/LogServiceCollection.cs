using Domore.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs;
internal sealed class LogServiceCollection : IDisposable {
    private sealed class ThresholdSnapshot {
        private readonly Dictionary<string, LogSeverity> TypeThreshold;

        public LogSeverity DefaultThreshold { get; }

        public ThresholdSnapshot(LogSeverity defaultThreshold, Dictionary<string, LogSeverity> typeThreshold) {
            DefaultThreshold = defaultThreshold;
            TypeThreshold = new Dictionary<string, LogSeverity>(typeThreshold);
        }

        public bool TryGetThreshold(string name, out LogSeverity severity) {
            return TypeThreshold.TryGetValue(name, out severity);
        }
    }

    private readonly object Locker = new();
    private readonly BackgroundQueue Queue = new();
    private readonly Dictionary<string, LogServiceProxy> Set = [];
    private volatile LogServiceProxy[] Proxies = new LogServiceProxy[0];
    private volatile ThresholdSnapshot Thresholds = new(
        LogSeverity.None,
        new Dictionary<string, LogSeverity>());

    private void Dispose(bool disposing) {
        if (disposing) {
            Queue.Dispose();
        }
    }

    private void SetThresholdChanged() {
        lock (Locker) {
            var names = Set.SelectMany(item => item.Value.Config.Names).Distinct();
            var typeThreshold = new Dictionary<string, LogSeverity>();
            foreach (var name in names) {
                var severity = Set
                    .Select(item => item.Value)
                    .Select(log => log.Config[name].Threshold)
                    .Where(sev => sev.HasValue)
                    .Select(sev => sev.Value)
                    .Where(sev => sev != LogSeverity.None)
                    .OrderBy(sev => sev)
                    .FirstOrDefault();
                if (severity != LogSeverity.None) {
                    typeThreshold[name] = severity;
                }
            }
            var defaultThreshold = Set
                .Select(item => item.Value)
                .Select(log => log.Config.Default.Threshold)
                .Where(sev => sev.HasValue)
                .Select(sev => sev.Value)
                .Where(sev => sev != LogSeverity.None)
                .OrderBy(sev => sev)
                .FirstOrDefault();
            Thresholds = new ThresholdSnapshot(defaultThreshold, typeThreshold);
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
                    value = new LogServiceProxy(name);
                    value.Config.TypeThresholdChanged += Config_TypeThresholdChanged;
                    value.Config.DefaultThresholdChanged += Config_DefaultThresholdChanged;
                    Set[name] = value;
                    Proxies = Set.Values.ToArray();
                }
                return value;
            }
        }
    }

    public int Count =>
        Proxies.Length;

    public bool Send(LogSeverity severity, Type type) {
        var thresholds = Thresholds;
        if (thresholds.TryGetThreshold(type.Name, out var value)) {
            return value != LogSeverity.None && value <= severity;
        }
        var defaultThreshold = thresholds.DefaultThreshold;
        return defaultThreshold != LogSeverity.None && defaultThreshold <= severity;
    }

    public void Send(LogEntry entry) {
        Queue.Add(() => {
            var proxies = Proxies;
            foreach (var proxy in proxies) {
                try {
                    proxy.Log(entry);
                }
                catch (Exception ex) {
                    Logging.Notify(ex);
                }
            }
        });
    }

    public bool Complete(TimeSpan timeout) {
        var queueDrained = false;
        try {
            queueDrained = Queue.Complete(timeout);
            if (queueDrained == false) {
                Logging.Notify(new TimeoutException($"Log services did not drain within {timeout}."));
            }
        }
        catch (Exception ex) {
            Logging.Notify(ex);
        }

        var proxies = Proxies;
        foreach (var proxy in proxies) {
            try {
                proxy.Complete();
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
        }
        return queueDrained;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~LogServiceCollection() {
        Dispose(false);
    }
}
