using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Domore.Logs;
public sealed partial class LoggingTest {
    [Test]
    public void ServicePipelineContinuesAfterThrowingService() {
        ThrowingPipelineLogService.Reset();
        ReceivingPipelineLogService.Reset();

        var collection = new LogServiceCollection();
        try {
            _ = collection["a"];
            _ = collection["b"];
            var set = (Dictionary<string, LogServiceProxy>)typeof(LogServiceCollection)
                .GetField("Set", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(collection);
            var proxies = set.Values.ToArray();
            Assert.That(proxies, Has.Length.EqualTo(2));

            proxies[0].Type = typeof(ThrowingPipelineLogService).AssemblyQualifiedName;
            proxies[1].Type = typeof(ReceivingPipelineLogService).AssemblyQualifiedName;
            foreach (var proxy in proxies) {
                proxy.Config.Default.Threshold = LogSeverity.Info;
            }

            collection.Send(new LogEntry(
                typeof(LoggingTest),
                DateTime.UtcNow,
                LogSeverity.Info,
                new[] { "pipeline message" }));

            var received = ReceivingPipelineLogService.Received.Wait(WaitTimeout);
            Assert.Multiple(() => {
                Assert.That(ThrowingPipelineLogService.LogCalls, Is.EqualTo(1));
                Assert.That(received, Is.True, "the service after the throwing service did not receive the entry");
                Assert.That(ReceivingPipelineLogService.Messages, Is.EqualTo(new[] { "pipeline message" }));
            });
        }
        finally {
            collection.Dispose();
        }
    }

    [Test]
    public void ServicePipelineCompletesOldServiceAfterChangingType() {
        ChangingPipelineLogServiceA.Reset();
        ChangingPipelineLogServiceB.Reset();

        var log = Logging.For(typeof(LoggingTest));
        ConfigurePipelineService("changing", typeof(ChangingPipelineLogServiceA));
        log.Info("first entry");

        var firstReceived = ChangingPipelineLogServiceA.Received.Wait(WaitTimeout);
        ConfigurePipelineService("changing", typeof(ChangingPipelineLogServiceB));
        log.Info("second entry");

        var secondReceived = ChangingPipelineLogServiceB.Received.Wait(WaitTimeout);
        var oldServiceCompletedBeforeLoggingComplete = ChangingPipelineLogServiceA.Completed.Wait(WaitTimeout);
        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(firstReceived, Is.True, "the first service did not receive the first entry");
            Assert.That(secondReceived, Is.True, "the replacement service did not receive the second entry");
            Assert.That(oldServiceCompletedBeforeLoggingComplete, Is.True,
                "the old service was not completed when its type changed");
            Assert.That(ChangingPipelineLogServiceA.Messages, Is.EqualTo(new[] { "LoggingTest first entry" }));
            Assert.That(ChangingPipelineLogServiceB.Messages, Is.EqualTo(new[] { "LoggingTest second entry" }));
        });
    }

    [Test]
    public void ServicePipelineDefaultThresholdIsVisibleDuringConcurrentFirstAccess() {
        const int iterations = 2000;
        using var barrier = new Barrier(3);
        var failureLocker = new object();
        var workerFailure = default(Exception);
        var enabledFailures = 0;
        var synchronized = true;
        LogServiceConfig currentConfig = null;

        void SetWorkerFailure(Exception exception) {
            lock (failureLocker) {
                workerFailure ??= exception;
            }
        }

        void RunWorker(bool setThreshold) {
            for (var i = 0; i < iterations; i++) {
                try {
                    if (barrier.SignalAndWait(WaitTimeout) == false) {
                        return;
                    }
                    var config = currentConfig;
                    if (setThreshold) {
                        config.Default.Threshold = LogSeverity.Debug;
                    }
                    else {
                        _ = config.Default;
                    }
                }
                catch (Exception ex) {
                    SetWorkerFailure(ex);
                }

                try {
                    if (barrier.SignalAndWait(WaitTimeout) == false) {
                        return;
                    }
                }
                catch (Exception ex) {
                    SetWorkerFailure(ex);
                    return;
                }
            }
        }

        var firstAccess = new Thread(() => RunWorker(setThreshold: false)) {
            IsBackground = true
        };
        var setter = new Thread(() => RunWorker(setThreshold: true)) {
            IsBackground = true
        };
        firstAccess.Start();
        setter.Start();

        for (var i = 0; i < iterations; i++) {
            var collection = new LogServiceCollection();
            try {
                currentConfig = collection[$"race-{i}"].Config;
                var started = barrier.SignalAndWait(WaitTimeout);
                var completed = barrier.SignalAndWait(WaitTimeout);
                if (started == false || completed == false) {
                    synchronized = false;
                    break;
                }
                if (collection.Send(LogSeverity.Debug, typeof(LoggingTest)) == false) {
                    enabledFailures++;
                }
            }
            finally {
                collection.Dispose();
            }
        }

        var firstAccessStopped = firstAccess.Join(WaitTimeout);
        var setterStopped = setter.Join(WaitTimeout);

        Assert.Multiple(() => {
            Assert.That(synchronized, Is.True, "the threshold stress workers did not synchronize");
            Assert.That(firstAccessStopped, Is.True, "the first-access worker did not stop");
            Assert.That(setterStopped, Is.True, "the threshold setter worker did not stop");
            Assert.That(workerFailure, Is.Null);
            Assert.That(enabledFailures, Is.Zero,
                "Enabled did not observe a concurrently configured default threshold");
        });
    }

    [Test]
    public void ServicePipelineEnabledIncludesOtherServicesDefaultThreshold() {
        DefaultThresholdPipelineLogService.Reset();
        ExplicitThresholdPipelineLogService.Reset();

        Config = $@"
log[x].type = {typeof(DefaultThresholdPipelineLogService).AssemblyQualifiedName}
log[x].config.default.severity = debug
log[x].config.default.format = {{log}}
log[y].type = {typeof(ExplicitThresholdPipelineLogService).AssemblyQualifiedName}
log[y].config[{typeof(LoggingTest).Name}].severity = error
log[y].config.default.format = {{log}}
";
        var log = Logging.For(typeof(LoggingTest));
        var enabled = log.Enabled(LogSeverity.Debug);
        if (enabled) {
            log.Debug("mixed threshold entry");
        }

        var received = DefaultThresholdPipelineLogService.Received.Wait(WaitTimeout);
        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(enabled, Is.True);
            Assert.That(received, Is.True, "the service with a default Debug threshold missed the entry");
            Assert.That(DefaultThresholdPipelineLogService.Messages,
                Is.EqualTo(new[] { "LoggingTest mixed threshold entry" }));
            Assert.That(ExplicitThresholdPipelineLogService.Messages, Is.Empty);
        });
    }

    private void ConfigurePipelineService(string name, Type serviceType) {
        Config = $"log[{name}].type = {serviceType.AssemblyQualifiedName}" + Environment.NewLine +
            $"log[{name}].config.default.severity = info" + Environment.NewLine +
            $"log[{name}].config.default.format = {{log}}";
    }

    private sealed class ThrowingPipelineLogService : ILogService {
        private static readonly object Locker = new();
        private static int _LogCalls;

        public static int LogCalls {
            get {
                lock (Locker) {
                    return _LogCalls;
                }
            }
        }

        public ThrowingPipelineLogService() {
        }

        public static void Reset() {
            lock (Locker) {
                _LogCalls = 0;
            }
        }

        void ILogService.Complete() {
        }

        void ILogService.Log(string name, string data, LogSeverity severity) {
            lock (Locker) {
                _LogCalls++;
            }
            throw new InvalidOperationException("intentional pipeline test failure");
        }
    }

    private sealed class ReceivingPipelineLogService : ILogService {
        private static readonly object Locker = new();
        private static readonly List<string> Data = new();

        public static ManualResetEventSlim Received { get; } = new();

        public static string[] Messages {
            get {
                lock (Locker) {
                    return Data.ToArray();
                }
            }
        }

        public ReceivingPipelineLogService() {
        }

        public static void Reset() {
            lock (Locker) {
                Data.Clear();
            }
            Received.Reset();
        }

        void ILogService.Complete() {
        }

        void ILogService.Log(string name, string data, LogSeverity severity) {
            lock (Locker) {
                Data.Add(data);
            }
            Received.Set();
        }
    }

    private sealed class ChangingPipelineLogServiceA : ILogService {
        private static readonly object Locker = new();
        private static readonly List<string> Data = new();

        public static ManualResetEventSlim Received { get; } = new();
        public static ManualResetEventSlim Completed { get; } = new();

        public static string[] Messages {
            get {
                lock (Locker) {
                    return Data.ToArray();
                }
            }
        }

        public ChangingPipelineLogServiceA() {
        }

        public static void Reset() {
            lock (Locker) {
                Data.Clear();
            }
            Received.Reset();
            Completed.Reset();
        }

        void ILogService.Complete() {
            Completed.Set();
        }

        void ILogService.Log(string name, string data, LogSeverity severity) {
            lock (Locker) {
                Data.Add(data);
            }
            Received.Set();
        }
    }

    private sealed class ChangingPipelineLogServiceB : ILogService {
        private static readonly object Locker = new();
        private static readonly List<string> Data = new();

        public static ManualResetEventSlim Received { get; } = new();

        public static string[] Messages {
            get {
                lock (Locker) {
                    return Data.ToArray();
                }
            }
        }

        public ChangingPipelineLogServiceB() {
        }

        public static void Reset() {
            lock (Locker) {
                Data.Clear();
            }
            Received.Reset();
        }

        void ILogService.Complete() {
        }

        void ILogService.Log(string name, string data, LogSeverity severity) {
            lock (Locker) {
                Data.Add(data);
            }
            Received.Set();
        }
    }

    private sealed class DefaultThresholdPipelineLogService : ILogService {
        private static readonly object Locker = new();
        private static readonly List<string> Data = new();

        public static ManualResetEventSlim Received { get; } = new();

        public static string[] Messages {
            get {
                lock (Locker) {
                    return Data.ToArray();
                }
            }
        }

        public DefaultThresholdPipelineLogService() {
        }

        public static void Reset() {
            lock (Locker) {
                Data.Clear();
            }
            Received.Reset();
        }

        void ILogService.Complete() {
        }

        void ILogService.Log(string name, string data, LogSeverity severity) {
            lock (Locker) {
                Data.Add(data);
            }
            Received.Set();
        }
    }

    private sealed class ExplicitThresholdPipelineLogService : ILogService {
        private static readonly object Locker = new();
        private static readonly List<string> Data = new();

        public static string[] Messages {
            get {
                lock (Locker) {
                    return Data.ToArray();
                }
            }
        }

        public ExplicitThresholdPipelineLogService() {
        }

        public static void Reset() {
            lock (Locker) {
                Data.Clear();
            }
        }

        void ILogService.Complete() {
        }

        void ILogService.Log(string name, string data, LogSeverity severity) {
            lock (Locker) {
                Data.Add(data);
            }
        }
    }
}
