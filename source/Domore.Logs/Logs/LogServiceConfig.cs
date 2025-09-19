using System;
using System.Collections.Generic;
using System.Threading;

namespace Domore.Logs; 
internal sealed class LogServiceConfig {
    private readonly object Locker = new();
    private readonly Dictionary<string, LogTypeConfig> Type = [];

    private void Type_ThresholdChanged(object sender, EventArgs e) {
        var config = (LogTypeConfig)sender;
        var args = new LogTypeThresholdChangedEventArgs(config.Threshold, config.Name);
        TypeThresholdChanged?.Invoke(this, args);
    }

    private void Default_ThresholdChanged(object sender, EventArgs e) {
        var args = new LogTypeThresholdChangedEventArgs(Default.Threshold);
        DefaultThresholdChanged?.Invoke(this, args);
    }

    public event LogTypeThresholdChangedEvent TypeThresholdChanged;
    public event LogTypeThresholdChangedEvent DefaultThresholdChanged;

    public LogTypeConfig this[string name] {
        get {
            lock (Type) {
                if (Type.TryGetValue(name, out var value) == false) {
                    Type[name] = value = new LogTypeConfig(name);
                    Type[name].ThresholdChanged += Type_ThresholdChanged;
                }
                return value;
            }
        }
    }

    public LogTypeConfig Default {
        get {
            if (_Default == null) {
                lock (Locker) {
                    if (_Default == null) {
                        var @default = new LogTypeConfig();
                        Thread.MemoryBarrier();
                        _Default = @default;
                        _Default.ThresholdChanged += Default_ThresholdChanged;
                    }
                }
            }
            return _Default;
        }
    }
    private LogTypeConfig _Default;

    public IEnumerable<string> Names {
        get {
            lock (Type) {
                return new List<string>(Type.Keys);
            }
        }
    }
}
