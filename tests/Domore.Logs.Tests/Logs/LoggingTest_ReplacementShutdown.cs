using Domore.Threading;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Logs;

partial class LoggingTest {
    private sealed class ReplacementShutdownOldService : ILogService {
        public static Action Completion { get; set; }
        public static int CompleteCount;

        public void Log(string name, string data, LogSeverity severity) { }
        public void Complete() {
            Interlocked.Increment(ref CompleteCount);
            Completion?.Invoke();
        }
    }

    private sealed class ReplacementShutdownNewService : ILogService {
        public static ConcurrentQueue<string> Entries { get; } = new();
        public static int CompleteCount;

        public void Log(string name, string data, LogSeverity severity) => Entries.Enqueue(data);
        public void Complete() => Interlocked.Increment(ref CompleteCount);
    }

    private static LogManager ReplacementShutdownManager() {
        var config = Logging.Config;
        return (LogManager)config.GetType().GetProperty("Log").GetValue(config, null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ServiceReplacementShutdownReturnsBeforeQueuedDelivery(bool throwFromCompletion) {
        ReplacementShutdownOldService.CompleteCount = 0;
        ReplacementShutdownNewService.CompleteCount = 0;
        while (ReplacementShutdownNewService.Entries.TryDequeue(out _)) { }
        using var completionEntered = new ManualResetEventSlim();
        using var requestCompletion = new ManualResetEventSlim();
        using var completionReturned = new ManualResetEventSlim();
        using var workerEntered = new ManualResetEventSlim();
        var workerObservedReturn = false;
        var contextRestored = false;
        var manager = ReplacementShutdownManager();
        var proxy = manager["replacement"];
        proxy.Type = typeof(ReplacementShutdownOldService).AssemblyQualifiedName;
        proxy.Config.Default.Threshold = LogSeverity.Info;
        _ = proxy.Service;
        ReplacementShutdownOldService.Completion = () => {
            completionEntered.Set();
            if (!requestCompletion.Wait(TimeSpan.FromSeconds(5))) {
                throw new TimeoutException("The test did not release replacement completion.");
            }
            Logging.Complete();
            completionReturned.Set();
            if (throwFromCompletion) {
                throw new InvalidOperationException("Expected replacement completion failure.");
            }
        };
        var replacement = Task.Run(() => {
            try {
                proxy.Type = typeof(ReplacementShutdownNewService).AssemblyQualifiedName;
                return (Exception)null;
            }
            catch (Exception ex) {
                return ex;
            }
            finally {
                contextRestored = typeof(Logging).GetField("UsedManager", BindingFlags.Static | BindingFlags.NonPublic)
                    .GetValue(null) == null;
            }
        });
        try {
            Assert.That(completionEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            var queue = (BackgroundQueue)typeof(LogServiceProxy)
                .GetField("DispatchQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(proxy);
            queue.Add(() => {
                workerEntered.Set();
                // A synchronous shutdown would wait for this worker while holding the proxy lock.
                // On failure, skip delivery after a bounded wait so the runner can still shut down.
                workerObservedReturn = completionReturned.Wait(TimeSpan.FromSeconds(2));
                if (workerObservedReturn) {
                    proxy.Log(new LogEntry(typeof(LoggingTest), DateTime.UtcNow, LogSeverity.Info, ["queued during replacement"]));
                }
            });
            Assert.That(workerEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            requestCompletion.Set();
            Assert.That(replacement.Wait(TimeSpan.FromSeconds(5)), Is.True);
            WaitForDeferredCompletions();
            Assert.Multiple(() => {
                Assert.That(workerObservedReturn, Is.True, "Replacement completion must return before waiting for queued delivery.");
                Assert.That(contextRestored, Is.True, "Callback context must be restored even when completion throws.");
                Assert.That(replacement.Result, throwFromCompletion ? Is.TypeOf<InvalidOperationException>() : Is.Null);
                Assert.That(ReplacementShutdownNewService.Entries.ToArray(), Is.EqualTo(["queued during replacement"]));
                Assert.That(ReplacementShutdownOldService.CompleteCount, Is.EqualTo(1));
                Assert.That(ReplacementShutdownNewService.CompleteCount, Is.EqualTo(1));
            });
        }
        finally {
            requestCompletion.Set();
            replacement.Wait(TimeSpan.FromSeconds(5));
            ReplacementShutdownOldService.Completion = null;
        }
    }

    [Test]
    public void ReplacementCompletionOnRetainedConfigurationCannotRetireNewSession() {
        var retiredManager = ReplacementShutdownManager();
        Logging.Complete();
        var subscription = new DeferredCompletionSubscription();
        Logging.Subscribe(subscription);
        var proxy = retiredManager["late replacement"];
        proxy.Type = typeof(ReplacementShutdownOldService).AssemblyQualifiedName;
        _ = proxy.Service;
        ReplacementShutdownOldService.Completion = Logging.Complete;
        try {
            proxy.Type = typeof(ReplacementShutdownNewService).AssemblyQualifiedName;
            WaitForDeferredCompletions();
            Assert.That(subscription.Detached.IsSet, Is.False,
                "A completion callback belonging to retired configuration must not shut down the active manager.");
            Log.Info("new session survives replacement callback");
            Assert.That(subscription.Entries.ToArray(), Is.EqualTo(["new session survives replacement callback"]));
        }
        finally {
            ReplacementShutdownOldService.Completion = null;
            proxy.Complete();
        }
    }
}
