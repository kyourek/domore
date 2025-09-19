using System;

namespace Domore.Logs; 
internal delegate void LogTypeThresholdChangedEvent(object sender, LogTypeThresholdChangedEventArgs e);

internal sealed class LogTypeThresholdChangedEventArgs : EventArgs {
    public string TypeName { get; }
    public LogSeverity? Threshold { get; }

    public LogTypeThresholdChangedEventArgs(LogSeverity? threshold, string typeName = null) {
        TypeName = typeName;
        Threshold = threshold;
    }
}
