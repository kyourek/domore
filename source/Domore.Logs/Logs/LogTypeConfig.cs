using System;

namespace Domore.Logs; 
internal sealed class LogTypeConfig {
    public event EventHandler ThresholdChanged;

    public string Format { get; set; }

    public string Name { get; }

    public LogTypeConfig(string name = null) {
        Name = name;
    }

    public LogSeverity? Severity {
        get => Threshold;
        set => Threshold = value;
    }

    public LogSeverity? Threshold {
        get => _Threshold;
        set {
            if (_Threshold != value) {
                _Threshold = value;
                ThresholdChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    private LogSeverity? _Threshold;
}
