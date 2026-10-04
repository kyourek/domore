using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Domore.Logs; 
internal sealed class LogSubscriptionProxy {
    private const int MaxThresholdAttempts = 3;
    private readonly ConcurrentDictionary<Type, ThresholdValue> ThresholdCache = [];
    private int Completed;
    private long ThresholdGeneration;

    public ILogSubscription Agent { get; }

    public LogSubscriptionProxy(ILogSubscription agent) {
        Agent = agent ?? new None();
        Agent.ThresholdChanged += Agent_ThresholdChanged;
    }

    private void Agent_ThresholdChanged(object sender, EventArgs e) {
        Interlocked.Increment(ref ThresholdGeneration);
        ThresholdCache.Clear();
        ThresholdChanged?.Invoke(this, e);
    }

    public event EventHandler ThresholdChanged;

    public LogSeverity Threshold(Type type) {
        if (type == null) {
            return LogSeverity.None;
        }

        var severity = LogSeverity.None;
        for (var attempt = 0; attempt < MaxThresholdAttempts; attempt++) {
            var generation = Interlocked.Read(ref ThresholdGeneration);
            if (ThresholdCache.TryGetValue(type, out var cached) && cached.Generation == generation) {
                if (generation == Interlocked.Read(ref ThresholdGeneration)) {
                    return cached.Severity;
                }
                continue;
            }

            try {
                severity = Agent.Threshold(type);
            }
            catch (Exception ex) {
                Logging.Notify(ex);
                severity = LogSeverity.None;
            }

            if (generation == Interlocked.Read(ref ThresholdGeneration)) {
                ThresholdCache[type] = new ThresholdValue(generation, severity);
                if (generation == Interlocked.Read(ref ThresholdGeneration)) {
                    return severity;
                }
            }
        }
        return severity;
    }

    public void Receive(LogEntry entry) {
        if (entry == null) {
            return;
        }
        var threshold = Threshold(entry.LogType);
        if (threshold != LogSeverity.None && threshold <= entry.EntrySeverity) {
            try {
                Agent.Receive(entry);
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
        }
    }

    public void Complete() {
        if (Interlocked.Exchange(ref Completed, 1) != 0) {
            return;
        }
        try {
            Agent.ThresholdChanged -= Agent_ThresholdChanged;
        }
        catch (Exception ex) {
            Logging.Notify(ex);
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

    private sealed class None : ILogSubscription {
        event EventHandler ILogSubscription.ThresholdChanged {
            add { }
            remove { }
        }

        void ILogSubscription.Receive(ILogEntry entry) {
        }

        LogSeverity ILogSubscription.Threshold(Type type) {
            return LogSeverity.None;
        }
    }
}
