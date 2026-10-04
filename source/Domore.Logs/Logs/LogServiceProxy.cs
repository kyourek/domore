using System;
using System.Collections.Generic;
using System.Threading;

namespace Domore.Logs;

internal sealed class LogServiceProxy {
    private static readonly LogServiceFactory Factory = new();

    private readonly LogManager Manager;
    private readonly Domore.Threading.BackgroundQueue DispatchQueue;
    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        AdmissionLocker = new();

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
    private long PendingItemCount;
    private long PendingMessageBytes;
    private long DroppedItemCount;
    private long DroppedMessageBytes;
    private bool AdmissionClosed;

    private static long SaturatingAdd(long value, long increment) =>
        value > long.MaxValue - increment ? long.MaxValue : value + increment;

    private ILogService GetServiceUnsafe() {
        if (_Service is null) {
            try {
                using (LogCallbackGuard.EnterManager(Manager))
                using (LogCallbackGuard.Enter()) {
                    _Service = Factory.Create(Type) ?? new None();
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

    private void ApplyPendingType() {
        if (ServiceCallDepth > 0 || PendingTypeChange == false) {
            return;
        }
        var type = PendingType;
        PendingType = null;
        PendingTypeChange = false;
        Type = type;
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

    public bool ThreadIsCurrentThread =>
        DispatchQueue.ThreadIsCurrentThread;

    public int QueueItemLimit {
        get {
            lock (AdmissionLocker) {
                return field;
            }
        }
        set {
            if (value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(QueueItemLimit), value,
                    "The queue item limit must be greater than zero.");
            }
            lock (AdmissionLocker) {
                field = value;
            }
        }
    } = 1024;

    public long QueueByteLimit {
        get {
            lock (AdmissionLocker) {
                return field;
            }
        }
        set {
            if (value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(QueueByteLimit), value,
                    "The queue byte limit must be greater than zero.");
            }
            lock (AdmissionLocker) {
                field = value;
            }
        }
    } = 8L * 1024 * 1024;

    public LogQueueStatus QueueStatus {
        get {
            LogQueueStatistics dispatchQueue;
            lock (AdmissionLocker) {
                dispatchQueue = new LogQueueStatistics(
                    QueueItemLimit,
                    QueueByteLimit,
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

    public LogServiceConfig Config {
        get {
            if (field is null) {
                lock (Locker) {
                    if (field is null) {
                        var config = new LogServiceConfig();
                        Thread.MemoryBarrier();
                        field = config;
                    }
                }
            }
            return field;
        }
    }

    public string Type {
        get {
            lock (Locker) {
                if (PendingTypeChange) {
                    return PendingType;
                }
                return field ??= Name;
            }
        }
        set {
            lock (Locker) {
                var current = PendingTypeChange ? PendingType : field ?? Name;
                if (current == value) {
                    return;
                }
                if (ServiceCallDepth > 0) {
                    var discarded = PendingService;
                    PendingService = null;
                    PendingTypeChange = value != (field ?? Name);
                    PendingType = PendingTypeChange ? value : null;
                    if (discarded is not null) {
                        CompleteServiceUnsafe(discarded);
                    }
                    return;
                }
                var replacement = PendingService;
                PendingService = null;
                var service = _Service;
                var completeService = service is not null && ServiceCompleted == false;
                field = value;
                _Service = replacement;
                ServiceInitializationError = null;
                ServiceCompleted = false;
                if (completeService) {
                    CompleteServiceUnsafe(service);
                }
            }
        }
    }

    public string Name { get; }

    public LogServiceProxy(string name, LogManager manager = null) {
        Name = name;
        Manager = manager;
        DispatchQueue = new Domore.Threading.BackgroundQueue(Logging.Notify);
    }

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
            if (PendingItemCount >= QueueItemLimit ||
                messageBytes > QueueByteLimit ||
                PendingMessageBytes > QueueByteLimit - messageBytes) {
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
