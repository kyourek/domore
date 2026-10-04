using System;
using System.Threading;

namespace Domore.Logs; 
internal sealed class LogServiceProxy {
    private static readonly LogServiceFactory Factory = new();

    private readonly object Locker = new();
    private readonly Action<ILogService> CompleteService;

    private sealed class None : ILogService {
        void ILogService.Log(string name, string data, LogSeverity severity) {
        }

        void ILogService.Complete() {
        }
    }

    public ILogService Service {
        get {
            var service = Interlocked.CompareExchange(ref _Service, null, null);
            if (service == null) {
                lock (Locker) {
                    service = Interlocked.CompareExchange(ref _Service, null, null);
                    if (service == null) {
                        service = Factory.Create(_Type ??= Name) ?? new None();
                        Interlocked.Exchange(ref _Service, service);
                    }
                }
            }
            return service;
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
        get {
            lock (Locker) {
                return _Type ??= Name;
            }
        }
        set {
            ILogService service = null;
            lock (Locker) {
                if (_Type != value) {
                    _Type = value;
                    service = Interlocked.Exchange(ref _Service, null);
                }
            }

            if (service != null) {
                try {
                    CompleteService(service);
                }
                catch (Exception ex) {
                    Logging.Notify(ex);
                }
            }
        }
    }
    private string _Type;

    public string Name { get; }

    public LogServiceProxy(string name, Action<ILogService> completeService) {
        Name = name;
        CompleteService = completeService ?? throw new ArgumentNullException(nameof(completeService));
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
            try {
                Service.Log(name, data, sev);
            }
            catch (Exception ex) {
                Logging.Notify(ex);
            }
        }
    }

    public void Complete() {
        Service.Complete();
    }
}
