using System;

namespace Domore.Logs {
    public interface ILogSubscription {
        event EventHandler ThresholdChanged;
        LogSeverity Threshold(Type type);
        void Receive(ILogEntry entry);
    }
}
