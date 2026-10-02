using Domore.Conf.Logs;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs;

[TestFixture]
public sealed partial class LoggingTest {
    private ILog Log {
        get => _Log ??= Logging.For(typeof(LoggingTest));
        set => _Log = value;
    }
    private ILog _Log;

    private bool SkipLoggingCompleteOnTearDown;

    private string Config {
        get => _Config;
        set => CONF.Contain(_Config = value).Configure(Logging.Config, key: "");
    }
    private string _Config;

    [SetUp]
    public void SetUp() {
        Log = null;
        SkipLoggingCompleteOnTearDown = false;
    }

    [TearDown]
    public void TearDown() {
        if (SkipLoggingCompleteOnTearDown != true) {
            Logging.Complete();
        }
    }

    [Test]
    public void LogEventIsRaised2() {
        var message = "";
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, e) => {
            message = e.LogList.Single();
        };
        var log = Logging.For(typeof(LoggingTest));
        log.Info("Here's the log");
        Assert.That(message, Is.EqualTo("Here's the log"));
    }

    [Test]
    public void LogEventIsNotRaisedIfThresholdIsNotMet() {
        var message = "";
        Logging.EventThreshold = LogSeverity.Warn;
        Logging.Event += (_, e) => {
            message = e.LogList.Single();
        };
        var log = Logging.For(typeof(LoggingTest));
        log.Info("Here's the log");
        Assert.That(message, Is.EqualTo(""));
    }

    [Test]
    public void LogEventIsRaisedWithManyMessages2() {
        var message = new List<string>();
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, e) => {
            message.AddRange(e.LogList);
        };
        var log = Logging.For(typeof(LoggingTest));
        log.Info("log1", "log2", "log3");
        Assert.That(message, Is.EqualTo(["log1", "log2", "log3"]));
    }

    [Test]
    public void DefaultLogEventThresholdIsNone() {
        Assert.That(Logging.EventThreshold, Is.EqualTo(LogSeverity.None));
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void EnabledIsFalseIfNothingIsConfigured(LogSeverity severity) {
        Assert.That(Log.Enabled(severity), Is.False);
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void SeverityEnabledIsFalseIfNothingIsConfigured(LogSeverity severity) {
        Assert.That(Log.GetType().GetMethod($"{severity}", Type.EmptyTypes).Invoke(Log, null), Is.False);
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void EnabledIsTrueIfEventIsSubscribedTo2(LogSeverity severity) {
        Logging.Event += (_, __) => { };
        Logging.EventThreshold = severity;
        Assert.That(Log.Enabled(severity), Is.True);
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void SeverityEnabledIsTrueIfEventIsSubscribedTo2(LogSeverity severity) {
        Logging.Event += (_, __) => { };
        Logging.EventThreshold = severity;
        Assert.That(Log.GetType().GetMethod($"{severity}", Type.EmptyTypes).Invoke(Log, null), Is.True);
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void SeverityEnabledIsFalseIfEventIsSubscribedToButThresholdNotMet2(LogSeverity severity) {
        Logging.Event += (_, __) => { };
        Logging.EventThreshold = severity == LogSeverity.Critical ? LogSeverity.None : (severity + 1);
        Assert.That(Log.GetType().GetMethod($"{severity}", Type.EmptyTypes).Invoke(Log, null), Is.False);
    }

    private sealed class TestLogService : ILogService {
        private readonly List<Item> List;

        public ReadOnlyCollection<Item> Items { get; }

        public static TestLogService Instance { get; private set; }

        public TestLogService() {
            List = new List<Item>();
            Items = new ReadOnlyCollection<Item>(List);
            Instance = this;
        }

        void ILogService.Complete() {
        }

        void ILogService.Log(string name, string data, LogSeverity severity) {
            List.Add(new Item(name, data, severity));
        }

        public sealed class Item {
            public string Name { get; }
            public string Data { get; }
            public LogSeverity Severity { get; }

            public Item(string name, string data, LogSeverity severity) {
                Name = name;
                Data = data;
                Severity = severity;
            }
        }
    }

    private void ConfigTest(string config = null) {
        Config = $@"
                Log[t].type = {typeof(TestLogService).AssemblyQualifiedName}
                log[t].config.default.severity = info
                log[t].config.default.format = {{log}} [{{sev}}]
                {config}
            ";
    }

    private sealed class CompletingLogService : ILogService {
        public static ManualResetEventSlim CallbackReturned { get; } = new();
        public static ManualResetEventSlim ServiceCompleted { get; } = new();

        public CompletingLogService() {
        }

        public static void Reset() {
            CallbackReturned.Reset();
            ServiceCompleted.Reset();
        }

        public void Log(string name, string data, LogSeverity severity) {
            Logging.Complete();
            CallbackReturned.Set();
        }

        public void Complete() {
            ServiceCompleted.Set();
        }
    }

    [Test]
    public void CompleteCanBeCalledFromLogServiceCallback() {
        CompletingLogService.Reset();
        Config = $@"
                log[complete].type = {typeof(CompletingLogService).AssemblyQualifiedName}
                log[complete].config.default.severity = info
            ";

        Log.Info("complete from callback");

        var callbackReturned = CompletingLogService.CallbackReturned.Wait(TimeSpan.FromSeconds(2));
        if (callbackReturned == false) {
            SkipLoggingCompleteOnTearDown = true;
        }
        Assert.That(callbackReturned, Is.True, "Logging.Complete should return without joining its own worker thread.");
        Assert.That(CompletingLogService.ServiceCompleted.Wait(TimeSpan.FromSeconds(2)), Is.True);
    }

    private sealed class ThrowingCompleteLogService : ILogService {
        public static ConcurrentQueue<string> Entries { get; } = new();
        public static bool FailNextComplete { get; set; }
        public static int CompleteCount { get; private set; }

        public static void Reset() {
            while (Entries.TryDequeue(out _)) {
            }
            FailNextComplete = true;
            CompleteCount = 0;
        }

        public void Log(string name, string data, LogSeverity severity) {
            Entries.Enqueue(data);
        }

        public void Complete() {
            CompleteCount++;
            if (FailNextComplete) {
                FailNextComplete = false;
                throw new InvalidOperationException("Expected test service completion failure.");
            }
        }
    }

    private sealed class HealthyCompleteLogService : ILogService {
        public static ConcurrentQueue<string> Entries { get; } = new();
        public static int CompleteCount { get; private set; }

        public static void Reset() {
            while (Entries.TryDequeue(out _)) {
            }
            CompleteCount = 0;
        }

        public void Log(string name, string data, LogSeverity severity) {
            Entries.Enqueue(data);
        }

        public void Complete() {
            CompleteCount++;
        }
    }

    [Test]
    public void CompletionErrorDoesNotPoisonNextLoggingSession() {
        ThrowingCompleteLogService.Reset();
        HealthyCompleteLogService.Reset();
        Config = $@"
                log[a_throw].type = {typeof(ThrowingCompleteLogService).AssemblyQualifiedName}
                log[a_throw].config.default.severity = info
                log[z_healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
                log[z_healthy].config.default.severity = info
            ";
        Log.Info("before completion");

        var firstCompletionFailed = false;
        try {
            Logging.Complete();
        }
        catch (Exception) {
            firstCompletionFailed = true;
        }

        var healthyCompletionCount = HealthyCompleteLogService.CompleteCount;
        Config = $@"
                log[a_throw].type = {typeof(ThrowingCompleteLogService).AssemblyQualifiedName}
                log[a_throw].config.default.severity = info
                log[z_healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
                log[z_healthy].config.default.severity = info
            ";
        Log.Info("after completion");

        var retryFailed = false;
        try {
            Logging.Complete();
        }
        catch (Exception) {
            retryFailed = true;
            SkipLoggingCompleteOnTearDown = true;
        }

        Assert.Multiple(() => {
            Assert.That(firstCompletionFailed, Is.True);
            Assert.That(healthyCompletionCount, Is.EqualTo(1));
            Assert.That(retryFailed, Is.False);
            Assert.That(ThrowingCompleteLogService.Entries.ToArray(), Is.EqualTo(["before completion", "after completion"]));
            Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["before completion", "after completion"]));
        });
    }

    private sealed class ThresholdProbeLogService : ILogService {
        public static ConcurrentQueue<string> Entries { get; } = new();

        public static void Clear() {
            while (Entries.TryDequeue(out _)) {
            }
        }

        public void Log(string name, string data, LogSeverity severity) {
            Entries.Enqueue(data);
        }

        public void Complete() {
        }
    }

    [Test]
    public void EnabledIncludesOtherServiceDefaultWhenTypeOverrideExists() {
        ThresholdProbeLogService.Clear();
        Config = $@"
                log[strict].type = {typeof(ThresholdProbeLogService).AssemblyQualifiedName}
                log[strict].config[LoggingTest].severity = warn
                log[fallback].type = {typeof(ThresholdProbeLogService).AssemblyQualifiedName}
                log[fallback].config.default.severity = debug
            ";

        var enabled = Log.Info();
        Log.Info("message");
        Logging.Complete();

        using (Assert.EnterMultipleScope()) {
            Assert.That(enabled, Is.True);
            Assert.That(ThresholdProbeLogService.Entries.ToArray(), Is.EqualTo(["message"]));
        }
    }

    [Test]
    public void EnabledRespectsExplicitNoneTypeOverride() {
        ThresholdProbeLogService.Clear();
        Config = $@"
                log[probe].type = {typeof(ThresholdProbeLogService).AssemblyQualifiedName}
                log[probe].config.default.severity = debug
                log[probe].config[LoggingTest].severity = none
            ";

        var enabled = Log.Info();
        Log.Info("suppressed");
        Logging.Complete();

        using (Assert.EnterMultipleScope()) {
            Assert.That(enabled, Is.False);
            Assert.That(ThresholdProbeLogService.Entries, Is.Empty);
        }
    }

    [Test]
    public void CustomLogServiceIsInstantiated() {
        ConfigTest();
        Log.Info("Some message");
        Logging.Complete();
        var actual = TestLogService.Instance.Items.Select(i => i.Data);
        var expected = new[] { "LoggingTest [inf] Some message" };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void CustomLogServiceIsPassedFormattedData() {
        ConfigTest();
        Log.Info("Here", "are some", "messages.");
        Logging.Complete();
        var actual = TestLogService.Instance.Items.Select(i => i.Data).Single();
        var expected = @"LoggingTest [inf]
  Here
  are some
  messages.";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void CustomLogServiceIsPassedFormattedException() {
        ConfigTest();
        var err = default(Exception);
        try {
            throw new Exception("Oops...");
        }
        catch (Exception ex) {
            Log.Error(err = ex);
        }
        Logging.Complete();
        var actual = TestLogService.Instance.Items.Select(i => i.Data).Single();
        var expected =
            "LoggingTest [err]" + Environment.NewLine +
            string.Join(Environment.NewLine, err
                .ToString()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => $"  {line}"));
        Assert.That(actual, Is.EqualTo(expected));
    }

    private sealed class XYPoint {
        public int X { get; set; }
        public int Y { get; set; }
    }

    [Test]
    public void CustomLogServiceIsPassedCustomFormat() {
        ConfigTest();
        Logging.Format(typeof(XYPoint), obj => {
            var p = (XYPoint)obj;
            return new[] {
                $"XY {{",
                $"  x: {p.X}",
                $"  y: {p.Y}",
                $"}}"
            };
        });
        Log.Warn(new XYPoint { X = 1, Y = 2 }, new XYPoint { X = 3, Y = 4 });
        Logging.Complete();
        var actual = TestLogService.Instance.Items.Select(i => i.Data).Single();
        var expected = @"LoggingTest [wrn]
  XY {
    x: 1
    y: 2
  }
  XY {
    x: 3
    y: 4
  }";
        Assert.That(actual, Is.EqualTo(expected));
    }

    private sealed class CustomLogQueue : ILogService {
        public static Queue<string> Queue { get; } = new();

        public void Log(string name, string data, LogSeverity severity) {
            Queue.Enqueue(data);
        }

        public void Complete() {
        }
    }

    [Test]
    public void ExampleLogServiceWorksAsShown() {
        LogConf.ConfigureLogging($@"
                log[queue].type = {typeof(CustomLogQueue).AssemblyQualifiedName}
                log[queue].config.default.severity = info
            ");
        Log.Info("Put me in the queue.");
        Logging.Complete();

        var message = CustomLogQueue.Queue.Dequeue();
        Assert.That(message, Is.EqualTo("Put me in the queue."));
    }
}
