using System;
using System.Collections.Generic;
using System.Threading;

namespace Domore.Logs;

internal sealed class LogServiceProxy {
    private static readonly LogServiceFactory Factory = new();
    private readonly LogManager Manager;
    private readonly Domore.Threading.BackgroundQueue DispatchQueue;
    private readonly object AdmissionLocker = new();

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
    private int _QueueItemLimit = 1024;
    private long _QueueByteLimit = 8L * 1024 * 1024;
    private long PendingItemCount;
    private long PendingMessageBytes;
    private long DroppedItemCount;
    private long DroppedMessageBytes;
    private bool AdmissionClosed;

    public bool ThreadIsCurrentThread =>
        DispatchQueue.ThreadIsCurrentThread;

    public int QueueItemLimit {
        get {
            lock (AdmissionLocker) {
                return _QueueItemLimit;
            }
        }
        set {
            if (value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(QueueItemLimit), value,
                    "The queue item limit must be greater than zero.");
            }
            lock (AdmissionLocker) {
                _QueueItemLimit = value;
            }
        }
    }

    public long QueueByteLimit {
        get {
            lock (AdmissionLocker) {
                return _QueueByteLimit;
            }
        }
        set {
            if (value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(QueueByteLimit), value,
                    "The queue byte limit must be greater than zero.");
            }
            lock (AdmissionLocker) {
                _QueueByteLimit = value;
            }
        }
    }

    public LogQueueStatus QueueStatus {
        get {
            LogQueueStatistics dispatchQueue;
            lock (AdmissionLocker) {
                dispatchQueue = new LogQueueStatistics(
                    _QueueItemLimit,
                    _QueueByteLimit,
                    PendingItemCount,
                    PendingMessageBytes,
                    DroppedItemCount,
                    DroppedMessageBytes);
            }

            LogQueueStatistics serviceQueue = null;
            var service = _Service;
            if (service is ILogQueueStatusProvider provider) {
                try {
                    // Optional service code is called after releasing the short admission lock.
                    using (LogCallbackGuard.EnterManager(Manager))
                    using (LogCallbackGuard.Enter()) {
                        serviceQueue = provider.QueueStatus;
                    }
                }
                catch {
                    // Status reporting must not interfere with the logging path.
                }
            }
            return new LogQueueStatus(dispatchQueue, serviceQueue);
        }
    }

    private static long SaturatingAdd(long value, long increment) =>
        value > long.MaxValue - increment ? long.MaxValue : value + increment;

    public bool Enqueue(LogEntry entry) {
        if (entry is null) {
            return false;
        }

        var messageBytes = entry.RetainedTextBytes;
        var rejected = false;
        lock (AdmissionLocker) {
            if (AdmissionClosed) {
                return false;
            }
            if (PendingItemCount >= _QueueItemLimit ||
                messageBytes > _QueueByteLimit ||
                PendingMessageBytes > _QueueByteLimit - messageBytes) {
                DroppedItemCount = SaturatingAdd(DroppedItemCount, 1);
                DroppedMessageBytes = SaturatingAdd(DroppedMessageBytes, messageBytes);
                rejected = true;
            }
            else {
                PendingItemCount++;
                PendingMessageBytes += messageBytes;
                DispatchQueue.Add(() => Deliver(entry, messageBytes));
            }
        }
        if (rejected) {
            LogQueueDiagnostics.ReportOverflow();
            return false;
        }
        return true;
    }

    private void Deliver(LogEntry entry, long messageBytes) {
        lock (AdmissionLocker) {
            if (PendingItemCount > 0) {
                PendingItemCount--;
            }
            if (PendingMessageBytes >= messageBytes) {
                PendingMessageBytes -= messageBytes;
            }
            else {
                PendingMessageBytes = 0;
            }
        }
        Log(entry);
    }

    public void CloseAdmission() {
        lock (AdmissionLocker) {
            AdmissionClosed = true;
        }
    }

    public void DrainQueue() {
        DispatchQueue.Complete();
    }

    public void DisposeQueue() {
        DispatchQueue.Dispose();
    }

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
    private volatile ILogService _Service;

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
        DispatchQueue = new Domore.Threading.BackgroundQueue(Logging.Notify);
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
        CloseAdmission();
        DrainQueue();
        CompleteService();
    }

    public void CompleteService() {
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
