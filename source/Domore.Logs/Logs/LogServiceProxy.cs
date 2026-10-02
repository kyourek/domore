using System.Threading;

namespace Domore.Logs;

internal sealed class LogServiceProxy {
    private static readonly LogServiceFactory Factory = new();

    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        Locker = new();

    private bool ServiceCompleted;
    private int ServiceCallDepth;
    private bool PendingTypeChange;
    private string PendingType;

    private ILogService GetServiceUnsafe() {
        if (_Service is null) {
            _Service = Factory.Create(Type) ?? new None();
            ServiceCompleted = false;
        }
        return _Service;
    }

    private void ReplaceType(string value) {
        var service = _Service;
        var completeService = service != null && ServiceCompleted == false;
        _Type = value;
        _Service = null;
        ServiceCompleted = false;
        if (completeService) {
            ServiceCallDepth++;
            try {
                service.Complete();
            }
            finally {
                ServiceCallDepth--;
                ApplyPendingType();
            }
        }
    }

    private void ApplyPendingType() {
        if (ServiceCallDepth > 0 || PendingTypeChange == false) {
            return;
        }
        var type = PendingType;
        PendingType = null;
        PendingTypeChange = false;
        ReplaceType(type);
    }

    public ILogService Service {
        get {
            lock (Locker) {
                return GetServiceUnsafe();
            }
        }
    }
    private ILogService _Service;

    public LogServiceConfig Config {
        get {
            if (_Config is null) {
                lock (Locker) {
                    if (_Config is null) {
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
                if (PendingTypeChange) {
                    return PendingType;
                }
                return _Type ??= Name;
            }
        }
        set {
            lock (Locker) {
                var current = PendingTypeChange ? PendingType : _Type ?? Name;
                if (current == value) {
                    return;
                }
                if (ServiceCallDepth > 0) {
                    PendingType = value;
                    PendingTypeChange = true;
                    return;
                }
                ReplaceType(value);
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
        lock (Locker) {
            var sev = entry.EntrySeverity;
            var name = entry.LogName;
            var limit = Config[name].Threshold ?? Config.Default.Threshold;
            if (limit.HasValue && limit.Value != LogSeverity.None && limit.Value <= sev) {
                var frmt = Config[name].Format ?? Config.Default.Format;
                var data = entry.LogData(frmt);
                var service = GetServiceUnsafe();
                ServiceCallDepth++;
                try {
                    service.Log(name, data, sev);
                }
                finally {
                    ServiceCallDepth--;
                    ApplyPendingType();
                }
            }
        }
    }

    public void Complete() {
        lock (Locker) {
            var service = GetServiceUnsafe();
            if (ServiceCompleted) {
                return;
            }
            ServiceCompleted = true;
            ServiceCallDepth++;
            try {
                service.Complete();
            }
            finally {
                ServiceCallDepth--;
                ApplyPendingType();
            }
        }
    }

    private sealed class None : ILogService {
        void ILogService.Log(string name, string data, LogSeverity severity) {
        }

        void ILogService.Complete() {
        }
    }
}
