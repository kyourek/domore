using System;
using System.Collections.Generic;

namespace Domore.Logs {
    public abstract class LogEventSubscription {
        private readonly Dictionary<Type, LogSeverity> ThresholdCache = [];

        internal event EventHandler ThresholdChangedEvent;

        internal LogSeverity InternalThreshold(Type type) {
            if (type == null) {
                return LogSeverity.None;
            }
            lock (ThresholdCache) {
                if (ThresholdCache.TryGetValue(type, out var severity) == false) {
                    try {
                        severity = Threshold(type);
                    }
                    catch (Exception ex) {
                        Logging.Notify(ex);
                    }
                    ThresholdCache[type] = severity;
                }
                return severity;
            }
        }

        internal void InternalReceive(ILogEntry entry) {
            try {
                Receive(entry);
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
        }

        protected void ThresholdChanged() {
            ThresholdCache.Clear();
            ThresholdChangedEvent?.Invoke(this, EventArgs.Empty);
        }

        protected abstract void Receive(ILogEntry entry);
        protected abstract LogSeverity Threshold(Type type);
    }
}
