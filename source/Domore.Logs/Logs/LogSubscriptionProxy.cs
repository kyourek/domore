using System;
using System.Collections.Concurrent;

namespace Domore.Logs; 
internal sealed class LogSubscriptionProxy {
    private readonly ConcurrentDictionary<Type, LogSeverity> ThresholdCache = [];

    public ILogSubscription Agent { get; }

    public LogSubscriptionProxy(ILogSubscription agent) {
        Agent = agent ?? new None();
        Agent.ThresholdChanged += Agent_ThresholdChanged;
    }

    private void Agent_ThresholdChanged(object sender, EventArgs e) {
        ThresholdCache.Clear();
        ThresholdChanged?.Invoke(this, e);
    }

    public event EventHandler ThresholdChanged;

    public LogSeverity Threshold(Type type) {
        if (type == null) {
            return LogSeverity.None;
        }
        return ThresholdCache.GetOrAdd(type, type => {
            try {
                return Agent.Threshold(type);
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
            return LogSeverity.None;
        });
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
        Agent.ThresholdChanged -= Agent_ThresholdChanged;
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
