using System.Threading;

namespace Domore.Logs; 
internal sealed class LogServiceProxy {
    private static readonly LogServiceFactory Factory = new();

    private readonly object Locker = new();

    private sealed class None : ILogService {
        void ILogService.Log(string name, string data, LogSeverity severity) {
        }

        void ILogService.Complete() {
        }
    }

    public ILogService Service {
        get {
            if (_Service == null) {
                lock (Locker) {
                    if (_Service == null) {
                        var service = Factory.Create(Type) ?? new None();
                        Thread.MemoryBarrier();
                        _Service = service;
                    }
                }
            }
            return _Service;
        }
    }
    private ILogService _Service;

    public LogServiceConfig Config {
        get {
            if (_Config == null) {
                lock (Locker) {
                    if (_Config == null) {
                        var config = new LogServiceConfig();
                        Thread.MemoryBarrier();
                        _Config = config;
                    }
                }
            }
            return _Config;
        }
    }
    private LogServiceConfig _Config;

    public string Type {
        get => _Type ??= Name;
        set {
            if (_Type != value) {
                lock (Locker) {
                    if (_Type != value) {
                        _Type = value;
                        _Service = null;
                    }
                }
            }
        }
    }
    private string _Type;

    public string Name { get; }

    public LogServiceProxy(string name) {
        Name = name;
    }

    public void Log(LogEntry entry) {
        if (entry == null) {
            return;
        }
        var sev = entry.EntrySeverity;
        var name = entry.LogName;
        var limit = Config[name].Threshold ?? Config.Default.Threshold;
        if (limit.HasValue && limit.Value != LogSeverity.None && limit.Value <= sev) {
            var frmt = Config[name].Format ?? Config.Default.Format;
            var data = entry.LogData(frmt);
            Service.Log(name, data, sev);
        }
    }

    public void Complete() {
        Service.Complete();
    }
}
