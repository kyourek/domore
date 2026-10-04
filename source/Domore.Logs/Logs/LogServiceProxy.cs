using System;
using System.Collections.Generic;
using System.Threading;

namespace Domore.Logs;

internal sealed class LogServiceProxy {
    private static readonly LogServiceFactory Factory = new();
    private readonly LogManager Manager;

    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        Locker = new();

    private bool ServiceCompleted;
    private Exception ServiceInitializationError;
    private int ServiceCallDepth;
    private bool PendingTypeChange;
    private string PendingType;
    private ILogService PendingService;

    private ILogService GetServiceUnsafe() {
        if (_Service is null) {
            try {
                using (LogCallbackGuard.EnterManager(Manager))
                using (LogCallbackGuard.Enter()) {
                    _Service = Factory.Create(_Type ?? Name) ?? new None();
                }
                ServiceInitializationError = null;
                ServiceCompleted = false;
            }
            catch (Exception ex) {
                ServiceInitializationError = ex;
                throw;
            }
        }
        return _Service;
    }

    private void CompleteServiceUnsafe(ILogService service) {
        ServiceCallDepth++;
        try {
            Logging.CompleteService(Manager, service);
        }
        finally {
            ServiceCallDepth--;
            ApplyPendingType();
        }
    }

    private void ReplaceType(string value, ILogService replacement = null) {
        var service = _Service;
        var completeService = service != null && ServiceCompleted == false;
        _Type = value;
        _Service = replacement;
        ServiceInitializationError = null;
        ServiceCompleted = false;
        if (completeService) {
            CompleteServiceUnsafe(service);
        }
    }

    private void ApplyPendingType() {
        if (ServiceCallDepth > 0 || PendingTypeChange == false) {
            return;
        }
        var type = PendingType;
        var service = PendingService;
        PendingType = null;
        PendingService = null;
        PendingTypeChange = false;
        ReplaceType(type, service);
    }

    public ILogService Service {
        get {
            lock (Locker) {
                // Configure the future instance without changing delivery during the active callback.
                if (PendingTypeChange) {
                    if (PendingService is null) {
                        using (LogCallbackGuard.EnterManager(Manager))
                        using (LogCallbackGuard.Enter()) {
                            PendingService = Factory.Create(PendingType) ?? new None();
                        }
                    }
                    return PendingService;
                }
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
                    var discarded = PendingService;
                    PendingService = null;
                    PendingTypeChange = value != (_Type ?? Name);
                    PendingType = PendingTypeChange ? value : null;
                    if (discarded is not null) {
                        CompleteServiceUnsafe(discarded);
                    }
                    return;
                }
                ReplaceType(value);
            }
        }
    }
    private string _Type;

    public string Name { get; }

    public LogServiceProxy(string name, LogManager manager = null) {
        Name = name;
        Manager = manager;
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
                    using (LogCallbackGuard.EnterManager(Manager))
                    using (LogCallbackGuard.Enter()) {
                        service.Log(name, data, sev);
                    }
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
            // Completing a configured proxy must not instantiate a service that has
            // never been requested or used.
            if (_Service is null) {
                if (ServiceInitializationError is not null) {
                    throw new AggregateException("The log service could not be initialized.", ServiceInitializationError);
                }
                return;
            }
            List<Exception> exceptions = null;
            // A completion callback may configure and install another live instance.
            do {
                var service = _Service;
                if (ServiceCompleted) {
                    break;
                }
                ServiceCompleted = true;
                try {
                    CompleteServiceUnsafe(service);
                }
                catch (Exception ex) {
                    (exceptions ??= []).Add(ex);
                }
            }
            while (_Service is not null && ServiceCompleted == false);
            if (exceptions is not null) {
                throw new AggregateException("One or more instances of the log service failed to complete.", exceptions);
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
