using Domore.Conf.Logs;
using Domore.Logs.Service;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs;

[TestFixture]
[NonParallelizable]
internal sealed class QueueLimitsTest {
    private readonly List<ILogService> FileServices = [];
    private string TempDir {
        get => field ??=
            Path.Combine(Path.GetTempPath(),
                         "domore.logs.queue-limits",
                         Guid.NewGuid().ToString("N"));
        set;
    }

    private sealed class GateService : ILogService {
        public static readonly ManualResetEventSlim Entered = new(false);
        public static readonly ManualResetEventSlim Release = new(false);
        public static readonly ConcurrentQueue<string> Received = new();
        public static int CompleteCount;

        public static void Reset() {
            Entered.Reset();
            Release.Reset();
            Interlocked.Exchange(ref CompleteCount, 0);
            while (Received.TryDequeue(out _)) { }
        }

        public void Log(string name, string data, LogSeverity severity) {
            if (data == "gate") {
                Entered.Set();
                if (!Release.Wait(TimeSpan.FromSeconds(15))) {
                    throw new TimeoutException("The logging test did not release the service gate.");
                }
            }
            Received.Enqueue(data);
        }

        public void Complete() => Interlocked.Increment(ref CompleteCount);
    }

    private sealed class FastService : ILogService {
        public static readonly ConcurrentQueue<string> Received = new();
        public static int CompleteCount;

        public static void Reset() {
            Interlocked.Exchange(ref CompleteCount, 0);
            while (Received.TryDequeue(out _)) { }
        }

        public void Log(string name, string data, LogSeverity severity) => Received.Enqueue(data);
        public void Complete() => Interlocked.Increment(ref CompleteCount);
    }

    private sealed class StatusProviderService : ILogService, ILogQueueStatusProvider {
        public static readonly ManualResetEventSlim LogCalled = new(false);
        public static readonly ConcurrentQueue<bool> EnabledChecks = new();
        public static int ConstructedCount;
        public static int StatusReadCount;
        public static bool ThrowOnStatus;

        public StatusProviderService() => Interlocked.Increment(ref ConstructedCount);

        public static void Reset() {
            LogCalled.Reset();
            Interlocked.Exchange(ref ConstructedCount, 0);
            Interlocked.Exchange(ref StatusReadCount, 0);
            ThrowOnStatus = false;
            while (EnabledChecks.TryDequeue(out _)) { }
        }

        public LogQueueStatistics QueueStatus {
            get {
                Interlocked.Increment(ref StatusReadCount);
                var logger = Logging.For(typeof(QueueLimitsTest));
                EnabledChecks.Enqueue(logger.Info());
                logger.Info("status provider synchronous feedback");
                var asyncEnabled = Task.Run(() => {
                    var asyncLogger = Logging.For(typeof(QueueLimitsTest));
                    var enabled = asyncLogger.Info();
                    asyncLogger.Info("status provider asynchronous feedback");
                    return enabled;
                }).GetAwaiter().GetResult();
                EnabledChecks.Enqueue(asyncEnabled);
                if (ThrowOnStatus) {
                    throw new InvalidOperationException("Expected queue status provider failure.");
                }
                return new LogQueueStatistics(7, 70, 0, 0, 0, 0);
            }
        }

        public void Log(string name, string data, LogSeverity severity) => LogCalled.Set();
        public void Complete() { }
    }

    private sealed class ReplacementOldService : ILogService {
        public static readonly ManualResetEventSlim Entered = new(false);
        public static readonly ManualResetEventSlim Release = new(false);
        public static readonly ConcurrentQueue<string> Received = new();
        public static LogServiceProxy Proxy;
        public static int CompleteCount;

        public static void Reset() {
            Entered.Reset();
            Release.Reset();
            Proxy = null;
            Interlocked.Exchange(ref CompleteCount, 0);
            while (Received.TryDequeue(out _)) { }
            ReplacementNewService.Reset();
        }

        public void Log(string name, string data, LogSeverity severity) {
            Received.Enqueue(data);
            if (data == "gate") {
                Entered.Set();
                if (!Release.Wait(TimeSpan.FromSeconds(15))) {
                    throw new TimeoutException("The replacement test did not release the service gate.");
                }
                Proxy.Type = typeof(ReplacementNewService).AssemblyQualifiedName;
            }
        }

        public void Complete() => Interlocked.Increment(ref CompleteCount);
    }

    private sealed class ReplacementNewService : ILogService {
        public static readonly ConcurrentQueue<string> Received = new();
        public static int CompleteCount;

        public static void Reset() {
            Interlocked.Exchange(ref CompleteCount, 0);
            while (Received.TryDequeue(out _)) { }
        }

        public void Log(string name, string data, LogSeverity severity) => Received.Enqueue(data);
        public void Complete() => Interlocked.Increment(ref CompleteCount);
    }

    private sealed class BlockingRecursiveWriter : TextWriter {
        private readonly ILog Log;
        public readonly ManualResetEventSlim Entered = new(false);
        public readonly ManualResetEventSlim Release = new(false);
        public readonly ManualResetEventSlim Returned = new(false);
        public int CallCount;

        public BlockingRecursiveWriter(ILog log) {
            Log = log;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string value) {
            Interlocked.Increment(ref CallCount);
            Entered.Set();
            try {
                Log.Info("recursive diagnostic feedback");
                if (!Release.Wait(TimeSpan.FromSeconds(15))) {
                    throw new TimeoutException("The test did not release the diagnostic writer.");
                }
            }
            finally {
                Returned.Set();
            }
        }

        public override void WriteLine(object value) => WriteLine(value?.ToString());
    }

    private sealed class ThrowingWriter : TextWriter {
        public readonly ManualResetEventSlim Attempted = new(false);
        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string value) {
            Attempted.Set();
            throw new InvalidOperationException("The diagnostic writer is unavailable.");
        }

        public override void WriteLine(object value) => WriteLine(value?.ToString());
    }

    [SetUp]
    public void SetUp() {
        GateService.Release.Set();
        ReplacementOldService.Release.Set();
        Logging.Complete();
        TempDir = null;
        FileServices.Clear();
        GateService.Reset();
        FastService.Reset();
        StatusProviderService.Reset();
        ReplacementOldService.Reset();
    }

    [TearDown]
    public void TearDown() {
        GateService.Release.Set();
        ReplacementOldService.Release.Set();
        Logging.Complete();
        foreach (var service in FileServices) {
            service.Complete();
        }
        if (Directory.Exists(TempDir)) {
            Directory.Delete(TempDir, recursive: true);
        }
    }

    [Test]
    public void ServiceDispatchDefaultsBoundPendingItemsAndKeepFifo() {
        Configure($@"
            log[gate].type = {typeof(GateService).AssemblyQualifiedName}
            log[gate].config.default.severity = info
        ");
        var logger = Logging.For(typeof(QueueLimitsTest));
        logger.Info("gate");
        Assert.That(GateService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);

        var accepted = new List<string>();
        for (var i = 0; i < 1025; i++) {
            var data = "item-" + i.ToString("D4");
            logger.Info(data);
            if (i < 1024) {
                accepted.Add(data);
            }
        }

        var statusTask = Task.Run(() => Logging.GetQueueStatus("gate"));
        Assert.That(statusTask.Wait(TimeSpan.FromSeconds(2)), Is.True,
            "Status must remain available while the service callback holds its delivery lock.");
        var status = statusTask.Result.DispatchQueue;
        var acceptedBytes = accepted.Sum(data => 2L * data.Length);
        Assert.Multiple(() => {
            Assert.That(status.ItemLimit, Is.EqualTo(1024));
            Assert.That(status.MessageByteLimit, Is.EqualTo(8L * 1024 * 1024));
            Assert.That(status.PendingItemCount, Is.EqualTo(1024));
            Assert.That(status.PendingMessageBytes, Is.EqualTo(acceptedBytes));
            Assert.That(status.DroppedItemCount, Is.EqualTo(1));
            Assert.That(status.DroppedMessageBytes, Is.EqualTo(2L * "item-1024".Length));
        });

        GateService.Release.Set();
        Logging.Complete();
        Assert.That(GateService.Received.ToArray(), Is.EqualTo(new[] { "gate" }.Concat(accepted)));
    }

    [Test]
    public void ServiceDispatchByteLimitRejectsNewestAndLimitDecreaseDoesNotEvict() {
        Configure($@"
            log[gate].type = {typeof(GateService).AssemblyQualifiedName}
            log[gate].queue item limit = 4
            log[gate].config.default.severity = info
        ");
        var logger = Logging.For(typeof(QueueLimitsTest));
        logger.Info("gate");
        Assert.That(GateService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Configure("log[gate].queue byte limit = 6");
        logger.Info("abc");
        logger.Info("d");

        Configure("log[gate].queue byte limit = 1");
        var beforeMoreDrops = Logging.GetQueueStatus("gate").DispatchQueue;
        logger.Info("x");
        var status = Logging.GetQueueStatus("gate").DispatchQueue;
        Assert.Multiple(() => {
            Assert.That(beforeMoreDrops.PendingItemCount, Is.EqualTo(1));
            Assert.That(beforeMoreDrops.PendingMessageBytes, Is.EqualTo(6));
            Assert.That(status.PendingItemCount, Is.EqualTo(1), "Lowering a limit must retain accepted work.");
            Assert.That(status.PendingMessageBytes, Is.EqualTo(6));
            Assert.That(status.MessageByteLimit, Is.EqualTo(1));
            Assert.That(status.DroppedItemCount, Is.EqualTo(2));
            Assert.That(status.DroppedMessageBytes, Is.EqualTo(4));
        });

        GateService.Release.Set();
        Logging.Complete();
        Assert.That(GateService.Received.ToArray(), Is.EqualTo(["gate", "abc"]));
    }

    [Test]
    public void ServiceDispatchRejectsOversizedMessageEvenWithNoPendingItems() {
        Configure($@"
            log[gate].type = {typeof(GateService).AssemblyQualifiedName}
            log[gate].queue item limit = 10
            log[gate].config.default.severity = info
        ");
        var logger = Logging.For(typeof(QueueLimitsTest));
        logger.Info("gate");
        Assert.That(GateService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Configure("log[gate].queue byte limit = 2");
        logger.Info("abc");
        var status = Logging.GetQueueStatus("gate").DispatchQueue;

        Assert.Multiple(() => {
            Assert.That(status.PendingItemCount, Is.Zero);
            Assert.That(status.PendingMessageBytes, Is.Zero);
            Assert.That(status.DroppedItemCount, Is.EqualTo(1));
            Assert.That(status.DroppedMessageBytes, Is.EqualTo(6));
        });

        GateService.Release.Set();
        Logging.Complete();
        Assert.That(GateService.Received.ToArray(), Is.EqualTo(["gate"]));
    }

    [Test]
    public void ServiceDispatchItemLimitDecreaseRetainsAcceptedItemsAndRejectsNewest() {
        Configure($@"
            log[gate].type = {typeof(GateService).AssemblyQualifiedName}
            log[gate].queue item limit = 4
            log[gate].config.default.severity = info
        ");
        var logger = Logging.For(typeof(QueueLimitsTest));
        logger.Info("gate");
        Assert.That(GateService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        logger.Info("accepted-1");
        logger.Info("accepted-2");

        Configure("log[gate].queue item limit = 1");
        logger.Info("newest");
        var status = Logging.GetQueueStatus("gate").DispatchQueue;
        Assert.Multiple(() => {
            Assert.That(status.ItemLimit, Is.EqualTo(1));
            Assert.That(status.PendingItemCount, Is.EqualTo(2));
            Assert.That(status.DroppedItemCount, Is.EqualTo(1));
            Assert.That(status.DroppedMessageBytes, Is.EqualTo(2L * "newest".Length));
        });

        GateService.Release.Set();
        Logging.Complete();
        Assert.That(GateService.Received.ToArray(), Is.EqualTo(["gate", "accepted-1", "accepted-2"]));
    }

    [Test]
    public void DefaultUtf16ByteLimitRejectsAnOversizedMessageForBothQueueKinds() {
        Configure($@"
            log[gate].type = {typeof(GateService).AssemblyQualifiedName}
            log[gate].config.default.severity = info
        ");
        var logger = Logging.For(typeof(QueueLimitsTest));
        logger.Info("gate");
        Assert.That(GateService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        const int textLength = 4 * 1024 * 1024 + 1;
        var oversized = new string('x', textLength);
        logger.Info(oversized);
        var dispatch = Logging.GetQueueStatus("gate").DispatchQueue;

        var fileLog = NewFileLog("file-default-bytes.log");
        ((ILogService)fileLog).Log("test", oversized, LogSeverity.Info);
        var fileStatus = fileLog.QueueStatus;
        Assert.Multiple(() => {
            Assert.That(dispatch.ItemLimit, Is.EqualTo(1024));
            Assert.That(dispatch.MessageByteLimit, Is.EqualTo(8L * 1024 * 1024));
            Assert.That(dispatch.PendingItemCount, Is.Zero);
            Assert.That(dispatch.PendingMessageBytes, Is.Zero);
            Assert.That(dispatch.DroppedItemCount, Is.EqualTo(1));
            Assert.That(dispatch.DroppedMessageBytes, Is.EqualTo(8L * 1024 * 1024 + 2));
            Assert.That(fileStatus.ItemLimit, Is.EqualTo(1024));
            Assert.That(fileStatus.MessageByteLimit, Is.EqualTo(8L * 1024 * 1024));
            Assert.That(fileStatus.PendingItemCount, Is.Zero);
            Assert.That(fileStatus.PendingMessageBytes, Is.Zero);
            Assert.That(fileStatus.DroppedItemCount, Is.EqualTo(1));
            Assert.That(fileStatus.DroppedMessageBytes, Is.EqualTo(8L * 1024 * 1024 + 2));
        });

        GateService.Release.Set();
        Logging.Complete();
        ((ILogService)fileLog).Complete();
    }

    [Test]
    public void SaturatedServiceDoesNotStopAnotherDestinationAndQueuesAreIsolated() {
        Configure($@"
            log[slow].type = {typeof(GateService).AssemblyQualifiedName}
            log[slow].queue item limit = 2
            log[slow].config.default.severity = info
            log[healthy].type = {typeof(FastService).AssemblyQualifiedName}
            log[healthy].config.default.severity = info
        ");
        var logger = Logging.For(typeof(QueueLimitsTest));
        logger.Info("gate");
        Assert.That(GateService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);

        var messages = new List<string>();
        for (var i = 0; i < 30; i++) {
            var data = "healthy-" + i.ToString("D2");
            messages.Add(data);
            logger.Info(data);
        }

        var healthyDrained = SpinWait.SpinUntil(
            () => FastService.Received.Count >= 31,
            TimeSpan.FromSeconds(3));
        var slowStatus = Logging.GetQueueStatus("slow").DispatchQueue;
        var healthyStatus = Logging.GetQueueStatus("healthy").DispatchQueue;
        Assert.Multiple(() => {
            Assert.That(healthyDrained, Is.True,
                "A blocked destination must not stop a healthy destination from draining its own queue.");
            Assert.That(slowStatus.PendingItemCount, Is.EqualTo(2));
            Assert.That(slowStatus.DroppedItemCount, Is.EqualTo(28));
            Assert.That(healthyStatus.DroppedItemCount, Is.Zero);
        });

        GateService.Release.Set();
        Logging.Complete();
        Assert.That(FastService.Received.ToArray(), Is.EqualTo(new[] { "gate" }.Concat(messages)));
        Assert.That(GateService.Received.ToArray(), Is.EqualTo(["gate", messages[0], messages[1]]));
    }

    [Test]
    public void QueueStatusConfigurationAndUnknownNamesUsePublicApiWithoutCreatingQueues() {
        Configure($@"
            log[file].type = file
            log[file].queue item limit = 3
            log[file].queue byte limit = 100
            log[file].service.directory = {TempDir}
            log[file].service.name = config.log
            log[file].service.flush interval = 01:00:00
            log[file].service.queue item limit = 4
            log[file].service.queue byte limit = 200
            log[file].config.default.severity = info
        ");
        var manager = CurrentManager();
        var services = (LogServiceCollection)typeof(LogManager)
            .GetField("Services", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
        var count = services.Count;
        var unknown = Logging.GetQueueStatus("not-configured");
        var status = Logging.GetQueueStatus("file");

        Assert.Multiple(() => {
            Assert.That(unknown, Is.Null);
            Assert.That(services.Count, Is.EqualTo(count), "A status query must not create a destination.");
            Assert.That(status.DispatchQueue.ItemLimit, Is.EqualTo(3));
            Assert.That(status.DispatchQueue.MessageByteLimit, Is.EqualTo(100));
            Assert.That(status.ServiceQueue.ItemLimit, Is.EqualTo(4));
            Assert.That(status.ServiceQueue.MessageByteLimit, Is.EqualTo(200));
            Assert.That(Logging.GetQueueStatus("file").ServiceQueue, Is.Not.Null);
        });
        Assert.Throws<ArgumentNullException>(() => Logging.GetQueueStatus(null));
    }

    [Test]
    public void QueueLimitsAndPublicStatisticsRejectInvalidValues() {
        var proxy = new LogServiceProxy("validation");
        var fileLog = new FileLog();
        try {
            Assert.Multiple(() => {
                Assert.Throws<ArgumentOutOfRangeException>(() => proxy.QueueItemLimit = 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => proxy.QueueItemLimit = -1);
                Assert.Throws<ArgumentOutOfRangeException>(() => proxy.QueueByteLimit = 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => proxy.QueueByteLimit = -1);
                Assert.Throws<ArgumentOutOfRangeException>(() => fileLog.QueueItemLimit = 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => fileLog.QueueItemLimit = -1);
                Assert.Throws<ArgumentOutOfRangeException>(() => fileLog.QueueByteLimit = 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => fileLog.QueueByteLimit = -1);
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new LogQueueStatistics(0, 1, 0, 0, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new LogQueueStatistics(1, 0, 0, 0, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new LogQueueStatistics(1, 1, -1, 0, 0, 0));
                Assert.DoesNotThrow(() => new LogQueueStatistics(1, 1, 0, 0, 0, 0));
            });
        }
        finally {
            proxy.DisposeQueue();
        }
    }

    [Test]
    public void OptionalStatusProviderRunsUnderCallbackGuardAndReadDoesNotCreateUnusedService() {
        Configure($@"
            log[provider].type = {typeof(StatusProviderService).AssemblyQualifiedName}
            log[provider].config.default.severity = info
            log[unused].type = {typeof(StatusProviderService).AssemblyQualifiedName}
            log[unused].config.default.severity = none
        ");
        var nestedEvents = 0;
        LogEventHandler handler = (_, args) => {
            var data = string.Join(Environment.NewLine, args.LogList);
            if (data.IndexOf("status provider", StringComparison.Ordinal) >= 0) {
                Interlocked.Increment(ref nestedEvents);
            }
        };
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += handler;
        try {
            Logging.For(typeof(QueueLimitsTest)).Info("create provider service");
            Assert.That(StatusProviderService.LogCalled.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(StatusProviderService.ConstructedCount, Is.EqualTo(1));

            var unused = Logging.GetQueueStatus("unused");
            Assert.That(unused.ServiceQueue, Is.Null);
            Assert.That(StatusProviderService.ConstructedCount, Is.EqualTo(1),
                "Reading status must not construct an unused service.");

            StatusProviderService.ThrowOnStatus = true;
            var failed = Logging.GetQueueStatus("provider");
            Assert.That(failed.ServiceQueue, Is.Null,
                "An optional service status callback failure must be contained.");
            StatusProviderService.ThrowOnStatus = false;
            var status = Logging.GetQueueStatus("provider");

            Assert.Multiple(() => {
                Assert.That(status.ServiceQueue.ItemLimit, Is.EqualTo(7));
                Assert.That(status.ServiceQueue.MessageByteLimit, Is.EqualTo(70));
                Assert.That(StatusProviderService.StatusReadCount, Is.EqualTo(2));
                Assert.That(StatusProviderService.EnabledChecks.ToArray(), Is.All.False,
                    "Synchronous and flowed asynchronous enablement checks are suppressed.");
                Assert.That(nestedEvents, Is.Zero,
                    "Synchronous and flowed asynchronous nested logging is suppressed.");
            });
        }
        finally {
            Logging.Event -= handler;
            Logging.EventThreshold = LogSeverity.None;
        }
    }

    [Test]
    public void FileLogDefaultsBoundItemsAndKeepAcceptedFifoOnCompletion() {
        var fileLog = NewFileLog("file-count.log");
        var service = (ILogService)fileLog;
        var accepted = new List<string>();
        for (var i = 0; i < 1024; i++) {
            var data = "accepted-" + i.ToString("D4");
            accepted.Add(data);
            service.Log("test", data, LogSeverity.Info);
        }
        service.Log("test", "newest", LogSeverity.Info);
        var status = fileLog.QueueStatus;
        Assert.Multiple(() => {
            Assert.That(status.ItemLimit, Is.EqualTo(1024));
            Assert.That(status.MessageByteLimit, Is.EqualTo(8L * 1024 * 1024));
            Assert.That(status.PendingItemCount, Is.EqualTo(1024));
            Assert.That(status.PendingMessageBytes, Is.EqualTo(accepted.Sum(data => 2L * data.Length)));
            Assert.That(status.DroppedItemCount, Is.EqualTo(1));
            Assert.That(status.DroppedMessageBytes, Is.EqualTo(2L * "newest".Length));
        });

        service.Complete();
        Assert.That(File.ReadAllLines(Path.Combine(TempDir, "file-count.log")), Is.EqualTo(accepted));
    }

    [Test]
    public void FileLogByteLimitAndLimitReductionRetainAcceptedMessages() {
        var fileLog = NewFileLog("file-bytes.log");
        fileLog.QueueItemLimit = 3;
        fileLog.QueueByteLimit = 6;
        var service = (ILogService)fileLog;
        service.Log("test", "abc", LogSeverity.Info);
        service.Log("test", "d", LogSeverity.Info);
        fileLog.QueueByteLimit = 1;
        var statusBeforeLatest = fileLog.QueueStatus;
        service.Log("test", "x", LogSeverity.Info);
        fileLog.QueueItemLimit = 1;
        var status = fileLog.QueueStatus;

        Assert.Multiple(() => {
            Assert.That(statusBeforeLatest.PendingItemCount, Is.EqualTo(1));
            Assert.That(statusBeforeLatest.PendingMessageBytes, Is.EqualTo(6));
            Assert.That(status.PendingItemCount, Is.EqualTo(1));
            Assert.That(status.PendingMessageBytes, Is.EqualTo(6));
            Assert.That(status.ItemLimit, Is.EqualTo(1));
            Assert.That(status.MessageByteLimit, Is.EqualTo(1));
            Assert.That(status.DroppedItemCount, Is.EqualTo(2));
            Assert.That(status.DroppedMessageBytes, Is.EqualTo(4));
        });

        service.Complete();
        Assert.That(File.ReadAllLines(Path.Combine(TempDir, "file-bytes.log")), Is.EqualTo(["abc"]));
        Assert.Throws<ArgumentOutOfRangeException>(() => fileLog.QueueItemLimit = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => fileLog.QueueByteLimit = 0);
    }

    [Test]
    public void FileLogItemLimitDecreaseRetainsAcceptedItemsAndRejectsNewest() {
        var fileLog = NewFileLog("file-count-reduction.log");
        fileLog.QueueItemLimit = 4;
        var service = (ILogService)fileLog;
        service.Log("test", "accepted-1", LogSeverity.Info);
        service.Log("test", "accepted-2", LogSeverity.Info);

        fileLog.QueueItemLimit = 1;
        service.Log("test", "newest", LogSeverity.Info);
        var status = fileLog.QueueStatus;
        Assert.Multiple(() => {
            Assert.That(status.ItemLimit, Is.EqualTo(1));
            Assert.That(status.PendingItemCount, Is.EqualTo(2));
            Assert.That(status.DroppedItemCount, Is.EqualTo(1));
            Assert.That(status.DroppedMessageBytes, Is.EqualTo(2L * "newest".Length));
        });

        service.Complete();
        Assert.That(File.ReadAllLines(Path.Combine(TempDir, "file-count-reduction.log")),
            Is.EqualTo(["accepted-1", "accepted-2"]));
    }

    [Test]
    public void FileLogRejectsOversizedMessageWithAnEmptyQueue() {
        var fileLog = NewFileLog("file-oversized.log");
        fileLog.QueueByteLimit = 2;
        ((ILogService)fileLog).Log("test", "abc", LogSeverity.Info);
        var status = fileLog.QueueStatus;
        Assert.Multiple(() => {
            Assert.That(status.PendingItemCount, Is.Zero);
            Assert.That(status.PendingMessageBytes, Is.Zero);
            Assert.That(status.DroppedItemCount, Is.EqualTo(1));
            Assert.That(status.DroppedMessageBytes, Is.EqualTo(6));
        });
    }

    [Test]
    public void FileLogFullRejectionDoesNotWaitForTheIoLock() {
        var fileLog = NewFileLog("file-nonblocking.log");
        fileLog.QueueItemLimit = 1;
        var service = (ILogService)fileLog;
        service.Log("test", "accepted", LogSeverity.Info);
        var locker = typeof(FileLog).GetField("Locker", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(fileLog);
        var completed = false;
        Task attempt = null;
        EnterFileLogLock(locker);
        try {
            using var started = new ManualResetEventSlim(false);
            attempt = Task.Run(() => {
                started.Set();
                service.Log("test", "newest", LogSeverity.Info);
            });
            Assert.That(started.Wait(TimeSpan.FromSeconds(2)), Is.True);
            completed = attempt.Wait(TimeSpan.FromSeconds(2));
        }
        finally {
            ExitFileLogLock(locker);
        }
        Assert.That(attempt.Wait(TimeSpan.FromSeconds(5)), Is.True);
        var status = fileLog.QueueStatus;
        Assert.Multiple(() => {
            Assert.That(completed, Is.True,
                "A full FileLog queue must reject before asking for the IO/settings lock.");
            Assert.That(status.PendingItemCount, Is.EqualTo(1));
            Assert.That(status.DroppedItemCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void FileLogConcurrentAdmissionAndTimerFlushKeepEveryAcceptedLine() {
        var fileLog = NewFileLog("file-concurrent.log");
        fileLog.QueueItemLimit = 1024;
        fileLog.QueueByteLimit = 1024 * 1024;
        fileLog.LogCountLimit = 7;
        fileLog.FlushInterval = TimeSpan.FromMilliseconds(2);
        var service = (ILogService)fileLog;
        var expected = new ConcurrentQueue<string>();
        Parallel.For(0, 8, worker => {
            for (var item = 0; item < 100; item++) {
                var data = $"worker-{worker}-item-{item}";
                expected.Enqueue(data);
                service.Log("test", data, LogSeverity.Info);
            }
        });
        Assert.That(fileLog.QueueStatus.DroppedItemCount, Is.Zero);
        service.Complete();
        var actual = File.ReadAllLines(Path.Combine(TempDir, "file-concurrent.log"));

        Assert.That(actual, Is.EquivalentTo(expected.ToArray()));
    }

    [Test]
    public void FileLogCompletionWaitsForInflightTimerWriteAndConcurrentCompletion() {
        Directory.CreateDirectory(TempDir);
        var fileLog = NewFileLog("completion-race.log");
        fileLog.IORetryLimit = 100;
        fileLog.IORetryDelay = 100;
        fileLog.FlushInterval = TimeSpan.FromMilliseconds(10);
        var path = Path.Combine(TempDir, "completion-race.log");
        File.WriteAllText(path, "");
        var ioLock = typeof(FileLog).GetField("Locker", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(fileLog);
        var service = (ILogService)fileLog;
        using var fileLock = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        service.Log("test", "accepted before completion", LogSeverity.Info);

        var inFlight = SpinWait.SpinUntil(() => {
            if (TryEnterFileLogLock(ioLock)) {
                ExitFileLogLock(ioLock);
                return false;
            }
            return fileLog.QueueStatus.PendingItemCount == 0;
        }, TimeSpan.FromSeconds(5));
        Assert.That(inFlight, Is.True, "The timer must take the accepted item into its in-flight batch.");

        using var firstStarted = new ManualResetEventSlim();
        using var secondStarted = new ManualResetEventSlim();
        var firstComplete = Task.Run(() => {
            firstStarted.Set();
            service.Complete();
        });
        var secondComplete = Task.Run(() => {
            secondStarted.Set();
            service.Complete();
        });
        Assert.That(firstStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(secondStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);
        var bothWaitForWrite = !firstComplete.Wait(TimeSpan.FromMilliseconds(30)) &&
                               !secondComplete.Wait(TimeSpan.FromMilliseconds(30));
        fileLock.Dispose();
        Assert.That(bothWaitForWrite, Is.True,
            "Every completion caller must wait for an already-started accepted write.");
        Assert.That(Task.WaitAll([firstComplete, secondComplete], TimeSpan.FromSeconds(10)), Is.True);
        Assert.That(File.ReadAllLines(path), Is.EqualTo(["accepted before completion"]));
    }

    [Test]
    public void TimedOutManagerRetirementLaterDrainsEveryAcceptedDestinationItem() {
        Configure($@"
            log[gate].type = {typeof(GateService).AssemblyQualifiedName}
            log[gate].queue item limit = 2
            log[gate].config.default.severity = info
        ");
        var logger = Logging.For(typeof(QueueLimitsTest));
        logger.Info("gate");
        Assert.That(GateService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        logger.Info("accepted-1");
        logger.Info("accepted-2");
        logger.Info("dropped");

        Assert.That(Logging.Complete(TimeSpan.FromMilliseconds(100)), Is.False);
        GateService.Release.Set();
        Assert.That(Logging.Complete(TimeSpan.FromSeconds(5)), Is.True);
        Assert.Multiple(() => {
            Assert.That(GateService.Received.ToArray(), Is.EqualTo(["gate", "accepted-1", "accepted-2"]));
            Assert.That(GateService.CompleteCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReplacementCompletesOldServiceAndDrainsAcceptedWorkUnderSaturation() {
        Configure($@"
            log[replace].type = {typeof(ReplacementOldService).AssemblyQualifiedName}
            log[replace].queue item limit = 2
            log[replace].config.default.severity = info
        ");
        ReplacementOldService.Proxy = CurrentManager()["replace"];
        var logger = Logging.For(typeof(QueueLimitsTest));
        logger.Info("gate");
        Assert.That(ReplacementOldService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        logger.Info("accepted-1");
        logger.Info("accepted-2");
        logger.Info("dropped");
        var beforeRelease = Logging.GetQueueStatus("replace").DispatchQueue;
        Assert.That(beforeRelease.DroppedItemCount, Is.EqualTo(1));

        ReplacementOldService.Release.Set();
        var delivered = SpinWait.SpinUntil(
            () => ReplacementNewService.Received.Count == 2,
            TimeSpan.FromSeconds(5));
        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(delivered, Is.True);
            Assert.That(ReplacementOldService.Received.ToArray(), Is.EqualTo(["gate"]));
            Assert.That(ReplacementNewService.Received.ToArray(), Is.EqualTo(["accepted-1", "accepted-2"]));
            Assert.That(ReplacementOldService.CompleteCount, Is.EqualTo(1));
            Assert.That(ReplacementNewService.CompleteCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SlowThrowingAndRecursiveDiagnosticWritersCannotStallQueueRejection() {
        Configure($@"
            log[gate].type = {typeof(GateService).AssemblyQualifiedName}
            log[gate].queue item limit = 1
            log[gate].config.default.severity = info
        ");
        var logger = Logging.For(typeof(QueueLimitsTest));
        var previousError = Console.Error;
        var recursiveEvents = 0;
        LogEventHandler handler = (_, args) => {
            if (string.Join(Environment.NewLine, args.LogList) == "recursive diagnostic feedback") {
                Interlocked.Increment(ref recursiveEvents);
            }
        };
        var writer = new BlockingRecursiveWriter(logger);
        var throwingWriter = new ThrowingWriter();
        var handlerInstalled = false;
        try {
            Logging.EventThreshold = LogSeverity.Info;
            Logging.Event += handler;
            handlerInstalled = true;
            Console.SetError(writer);
            logger.Info("gate");
            Assert.That(GateService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            logger.Info("queued");

            Assert.That(WaitForDiagnosticSlot(TimeSpan.FromSeconds(5)), Is.True,
                "The prior coalesced diagnostic must finish before this writer is installed.");
            logger.Info("first rejection");
            Assert.That(writer.Entered.Wait(TimeSpan.FromSeconds(3)), Is.True);
            var rejectionBurst = Task.Run(() => {
                for (var i = 0; i < 75; i++) {
                    logger.Info("drop-" + i);
                }
            });
            Assert.That(rejectionBurst.Wait(TimeSpan.FromSeconds(2)), Is.True,
                "Rejections must finish while the asynchronous diagnostic writer remains blocked.");
            var status = Logging.GetQueueStatus("gate").DispatchQueue;
            Assert.Multiple(() => {
                Assert.That(writer.CallCount, Is.EqualTo(1), "Concurrent rejections must coalesce diagnostics.");
                Assert.That(status.DroppedItemCount, Is.EqualTo(76));
                Assert.That(recursiveEvents, Is.Zero, "Diagnostic feedback must be guarded.");
            });

            writer.Release.Set();
            Assert.That(writer.Returned.Wait(TimeSpan.FromSeconds(3)), Is.True);
            var reportField = typeof(LogQueueDiagnostics).GetField("Reporting", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(SpinWait.SpinUntil(() => (int)reportField.GetValue(null) == 0, TimeSpan.FromSeconds(3)), Is.True);

            Console.SetError(throwingWriter);
            var emittedDuringThrowingAttempt = 0;
            var throwerObserved = SpinWait.SpinUntil(() => {
                if (throwingWriter.Attempted.IsSet) {
                    return true;
                }
                logger.Info("drop after throwing writer installed");
                emittedDuringThrowingAttempt++;
                Thread.Yield();
                return throwingWriter.Attempted.IsSet;
            }, TimeSpan.FromSeconds(5));
            Assert.That(throwerObserved, Is.True,
                "A later coalesced report must reach the throwing diagnostic writer.");
            Assert.That(throwingWriter.Attempted.Wait(TimeSpan.FromSeconds(3)), Is.True);
            Assert.That(SpinWait.SpinUntil(() => (int)reportField.GetValue(null) == 0, TimeSpan.FromSeconds(3)), Is.True);
            Assert.That(Logging.GetQueueStatus("gate").DispatchQueue.DroppedItemCount,
                Is.EqualTo(76 + emittedDuringThrowingAttempt));
        }
        finally {
            writer.Release.Set();
            Console.SetError(previousError);
            if (handlerInstalled) {
                Logging.Event -= handler;
            }
            Logging.EventThreshold = LogSeverity.None;
            GateService.Release.Set();
        }
        Logging.Complete();
    }

    private static void Configure(string source) =>
        CONF.Contain(source).ConfigureLogging();

    private static bool WaitForDiagnosticSlot(TimeSpan timeout) {
        var reportField = typeof(LogQueueDiagnostics).GetField("Reporting", BindingFlags.Static | BindingFlags.NonPublic);
        var nextField = typeof(LogQueueDiagnostics).GetField("NextReportTimestamp", BindingFlags.Static | BindingFlags.NonPublic);
        return SpinWait.SpinUntil(() =>
            (int)reportField.GetValue(null) == 0 &&
            Stopwatch.GetTimestamp() >= (long)nextField.GetValue(null), timeout);
    }

    private static void EnterFileLogLock(object locker) {
#if NET9_0_OR_GREATER
        if (locker is Lock serviceLock) {
            serviceLock.Enter();
            return;
        }
#endif
        Monitor.Enter(locker);
    }

    private static void ExitFileLogLock(object locker) {
#if NET9_0_OR_GREATER
        if (locker is Lock serviceLock) {
            serviceLock.Exit();
            return;
        }
#endif
        Monitor.Exit(locker);
    }

    private static bool TryEnterFileLogLock(object locker) {
#if NET9_0_OR_GREATER
        if (locker is Lock serviceLock) {
            return serviceLock.TryEnter();
        }
#endif
        return Monitor.TryEnter(locker);
    }

    private static LogManager CurrentManager() {
        var config = Logging.Config;
        return (LogManager)config.GetType().GetProperty("Log").GetValue(config, null);
    }

    private FileLog NewFileLog(string name) {
        var fileLog = new FileLog {
            Directory = TempDir,
            Name = name,
            FlushInterval = TimeSpan.FromHours(1)
        };
        FileServices.Add((ILogService)fileLog);
        return fileLog;
    }
}
