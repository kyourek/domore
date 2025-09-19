using System;

namespace Domore.Logs.Mocks; 
internal sealed class MockLogSubscription : ILogSubscription {
    private event EventHandler OnThresholdChanged;

    public Func<Type, LogSeverity> Threshold { get; set; }
    public Action<ILogEntry> Receive { get; set; }

    public void ThresholdChanged() {
        OnThresholdChanged?.Invoke(this, EventArgs.Empty);
    }

    event EventHandler ILogSubscription.ThresholdChanged {
        add { OnThresholdChanged += value; }
        remove { OnThresholdChanged -= value; }
    }

    void ILogSubscription.Receive(ILogEntry entry) {
        Receive(entry);
    }

    LogSeverity ILogSubscription.Threshold(Type type) {
        return Threshold(type);
    }
}
