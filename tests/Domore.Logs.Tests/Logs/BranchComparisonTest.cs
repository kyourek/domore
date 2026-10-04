using Domore.IO;
using Domore.Logs.Service;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Domore.Logs;

[TestFixture]
[NonParallelizable]
public sealed class BranchComparisonTest {
    private static LogManager Manager() =>
        (LogManager)Logging.Config.GetType().GetProperty("Log").GetValue(Logging.Config, null);

    private static string NewDirectory() {
        var path = Path.Combine(Path.GetTempPath(), "domore-branch-probes", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    [SetUp]
    public void Setup() {
        Logging.Complete();
        GatedService.Reset();
    }

    [TearDown]
    public void Cleanup() {
        GatedService.Release.Set();
        Logging.Complete();
    }

    [Test]
    public void NullTypeLogDoesNotThrow() {
        Assert.DoesNotThrow(() => Logging.For(null).Info("message"));
    }

    [Test]
    public void NestedEventLoggingDoesNotRecurse() {
        var calls = 0;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => {
            if (++calls < 6) Logging.For(typeof(BranchComparisonTest)).Info("nested");
        };
        Logging.For(typeof(BranchComparisonTest)).Info("outer");
        Assert.That(calls, Is.EqualTo(1));
    }

    private sealed class Message {
        public int Calls { get; private set; }
        public override string ToString() {
            Calls++;
            return "message";
        }
    }

    [Test]
    public void DisabledLoggingDoesNotFormat() {
        var message = new Message();
        Logging.For(typeof(BranchComparisonTest)).Info(message);
        Assert.That(message.Calls, Is.Zero);
    }

    [Test]
    public void FormatterFailurePreservesSurroundingArguments() {
        var formatter = new LogFormatter();
        formatter.Format(typeof(Message), _ => throw new FormatException("expected"));
        var lines = formatter.Format("before", new Message(), "after");
        Assert.That(lines.First(), Is.EqualTo("before"));
        Assert.That(lines.Last(), Is.EqualTo("after"));
    }

    [Test]
    public void NullFormatterResultFallsBack() {
        var formatter = new LogFormatter();
        formatter.Format(typeof(Message), _ => null);
        Assert.That(formatter.Format(new Message()), Is.EqualTo(new[] { "message" }));
    }

    [Test]
    public void UndefinedSeverityStillFormats() {
        var entry = new LogEntry(typeof(BranchComparisonTest), DateTime.UtcNow, (LogSeverity)7, new[] { "message" });
        Assert.DoesNotThrow(() => entry.LogData("{sev}"));
    }

    [Test]
    public void TimestampUsesGregorianCalendar() {
        var previous = Thread.CurrentThread.CurrentCulture;
        try {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
            var entry = new LogEntry(typeof(BranchComparisonTest),
                new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc), LogSeverity.Info, new[] { "message" });
            Assert.That(entry.LogData("{dat}"), Is.EqualTo("2026-10-04 message"));
        }
        finally {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Test]
    public void ExistingShortTypeThresholdIsHonored() {
        var proxy = Manager()["legacy"];
        proxy.Config.Default.Threshold = LogSeverity.Error;
        proxy.Config[typeof(BranchComparisonTest).Name].Threshold = LogSeverity.Debug;
        Assert.That(Logging.For(typeof(BranchComparisonTest)).Debug(), Is.True);
    }

    [Test]
    public void ExplicitNoneThresholdIsNotEnabled() {
        var proxy = Manager()["disabled"];
        proxy.Config.Default.Threshold = LogSeverity.Info;
        proxy.Config[typeof(BranchComparisonTest).Name].Threshold = LogSeverity.None;
        proxy.Config[typeof(BranchComparisonTest).FullName].Threshold = LogSeverity.None;
        Assert.That(Logging.For(typeof(BranchComparisonTest)).Info(), Is.False);
    }

    [Test]
    public void FileNameWithSubdirectoryCreatesParent() {
        var dir = NewDirectory();
        ILogService service = new FileLog { Directory = dir, Name = "nested/output.log" };
        service.Log("probe", "message", LogSeverity.Info);
        service.Complete();
        Assert.That(File.Exists(Path.Combine(dir, "nested", "output.log")), Is.True);
    }

    [Test]
    public void FinalFileFlushAppliesRotation() {
        var dir = NewDirectory();
        ILogService service = new FileLog { Directory = dir, Name = "output.log", FileSizeLimit = 1 };
        service.Log("probe", "message", LogSeverity.Info);
        service.Complete();
        Assert.That(Directory.GetFiles(dir, "output_*.log"), Has.Length.EqualTo(1));
    }

    [Test]
    public void RootRelativePathStaysRooted() {
        var path = @"\logs\output.log";
        Assert.That(new PathFormatter().Format(path), Is.EqualTo(path));
    }

    [Test]
    public void RepeatedPathTokenIsFullyExpanded() {
        var value = Thread.CurrentThread.ManagedThreadId.ToString();
        Assert.That(new PathFormatter().Format("{Thread.ManagedThreadId}-{Thread.ManagedThreadId}.log"),
            Is.EqualTo(value + "-" + value + ".log"));
    }

    [Test]
    public void CompletionPreservesAlreadyStartedEntry() {
        var proxy = Manager()["gated"];
        proxy.Type = typeof(GatedService).AssemblyQualifiedName;
        proxy.Config.Default.Threshold = LogSeverity.Info;
        _ = proxy.Service;
        using var eventEntered = new ManualResetEventSlim();
        using var eventRelease = new ManualResetEventSlim();
        using var completionDone = new ManualResetEventSlim();
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => {
            eventEntered.Set();
            eventRelease.Wait(TimeSpan.FromSeconds(5));
        };
        var logging = new Thread(() => Logging.For(typeof(BranchComparisonTest)).Info("already started")) {
            IsBackground = true
        };
        var completing = new Thread(() => {
            try { Logging.Complete(); }
            finally { completionDone.Set(); }
        }) { IsBackground = true };
        logging.Start();
        Assert.That(eventEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);
        completing.Start();
        completionDone.Wait(TimeSpan.FromMilliseconds(300));
        eventRelease.Set();
        Assert.That(logging.Join(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(completing.Join(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(GatedService.Messages, Is.EqualTo(1));
    }

    [Test]
    public void CompletionDoesNotOverlapInflightServiceCall() {
        var proxy = Manager()["gated"];
        proxy.Type = typeof(GatedService).AssemblyQualifiedName;
        proxy.Config.Default.Threshold = LogSeverity.Info;
        GatedService.Block = true;
        Logging.For(typeof(BranchComparisonTest)).Info("blocked");
        Assert.That(GatedService.Entered.Wait(TimeSpan.FromSeconds(2)), Is.True);
        var timeoutComplete = typeof(Logging).GetMethod("Complete", new[] { typeof(TimeSpan) });
        var completing = new Thread(() => {
            if (timeoutComplete == null) Logging.Complete();
            else timeoutComplete.Invoke(null, new object[] { TimeSpan.FromMilliseconds(50) });
        }) { IsBackground = true };
        completing.Start();
        GatedService.Completed.Wait(TimeSpan.FromMilliseconds(300));
        GatedService.Release.Set();
        Assert.That(completing.Join(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(GatedService.CompletedWhileLogging, Is.False);
    }

    [Test]
    public void FullQueueDoesNotLoseReplacementCleanup() {
        var proxy = Manager()["gated"];
        proxy.Type = typeof(GatedService).AssemblyQualifiedName;
        proxy.Config.Default.Threshold = LogSeverity.Info;
        GatedService.Block = true;
        Logging.For(typeof(BranchComparisonTest)).Info("blocked");
        Assert.That(GatedService.Entered.Wait(TimeSpan.FromSeconds(2)), Is.True);
        for (var i = 0; i < 1100; i++) Logging.For(typeof(BranchComparisonTest)).Info("queued");
        using var replacementDone = new ManualResetEventSlim();
        var replacing = new Thread(() => {
            try { proxy.Type = "debug"; }
            finally { replacementDone.Set(); }
        }) { IsBackground = true };
        replacing.Start();
        replacementDone.Wait(TimeSpan.FromMilliseconds(300));
        GatedService.Release.Set();
        Assert.That(replacing.Join(TimeSpan.FromSeconds(5)), Is.True);
        Logging.Complete();
        Assert.That(GatedService.CompleteCount, Is.EqualTo(1));
    }

    [Test]
    public void NullParamsArrayProducesEmptyEntry() {
        var received = 0;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => received++;
        Logging.For(typeof(BranchComparisonTest)).Info(null);
        Assert.That(received, Is.EqualTo(1));
    }

    [Test]
    public void EnumerableExpansionIsBounded() {
        var formatter = new LogFormatter();
        var lines = formatter.Format((object)Enumerable.Range(0, 150).ToArray());
        Assert.That(lines.Length, Is.LessThanOrEqualTo(101));
    }

    [Test]
    public void ServicePropertiesBeforeTypeAreApplied() {
        var text = "log[ordered].service.label = retained" + Environment.NewLine +
            "log[ordered].type = " + typeof(GatedService).AssemblyQualifiedName;
        var conf = Domore.Conf.Conf.Contain(text);
        Domore.Conf.Logs.LogConfContainer.ConfigureLogging(conf);
        Assert.That(((GatedService)Manager()["ordered"].Service).Label, Is.EqualTo("retained"));
    }

    [Test]
    public void NewConfigurationSurvivesOldManagerShutdown() {
        var previousFile = typeof(Log.Conf).GetField("File",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        (previousFile.GetValue(null) as IDisposable)?.Dispose();
        previousFile.SetValue(null, null);
        var oldManager = Manager();
        var proxy = oldManager["gated"];
        proxy.Type = typeof(GatedService).AssemblyQualifiedName;
        proxy.Config.Default.Threshold = LogSeverity.Info;
        GatedService.Block = true;
        Logging.For(typeof(BranchComparisonTest)).Info("blocked");
        Assert.That(GatedService.Entered.Wait(TimeSpan.FromSeconds(2)), Is.True);
        var completing = new Thread(Logging.Complete) { IsBackground = true };
        completing.Start();
        try {
            Assert.That(SpinWait.SpinUntil(() => !ReferenceEquals(Manager(), oldManager),
                TimeSpan.FromSeconds(2)), Is.True);
            var path = Path.Combine(NewDirectory(), "logging.conf");
            File.WriteAllText(path, "log.logeventthreshold = warn");
            Assert.That(Log.Conf.Configure(path), Is.True);
            GatedService.Release.Set();
            Assert.That(completing.Join(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(Log.Conf.Configured, Is.True,
                "Completion of the old session must not dispose the new session's watcher.");
        }
        finally {
            GatedService.Release.Set();
            completing.Join(TimeSpan.FromSeconds(5));
            (previousFile.GetValue(null) as IDisposable)?.Dispose();
            previousFile.SetValue(null, null);
        }
    }
    [Test]
    public void AcceptedQueueItemIsDrainedByConcurrentFirstCompletion() {
        var queue = new Domore.Threading.BackgroundQueue();
        var threadLocker = typeof(Domore.Threading.BackgroundQueue).GetField("ThreadLocker",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(queue);
        var collection = (System.Collections.Concurrent.BlockingCollection<Action>)
            typeof(Domore.Threading.BackgroundQueue).GetField("Collection",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(queue);
        var executed = 0;
        using var started = new ManualResetEventSlim();
        var adding = new Thread(() => {
            started.Set();
            queue.Add(() => Interlocked.Increment(ref executed));
        }) { IsBackground = true };
        var accepted = 0;
        lock (threadLocker) {
            adding.Start();
            Assert.That(started.Wait(TimeSpan.FromSeconds(2)), Is.True);
            SpinWait.SpinUntil(() => collection.Count > 0, TimeSpan.FromMilliseconds(100));
            accepted = collection.Count;
            queue.Complete(TimeSpan.FromMilliseconds(100));
            queue.Dispose();
        }
        Assert.That(adding.Join(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(executed, Is.EqualTo(accepted),
            "Complete must drain an item already accepted before the worker starts.");
    }
    [Test]
    public void FileCreatedAfterCachedMissKeepsExistingContent() {
        var dir = NewDirectory();
        var writer = new FileLog { Directory = dir, Name = "append.log", FlushInterval = TimeSpan.FromHours(1) };
        var fileInfo = (FileInfo)typeof(FileLog).GetProperty("FileInfo",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(writer, null);
        Assert.That(fileInfo.Exists, Is.False);
        File.WriteAllLines(fileInfo.FullName, new[] { "existing data" });
        ILogService service = writer;
        service.Log("probe", "logger data", LogSeverity.Info);
        service.Complete();
        Assert.That(File.ReadAllLines(fileInfo.FullName), Is.EqualTo(new[] { "existing data", "logger data" }));
    }

    [Test]
    public void AbsoluteFileNameRotatesInActualParentAndPreservesOtherDirectories() {
        var configuredDirectory = NewDirectory();
        var actualDirectory = NewDirectory();
        var unrelatedFile = Path.Combine(configuredDirectory, "app_20200101-000000-000.log");
        File.WriteAllText(unrelatedFile, "unrelated file");
        var writer = new FileLog {
            Directory = configuredDirectory,
            Name = Path.Combine(actualDirectory, "app.log"),
            FileSizeLimit = 1,
            FileAgeLimit = TimeSpan.FromDays(1),
            FlushInterval = TimeSpan.FromHours(1)
        };
        ILogService service = writer;
        try {
            service.Log("probe", "message", LogSeverity.Info);
            typeof(FileLog).GetMethod("TimerCallback",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(writer, new object[] { null });
        }
        finally {
            service.Complete();
        }
        Assert.Multiple(() => {
            Assert.That(File.Exists(unrelatedFile), Is.True);
            Assert.That(Directory.GetFiles(actualDirectory, "app_*.log"), Has.Length.EqualTo(1));
        });
    }
    private sealed class GatedService : ILogService {
        public string Label { get; set; }
        public static bool Block { get; set; }
        public static bool InLog { get; private set; }
        public static bool CompletedWhileLogging { get; private set; }
        public static int Messages { get; private set; }
        public static int CompleteCount { get; private set; }
        public static ManualResetEventSlim Entered { get; } = new();
        public static ManualResetEventSlim Release { get; } = new();
        public static ManualResetEventSlim Completed { get; } = new();

        public static void Reset() {
            Block = false;
            InLog = false;
            CompletedWhileLogging = false;
            Messages = 0;
            CompleteCount = 0;
            Entered.Reset();
            Release.Reset();
            Completed.Reset();
        }

        public void Log(string name, string data, LogSeverity severity) {
            InLog = true;
            Entered.Set();
            if (Block) Release.Wait(TimeSpan.FromSeconds(5));
            Messages++;
            InLog = false;
        }

        public void Complete() {
            CompleteCount++;
            CompletedWhileLogging |= InLog;
            Completed.Set();
        }
    }
}