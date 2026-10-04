using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs;

partial class LoggingTest {
    public class ReentrantConfiguredService : ILogService {
        public static ConcurrentQueue<ReentrantConfiguredService> Instances { get; } = new();
        public static ConcurrentQueue<string> Events { get; } = new();
        public static Action<ReentrantConfiguredService, string> Receive;
        public static Action<ReentrantConfiguredService> Completing;
        public ConcurrentQueue<string> Entries { get; } = new();
        public string Option { get; set; }
        public int CompleteCount;

        public ReentrantConfiguredService() => Instances.Enqueue(this);
        public void Log(string name, string data, LogSeverity severity) {
            Events.Enqueue($"{GetType().Name}:log-start:{data}");
            Entries.Enqueue(data);
            Receive?.Invoke(this, data);
            Events.Enqueue($"{GetType().Name}:log-end:{data}");
        }
        public void Complete() {
            Events.Enqueue($"{GetType().Name}:complete");
            Interlocked.Increment(ref CompleteCount);
            Completing?.Invoke(this);
        }
    }

    public sealed class ReentrantConfiguredReplacement : ReentrantConfiguredService {
        public string ReplacementOnly { get; set; }
    }

    private static LogServiceProxy ReentrantConfiguredProxy() {
        var config = Logging.Config;
        return ((LogManager)config.GetType().GetProperty("Log").GetValue(config, null))["reentrant"];
    }

    private static void ResetReentrantConfiguration() {
        ReentrantConfiguredService.Receive = null;
        ReentrantConfiguredService.Completing = null;
        while (ReentrantConfiguredService.Instances.TryDequeue(out _)) { }
        while (ReentrantConfiguredService.Events.TryDequeue(out _)) { }
    }

    private static void ConfigureReentrantService(Type type, string option, string replacementOnly = null, object config = null) {
        CONF.Contain($@"
            log[reentrant].type = {type.AssemblyQualifiedName}
            log[reentrant].service.option = {option}
            log[reentrant].service.replacementonly = {replacementOnly}
            log[reentrant].config.default.severity = info
        ").Configure(config ?? Logging.Config, key: "");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReentrantConfigurationTargetsReplacementAndDefersDelivery(bool throwFromOldCompletion) {
        ResetReentrantConfiguration();
        ConfigureReentrantService(typeof(ReentrantConfiguredService), "original");
        var proxy = ReentrantConfiguredProxy();
        var old = (ReentrantConfiguredService)proxy.Service;
        using var callbackConfigured = new ManualResetEventSlim();
        ReentrantConfiguredReplacement configured = null;
        string optionDuringCallback = null;
        var completedDuringCallback = -1;
        ReentrantConfiguredService.Receive = (service, data) => {
            if (ReferenceEquals(service, old) && data == "replace") {
                ConfigureReentrantService(typeof(ReentrantConfiguredReplacement), "configured", "replacement property");
                configured = proxy.Service as ReentrantConfiguredReplacement;
                optionDuringCallback = old.Option;
                completedDuringCallback = old.CompleteCount;
                // Nested delivery must remain on the active service until the outer callback returns.
                proxy.Log(new LogEntry(typeof(LoggingTest), DateTime.UtcNow, LogSeverity.Info, ["nested"]));
                callbackConfigured.Set();
            }
        };
        ReentrantConfiguredService.Completing = service => {
            if (ReferenceEquals(service, old) && throwFromOldCompletion) {
                throw new InvalidOperationException("Expected old completion failure.");
            }
        };
        try {
            Log.Info("replace");
            Assert.That(callbackConfigured.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Log.Info("next entry");
            Logging.Complete();
            var replacement = (ReentrantConfiguredReplacement)proxy.Service;
            var events = ReentrantConfiguredService.Events.ToArray();
            Assert.Multiple(() => {
                Assert.That(configured, Is.SameAs(replacement), "Configuration and later delivery must use the same replacement instance.");
                Assert.That(proxy.Type, Is.EqualTo(typeof(ReentrantConfiguredReplacement).AssemblyQualifiedName));
                Assert.That(optionDuringCallback, Is.EqualTo("original"));
                Assert.That(completedDuringCallback, Is.Zero);
                Assert.That(replacement.Option, Is.EqualTo("configured"));
                Assert.That(replacement.ReplacementOnly, Is.EqualTo("replacement property"));
                Assert.That(old.Entries.ToArray(), Is.EqualTo(["replace", "nested"]));
                Assert.That(replacement.Entries.ToArray(), Is.EqualTo(["next entry"]));
                Assert.That(old.CompleteCount, Is.EqualTo(1));
                Assert.That(replacement.CompleteCount, Is.EqualTo(1));
                Assert.That(ReentrantConfiguredService.Instances.Count, Is.EqualTo(2));
                Assert.That(Array.IndexOf(events, "ReentrantConfiguredService:complete"),
                    Is.GreaterThan(Array.IndexOf(events, "ReentrantConfiguredService:log-end:replace")));
            });
        }
        finally {
            ReentrantConfiguredService.Receive = null;
            ReentrantConfiguredService.Completing = null;
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReentrantTypeChangesRetireUnusedReplacementAndKeepLatestSettings(bool cancelReplacement) {
        ResetReentrantConfiguration();
        ConfigureReentrantService(typeof(ReentrantConfiguredService), "original");
        var proxy = ReentrantConfiguredProxy();
        var old = (ReentrantConfiguredService)proxy.Service;
        using var callbackConfigured = new ManualResetEventSlim();
        ReentrantConfiguredService unused = null;
        ReentrantConfiguredService configured = null;
        ReentrantConfiguredService.Receive = (service, data) => {
            if (ReferenceEquals(service, old) && data == "replace") {
                ConfigureReentrantService(typeof(ReentrantConfiguredReplacement), "discarded", "unused property");
                unused = proxy.Service as ReentrantConfiguredReplacement;
                ConfigureReentrantService(cancelReplacement ? typeof(ReentrantConfiguredService) : typeof(ReentrantConfiguredReplacement),
                    "latest", "latest property");
                if (!cancelReplacement) {
                    // Force a distinct pending type before choosing the final replacement again.
                    ConfigureReentrantService(typeof(ReentrantConfiguredService), "intermediate");
                    ConfigureReentrantService(typeof(ReentrantConfiguredReplacement), "latest", "latest property");
                }
                configured = (ReentrantConfiguredService)proxy.Service;
                callbackConfigured.Set();
            }
        };
        try {
            Log.Info("replace");
            Assert.That(callbackConfigured.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Log.Info("next entry");
            Logging.Complete();
            var active = (ReentrantConfiguredService)proxy.Service;
            Assert.Multiple(() => {
                Assert.That(unused, Is.Not.Null);
                Assert.That(configured, Is.SameAs(active));
                Assert.That(active.Option, Is.EqualTo("latest"));
                Assert.That(active.GetType(), Is.EqualTo(cancelReplacement ? typeof(ReentrantConfiguredService) : typeof(ReentrantConfiguredReplacement)));
                Assert.That(ReferenceEquals(active, old), Is.EqualTo(cancelReplacement));
                Assert.That(old.CompleteCount, Is.EqualTo(1));
                Assert.That(active.CompleteCount, Is.EqualTo(1));
                Assert.That(ReentrantConfiguredService.Instances.All(instance => instance.CompleteCount == 1), Is.True);
                Assert.That(unused?.Entries.ToArray() ?? [], Is.Empty);
                Assert.That(active.Entries.ToArray(), Is.EqualTo(cancelReplacement ? new[] { "replace", "next entry" } : new[] { "next entry" }));
            });
        }
        finally {
            ReentrantConfiguredService.Receive = null;
            ReentrantConfiguredService.Completing = null;
        }
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ShutdownCompletesReplacementConfiguredByOldCompletionCallback(bool throwFromOldCompletion, bool throwFromNewCompletion) {
        ResetReentrantConfiguration();
        ConfigureReentrantService(typeof(ReentrantConfiguredService), "original");
        var config = Logging.Config;
        var proxy = ReentrantConfiguredProxy();
        var old = (ReentrantConfiguredService)proxy.Service;
        ReentrantConfiguredReplacement configured = null;
        ReentrantConfiguredService.Completing = service => {
            if (ReferenceEquals(service, old)) {
                ConfigureReentrantService(typeof(ReentrantConfiguredReplacement), "configured", "replacement property", config);
                configured = proxy.Service as ReentrantConfiguredReplacement;
                if (throwFromOldCompletion) {
                    throw new InvalidOperationException("Expected old shutdown failure.");
                }
            }
            else if (throwFromNewCompletion) {
                throw new InvalidOperationException("Expected replacement shutdown failure.");
            }
        };
        try {
            Exception error = null;
            try { Logging.Complete(); }
            catch (Exception ex) { error = ex; }
            Assert.Multiple(() => {
                Assert.That(error?.ToString(), throwFromOldCompletion ? Does.Contain("Expected old shutdown failure.") : Is.Null);
                Assert.That((error as AggregateException)?.Flatten().InnerExceptions.Count ?? 0,
                    Is.EqualTo((throwFromOldCompletion ? 1 : 0) + (throwFromNewCompletion ? 1 : 0)));
                Assert.That(configured, Is.Not.Null);
                Assert.That(configured?.Option, Is.EqualTo("configured"));
                Assert.That(configured?.ReplacementOnly, Is.EqualTo("replacement property"));
                Assert.That(configured?.CompleteCount, Is.EqualTo(1), "A replacement created during shutdown must also be completed.");
                Assert.That(old.CompleteCount, Is.EqualTo(1));
                Assert.That(ReentrantConfiguredService.Instances.All(instance => instance.CompleteCount == 1), Is.True);
            });
        }
        finally {
            ReentrantConfiguredService.Completing = null;
            proxy.Complete();
        }
    }
}
