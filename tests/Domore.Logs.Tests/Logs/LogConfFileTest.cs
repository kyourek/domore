using Domore.Conf;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Domore.Logs;

[TestFixture]
[NonParallelizable]
public sealed partial class LogConfFileTest {
    private static readonly FieldInfo ConfigFile = typeof(Log.Conf).GetField("File", BindingFlags.Static | BindingFlags.NonPublic);
    private string DirectoryPath;
    private string FilePath;

    private static ConfFile ActiveFile() {
        var file = ConfigFile.GetValue(null);
        return (ConfFile)file.GetType().GetField("Agent", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(file);
    }

    [SetUp]
    public void SetUp() {
        Assert.That(Log.Conf.Configured, Is.False, "Each test needs an unconfigured static watcher.");
        DirectoryPath = Path.Combine(Path.GetTempPath(), "domore.logs.conftest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
        FilePath = Path.Combine(DirectoryPath, "logging.conf");
        RecordingService.Reset();
    }

    [TearDown]
    public void TearDown() {
        try {
            (ConfigFile.GetValue(null) as IDisposable)?.Dispose();
        }
        finally {
            ConfigFile.SetValue(null, null);
            Logging.Complete();
            if (Directory.Exists(DirectoryPath)) {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }

    [Test]
    public void FailedWatcherSetupLeavesConfigurationRetryable() {
        var invalidPath = Path.Combine(DirectoryPath, "missing", "logging.conf");
        Assert.Throws<ArgumentException>(() => Log.Conf.Configure(invalidPath));
        var configuredAfterFailure = Log.Conf.Configured;
        File.WriteAllText(FilePath, "log.logeventthreshold = warn");

        var retried = Log.Conf.Configure(FilePath);

        Assert.Multiple(() => {
            Assert.That(configuredAfterFailure, Is.False);
            Assert.That(retried, Is.True);
            Assert.That(Log.Conf.Configured, Is.True);
            Assert.That(Logging.EventThreshold, Is.EqualTo(LogSeverity.Warn));
            Assert.That(Log.Conf.Configure(FilePath), Is.False);
        });
    }

    public sealed class RecordingService : ILogService {
        public static ConcurrentQueue<string> Entries { get; } = new();
        public static int CompleteCount;

        public static void Reset() {
            while (Entries.TryDequeue(out _)) { }
            CompleteCount = 0;
        }

        public void Log(string name, string data, LogSeverity severity) => Entries.Enqueue(data);
        public void Complete() => Interlocked.Increment(ref CompleteCount);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ConfigurationReloadTargetsCurrentManagerAfterRestart(bool watcherReload) {
        string configuration(string severity) => $@"
            log[recording].type = {typeof(RecordingService).AssemblyQualifiedName}
            log[recording].config.default.severity = {severity}
        ";
        File.WriteAllText(FilePath, configuration("warn"));
        Assert.That(Log.Conf.Configure(FilePath), Is.True);
        var file = ActiveFile();
        if (watcherReload == false) {
            file.Configure(watch: false);
        }
        var logger = Logging.For(typeof(LogConfFileTest));
        Assert.That(logger.Debug(), Is.False);
        logger.Warn("before restart");
        Logging.Complete();
        Assert.That(logger.Debug(), Is.False, "The next manager starts unconfigured.");
        using var reloaded = new ManualResetEventSlim();
        var errors = new ConcurrentQueue<Exception>();
        EventHandler configured = (_, __) => reloaded.Set();
        ErrorEventHandler failed = (_, e) => errors.Enqueue(e.GetException());
        file.Configured += configured;
        file.ConfigureError += failed;
        try {
            File.WriteAllText(FilePath, configuration("debug"));
            if (watcherReload) {
                Assert.That(reloaded.Wait(TimeSpan.FromSeconds(10)), Is.True,
                    "The active filesystem watcher should apply the changed file after restart.");
            }
            else {
                file.Configure();
            }
            Assert.That(errors, Is.Empty);
            Assert.That(logger.Debug(), Is.True, "Reload must configure the current manager.");
            Assert.That(Log.Conf.Configure(FilePath), Is.False, "The existing watcher remains configured.");
            logger.Debug("after restart");
            Logging.Complete();

            Assert.Multiple(() => {
                Assert.That(RecordingService.Entries.ToArray(), Is.EqualTo(["before restart", "after restart"]));
                Assert.That(RecordingService.CompleteCount, Is.EqualTo(2));
            });
        }
        finally {
            file.Configured -= configured;
            file.ConfigureError -= failed;
        }
    }
}
