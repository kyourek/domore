using Domore.Logs.Mocks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Domore.Logs.Service;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs.Services;

[TestFixture]
[NonParallelizable]
internal sealed class FileLogTest {
    private string Id {
        get => field ??= Guid.NewGuid().ToString();
        set;
    }

    private string TempDir {
        get => field ??= Path.Combine(Path.GetTempPath(), "domore.logs.loggingtest", Id);
        set;
    }

    public string TempFile {
        get => field ??= new Func<string>(() => {
            if (Directory.Exists(TempDir) == false) {
                Directory.CreateDirectory(TempDir);
            }
            var path = Path.Combine(TempDir, "domore.logs.loggingtest");
            using (File.Create(path)) {
                return path;
            }
        })();
        set;
    }

    private string Config {
        get;
        set => CONF.Contain(field = value).Configure(Logging.Config, key: "");
    }

    private ILog Log {
        get => field ??= Logging.For(typeof(LoggingTest));
        set;
    }

    private void ConfigFile(string config = null) {
        Config = $@"
                Log[f].type = file
                Log[f].service.directory = {TempDir}
                log[f].service.name = {Path.GetFileName(TempFile)}
                log[f].service.flush interval = 00:00:00.1
                log[f].config.default.severity = info
                log[f].config.default.format = {{log}} [{{sev}}]
                {config}
            ";
    }

    private string ReadFile() {
        return File.ReadAllText(TempFile).Trim();
    }

    [SetUp]
    public void SetUp() {
        Id = null;
        TempDir = null;
        TempFile = null;
        Log = null;
        Config = null;
        if (Directory.Exists(TempDir)) {
            Directory.Delete(TempDir, recursive: true);
        }
    }

    [TearDown]
    public void TearDown() {
        Logging.Complete();
        if (File.Exists(TempFile)) {
            File.Delete(TempFile);
        }
        if (Directory.Exists(TempDir)) {
            Directory.Delete(TempDir, recursive: true);
        }
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void LogCountLimitRejectsNonPositiveValues(int limit) {
        var fileLog = new FileLog();

        Assert.Throws<ArgumentOutOfRangeException>(() => fileLog.LogCountLimit = limit);
        Assert.That(fileLog.LogCountLimit, Is.EqualTo(100));
    }

    [Test]
    public void LogCountLimitCanBeChangedWhileFileLogIsRunning() {
        var fileLog = new FileLog {
            Directory = TempDir,
            Name = "test.log",
            FlushInterval = TimeSpan.FromHours(1),
            LogCountLimit = 5
        };
        var service = (ILogService)fileLog;
        service.Log("test", "queued", LogSeverity.Info);

        Assert.That(fileLog.Started, Is.True);
        Assert.DoesNotThrow(() => fileLog.LogCountLimit = 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => fileLog.LogCountLimit = 0);
        Assert.That(fileLog.LogCountLimit, Is.EqualTo(2));

        service.Complete();

        Assert.That(File.ReadAllText(Path.Combine(TempDir, "test.log")).Trim(), Is.EqualTo("queued"));
    }

    [Test]
    public void WideSettingsWaitForTheFileServiceLock() {
        var fileLog = new FileLog();
        var locker = typeof(FileLog).GetField("Locker", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fileLog);

        AssertWideSettingWaitsForLock(locker, () => fileLog.FileSizeLimit = 1234567890123L);
        AssertWideSettingWaitsForLock(locker, () => fileLog.TotalSizeLimit = 2345678901234L);
        AssertWideSettingWaitsForLock(locker, () => fileLog.FileAgeLimit = TimeSpan.FromTicks(3456789012345L));
        AssertWideSettingWaitsForLock(locker, () => _ = fileLog.FileSizeLimit);
        AssertWideSettingWaitsForLock(locker, () => _ = fileLog.TotalSizeLimit);
        AssertWideSettingWaitsForLock(locker, () => _ = fileLog.FileAgeLimit);
    }

    private static void AssertWideSettingWaitsForLock(object locker, Action operation) {
        using var started = new ManualResetEventSlim(false);
        using var completed = new ManualResetEventSlim(false);
        Exception failure = null;
        var worker = new Thread(() => {
            started.Set();
            try {
                operation();
            }
            catch (Exception ex) {
                failure = ex;
            }
            finally {
                completed.Set();
            }
        }) {
            IsBackground = true
        };
        var waited = false;

        WithServiceLock(locker, () => {
            worker.Start();
            Assert.That(started.Wait(TimeSpan.FromSeconds(5)), Is.True);
            waited = completed.Wait(TimeSpan.FromMilliseconds(100)) == false;
        });

        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
        worker.Join();
        Assert.Multiple(() => {
            Assert.That(waited, Is.True);
            Assert.That(failure, Is.Null);
        });
    }

    private static void WithServiceLock(object locker, Action action) {
#if NET9_0_OR_GREATER
        var serviceLock = (Lock)locker;
        lock (serviceLock) {
            action();
        }
#else
        lock (locker) {
            action();
        }
#endif
    }

    [TestCase(-2d)]
    [TestCase(-1d)]
    [TestCase(0d)]
    [TestCase(0.5d)]
    [TestCase(1.5d)]
    [TestCase(2147483648d)]
    public void FlushIntervalRejectsUnusableTimerDelays(double milliseconds) {
        var fileLog = new FileLog();
        var original = fileLog.FlushInterval;
        var interval = TimeSpan.FromTicks((long)(milliseconds * TimeSpan.TicksPerMillisecond));

        Assert.Throws<ArgumentOutOfRangeException>(() => fileLog.FlushInterval = interval);
        Assert.That(fileLog.FlushInterval, Is.EqualTo(original));
    }

    [TestCase(1d)]
    [TestCase(2147483647d)]
    public void FlushIntervalAcceptsTimerRepresentableBounds(double milliseconds) {
        var fileLog = new FileLog();
        var expected = TimeSpan.FromMilliseconds(milliseconds);

        fileLog.FlushInterval = expected;

        Assert.That(fileLog.FlushInterval, Is.EqualTo(expected));
    }

    [Test]
    public void CorrectedFlushIntervalAllowsWriterToStartAndPreservesFirstEntry() {
        var fileLog = new FileLog {
            Directory = TempDir,
            Name = "flush-interval.log"
        };
        var service = (ILogService)fileLog;

        Assert.Throws<ArgumentOutOfRangeException>(() => fileLog.FlushInterval = TimeSpan.FromMilliseconds(-2));
        Assert.That(fileLog.Started, Is.False);
        fileLog.FlushInterval = TimeSpan.FromSeconds(1);
        service.Log("test", "first entry", LogSeverity.Info);

        Assert.That(fileLog.Started, Is.True);
        service.Complete();

        Assert.That(File.ReadAllText(Path.Combine(TempDir, "flush-interval.log")).Trim(), Is.EqualTo("first entry"));
    }

    [Test]
    public void FailedTimerStartupCanRetryWithoutDroppingQueuedEntries() {
        var fileLog = new FileLog {
            Directory = TempDir,
            Name = "retry-flush-interval.log"
        };
        var intervalField = typeof(FileLog).GetField("<FlushInterval>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(intervalField, Is.Not.Null);
        intervalField.SetValue(fileLog, TimeSpan.FromMilliseconds(-2));
        var service = (ILogService)fileLog;

        Assert.Throws<InvalidOperationException>(() => service.Log("test", "first entry", LogSeverity.Info));
        Assert.That(fileLog.Started, Is.False);
        fileLog.FlushInterval = TimeSpan.FromSeconds(1);
        service.Log("test", "retry entry", LogSeverity.Info);
        service.Complete();

        var actual = File.ReadAllLines(Path.Combine(TempDir, "retry-flush-interval.log"));
        Assert.That(actual, Is.EqualTo(["first entry", "retry entry"]));
    }

    [Test]
    public void CompletionRotatesFinalFlushAndAppliesRetention() {
        Directory.CreateDirectory(TempDir);
        var name = "final-flush.log";
        var activePath = Path.Combine(TempDir, name);
        var expiredArchive = Path.Combine(TempDir, "final-flush_20200101-000000-000+0000.log");
        var fileLog = new FileLog {
            Directory = TempDir,
            Name = name,
            FileSizeLimit = 1,
            TotalSizeLimit = 1024,
            FileAgeLimit = TimeSpan.FromDays(1),
            FlushInterval = TimeSpan.FromHours(1)
        };
        var service = (ILogService)fileLog;
        File.WriteAllText(expiredArchive, "expired");
        service.Log("test", "pending final entry", LogSeverity.Info);

        service.Complete();

        var archives = Directory.GetFiles(TempDir, "final-flush_*.log", SearchOption.TopDirectoryOnly);
        Assert.Multiple(() => {
            Assert.That(fileLog.Complete, Is.True);
            Assert.That(File.Exists(activePath), Is.False);
            Assert.That(File.Exists(expiredArchive), Is.False);
            Assert.That(archives, Has.Length.EqualTo(1));
            Assert.That(File.ReadAllText(archives.Single()).Trim(), Is.EqualTo("pending final entry"));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FileNameParentIsCreatedForAbsoluteAndRelativeNames(bool absoluteName) {
        var configuredDirectory = Path.Combine(TempDir, "configured");
        var activeDirectory = absoluteName
            ? Path.Combine(TempDir, "actual", "nested")
            : Path.Combine(configuredDirectory, "nested");
        var activePath = Path.Combine(activeDirectory, "app.log");
        var writer = new FileLog {
            Directory = configuredDirectory,
            Name = absoluteName ? activePath : Path.Combine("nested", "app.log"),
            FlushInterval = TimeSpan.FromHours(1)
        };
        var service = (ILogService)writer;

        service.Log("test", "created in resolved parent", LogSeverity.Info);
        service.Complete();

        Assert.That(File.ReadAllLines(activePath), Is.EqualTo(["created in resolved parent"]));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FileNameParentControlsRotationAndRetention(bool absoluteName) {
        var configuredDirectory = Path.Combine(TempDir, "configured");
        var activeDirectory = absoluteName
            ? Path.Combine(TempDir, "actual", "nested")
            : Path.Combine(configuredDirectory, "nested");
        Directory.CreateDirectory(configuredDirectory);
        Directory.CreateDirectory(activeDirectory);
        var archiveName = "app_20200101-000000-000+0000.log";
        var unrelatedArchive = Path.Combine(configuredDirectory, archiveName);
        var expiredArchive = Path.Combine(activeDirectory, archiveName);
        var oversizedArchive = Path.Combine(activeDirectory,
            "app_" + DateTime.UtcNow.AddHours(-1).ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "+0000.log");
        File.WriteAllText(unrelatedArchive, "another directory's archive");
        File.WriteAllText(expiredArchive, "expired");
        File.WriteAllText(oversizedArchive, new string('x', 2048));
        var activePath = Path.Combine(activeDirectory, "app.log");
        var writer = new FileLog {
            Directory = configuredDirectory,
            Name = absoluteName ? activePath : Path.Combine("nested", "app.log"),
            FileSizeLimit = 1,
            FileAgeLimit = TimeSpan.FromDays(1),
            TotalSizeLimit = 1024,
            FlushInterval = TimeSpan.FromHours(1)
        };
        var service = (ILogService)writer;

        service.Log("test", "resolved parent archive", LogSeverity.Info);
        service.Complete();

        var archives = Directory.GetFiles(activeDirectory, "app_*.log");
        Assert.Multiple(() => {
            Assert.That(File.Exists(activePath), Is.False);
            Assert.That(File.Exists(expiredArchive), Is.False);
            Assert.That(File.Exists(oversizedArchive), Is.False);
            Assert.That(File.ReadAllText(unrelatedArchive), Is.EqualTo("another directory's archive"));
            Assert.That(archives, Has.Length.EqualTo(1));
            Assert.That(File.ReadAllLines(archives.Single()), Is.EqualTo(["resolved parent archive"]));
            Assert.That(Directory.GetFiles(configuredDirectory, "app_*.log"), Is.EqualTo([unrelatedArchive]));
        });
    }

    [Test]
    public void AppendPreservesFileCreatedAfterMissingStateWasCached() {
        var writer = new FileLog {
            Directory = TempDir,
            Name = "append.log",
            FlushInterval = TimeSpan.FromHours(1)
        };
        var property = typeof(FileLog).GetProperty("FileInfo", BindingFlags.Instance | BindingFlags.NonPublic);
        var file = (FileInfo)property.GetValue(writer);
        Assert.That(file.Exists, Is.False);
        Directory.CreateDirectory(TempDir);
        File.WriteAllLines(file.FullName, ["other writer's data"]);
        var service = (ILogService)writer;

        service.Log("test", "logger's data", LogSeverity.Info);
        service.Complete();

        Assert.That(File.ReadAllLines(file.FullName), Is.EqualTo(["other writer's data", "logger's data"]));
    }

    [Test]
    public void LogsData() {
        ConfigFile();
        Log.Info("here's some data");
        Logging.Complete();
        var actual = ReadFile();
        var expected = "LoggingTest [inf] here's some data";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void LogsDataWhileSubscriptionsAreActive() {
        ConfigFile();
        var mock = new MockLogSubscription();
        var entries = new List<ILogEntry>();
        mock.Threshold = _ => LogSeverity.Info;
        mock.Receive = entries.Add;
        Logging.Subscribe(mock);
        Log.Info("here's some data");
        Logging.Complete();
        var actual = ReadFile();
        var expected = "LoggingTest [inf] here's some data";
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void DoesNotLogDataIfSeverityNotMet() {
        ConfigFile("log[f].config[LoggingTest].severity = warn");
        Log.Info("here's some data");
        Logging.Complete();
        var actual = ReadFile();
        Assert.That(actual, Is.Empty);
    }

    [Test]
    public void LogsDataIfSeverityIsChanged() {
        ConfigFile("log[f].config[LoggingTest].severity = error");
        Log.Warn("here's some data");
        Thread.Sleep(100);
        CONF.Contain("log[f].config[LoggingTest].severity = warn").Configure(Logging.Config, key: "");
        Log.Warn("this Should be logged");
        Logging.Complete();
        var actual = ReadFile();
        Assert.That(actual, Is.EqualTo("LoggingTest [wrn] this Should be logged"));
    }

    [Test]
    public void LogsLotsOfData() {
        ConfigFile("log[f].config[LoggingTest].format = {sev}");
        for (var i = 0; i < 100; i++) {
            Log.Info($"{i}");
        }
        Logging.Complete();
        var expected = string.Join(Environment.NewLine, Enumerable.Range(0, 100).Select(i => $"inf {i}"));
        var actual = ReadFile();
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RotatesLogFile() {
        var fileDir = TempDir;
        var fileName = $"domore.logs.loggingtest.{nameof(RotatesLogFile)}";
        var fileSearchPattern = $"domore.logs.loggingtest_*{nameof(RotatesLogFile)}";
        ConfigFile(@$"
                log[f].service.name = {fileName}
                log[f].service.file size limit = 1
                log[f].service.flush interval = 00:00:00.01
                log[f].config.default.format = {{sev}}
            ");
        Log.Critical("Some data that will be in a dated log");
        Thread.Sleep(100);
        Config = "log[f].service.FileSizeLimit = 1000";
        Log.Critical("More data that will be in the original log");
        Logging.Complete();

        var datedLog = Directory.GetFiles(fileDir, fileSearchPattern, SearchOption.TopDirectoryOnly).Single();
        Assert.That("crt Some data that will be in a dated log" + Environment.NewLine, Is.EqualTo(File.ReadAllText(datedLog)));

        var originalLog = Path.Combine(fileDir, fileName);
        Assert.That("crt More data that will be in the original log" + Environment.NewLine, Is.EqualTo(File.ReadAllText(originalLog)));
    }

    [TestCase("the_log")]
    [TestCase("the(log)")]
    public void RotatesLogFilesWithoutExtension(string name) {
        ConfigFile(@$"
                log[f].service.name = {name}
                log[f].service.file size limit = 1
                log[f].service.flush interval = 00:00:00.01
                log[f].config.default.format = {{sev}}
            ");
        Log.Critical("Some data that will be in a dated log");
        Thread.Sleep(100);
        Log.Critical("More data that will be in a dated log");
        Thread.Sleep(100);
        Log.Critical("That's all");
        Thread.Sleep(100);
        Logging.Complete();
        var datedLogs = Directory.GetFiles(TempDir, $"{name}_????????-??????-???*", SearchOption.TopDirectoryOnly);
        Assert.That(datedLogs.Length, Is.EqualTo(3));
    }

    [TestCase("the", "log")]
    [TestCase("the", "(log)")]
    public void RotatesLogFilesWithExtension(string name, string extension) {
        ConfigFile(@$"
                log[f].service.name = {name}.{extension}
                log[f].service.file size limit = 1
                log[f].service.flush interval = 00:00:00.01
                log[f].config.default.format = {{sev}}
            ");
        Log.Critical("Some data that will be in a dated log");
        Thread.Sleep(100);
        Log.Critical("More data that will be in a dated log");
        Thread.Sleep(100);
        Log.Critical("That's all");
        Thread.Sleep(100);
        Logging.Complete();
        var datedLogs = Directory.GetFiles(TempDir, $"{name}_????????-??????-???*.{extension}", SearchOption.TopDirectoryOnly);
        Assert.That(datedLogs.Length, Is.EqualTo(3));
    }

    [TestCase("the_log")]
    [TestCase("the(log)")]
    public void DeletesLogFilesWithoutExtension(string name) {
        ConfigFile(@$"
                log[f].service.name = {name}
                log[f].service.file size limit = 1
                log[f].service.total size limit = 25
                log[f].service.flush interval = 00:00:00.01
                log[f].config.default.format = {{sev}}
            ");
        Log.Critical("Some data that will be in a dated log");
        Thread.Sleep(100);
        Log.Critical("More data that will be in a dated log");
        Thread.Sleep(100);
        Log.Critical("That's all");
        Thread.Sleep(100);
        Logging.Complete();
        var datedLogs = Directory.GetFiles(TempDir, $"{name}_????????-??????-???*", SearchOption.TopDirectoryOnly);
        Assert.That(datedLogs.Length, Is.EqualTo(1));
    }

    [TestCase("the", "log")]
    [TestCase("the", "(log)")]
    public void DeletesLogFilesWithExtension(string name, string extension) {
        ConfigFile(@$"
                log[f].service.name = {name}.{extension}
                log[f].service.file size limit = 1
                log[f].service.total size limit = 25
                log[f].service.flush interval = 00:00:00.01
                log[f].config.default.format = {{sev}}
            ");
        Log.Critical("Some data that will be in a dated log");
        Thread.Sleep(100);
        Log.Critical("More data that will be in a dated log");
        Thread.Sleep(100);
        Log.Critical("That's all");
        Thread.Sleep(100);
        Logging.Complete();
        var datedLogs = Directory.GetFiles(TempDir, $"{name}_????????-??????-???*.{extension}", SearchOption.TopDirectoryOnly);
        Assert.That(datedLogs.Length, Is.EqualTo(1));
    }

    [Test]
    public void RotatesLogsManyTimes() {
        var fileDir = TempDir;
        var fileName = $"domore.logs.loggingtest.{nameof(RotatesLogsManyTimes)}";
        var fileSearchPattern = $"domore.logs.loggingtest_*.{nameof(RotatesLogsManyTimes)}";
        ConfigFile(@$"
                log[f].service.name = {fileName}
                log[f].service.file size limit = 1
                log[f].service.flush interval = 00:00:00.01
            ");
        for (var i = 0; i < 10; i++) {
            Log.Info($"{i}");
            Thread.Sleep(100);
        }
        Logging.Complete();
        var files = Directory.GetFiles(fileDir, fileSearchPattern, SearchOption.TopDirectoryOnly);
        var expected = 10;
        var actual = files.Length;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RespectsTotalSizeLimit() {
        var fileDir = TempDir;
        var fileName = TempFile = Path.Combine(fileDir, $"domore.logs.loggingtest.{nameof(RespectsTotalSizeLimit)}");
        ConfigFile(@$"
                log[f].service.name = {fileName}
                log[f].service.file size limit = 1
                log[f].service.total size limit = 1
                log[f].service.flush interval = 00:00:00.01
            ");
        for (var i = 0; i < 10; i++) {
            Log.Info($"{i}");
            Thread.Sleep(100);
        }
        Logging.Complete();
        var files = Directory.GetFiles(fileDir);
        Assert.That(files.Length, Is.Zero);
    }

    [Test]
    public void ArchiveTimestampsUseLocalTimeAndPreserveOffsets() {
        var fileLog = new FileLog {
            Directory = TempDir,
            Name = "archive.log"
        };
        var fileDateNameMethod = typeof(FileLog).GetMethod("FileDateName", BindingFlags.Instance | BindingFlags.NonPublic);
        var fileDateMethod = typeof(FileLog).GetMethod("FileDate", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(fileDateNameMethod, Is.Not.Null);
        Assert.That(fileDateMethod, Is.Not.Null);

        var before = DateTime.UtcNow;
        var generatedName = (string)fileDateNameMethod.Invoke(fileLog, null);
        var generatedDate = (DateTime?)fileDateMethod.Invoke(fileLog, [generatedName]);
        var legacyDate = (DateTime?)fileDateMethod.Invoke(fileLog, ["archive_20260115-120000-000.log"]);
        var expectedLocalText = generatedDate.Value.ToLocalTime().ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        var offset = TimeZoneInfo.Local.GetUtcOffset(generatedDate.Value);
        var offsetMinutes = (int)offset.TotalMinutes;
        var absoluteOffsetMinutes = Math.Abs(offsetMinutes);
        var expectedOffsetText = $"{(offsetMinutes < 0 ? "-" : "+")}{absoluteOffsetMinutes / 60:00}{absoluteOffsetMinutes % 60:00}.log";

        Assert.Multiple(() => {
            Assert.That(generatedName.Substring("archive_".Length, 19), Is.EqualTo(expectedLocalText));
            Assert.That(generatedName, Does.EndWith(expectedOffsetText));
            Assert.That(generatedDate.HasValue, Is.True);
            Assert.That(generatedDate.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(generatedDate.Value, Is.InRange(before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1)));
            Assert.That(legacyDate.HasValue, Is.True);
            Assert.That(legacyDate.Value.Kind, Is.EqualTo(DateTimeKind.Local));
        });
    }

    [Test]
    public void InvalidArchiveDatesDoNotAbortRetention() {
        Directory.CreateDirectory(TempDir);
        var fileLog = new FileLog {
            Directory = TempDir,
            Name = "archive.log",
            FileSizeLimit = 1,
            FileAgeLimit = TimeSpan.FromDays(1),
            TotalSizeLimit = long.MaxValue
        };
        var expiredArchive = Path.Combine(TempDir, "archive_20200101-000000-000.log");
        var invalidArchives = new[] {
            "archive_20261301-000000-000.log",
            "archive_20260230-000000-000.log",
            "archive_20230229-000000-000.log",
            "archive_20260101-246000-000.log"
        }.Select(name => Path.Combine(TempDir, name)).ToArray();
        File.WriteAllText(Path.Combine(TempDir, "archive.log"), "trigger rotation");
        File.WriteAllText(expiredArchive, "expired");
        foreach (var path in invalidArchives) {
            File.WriteAllText(path, "invalid");
        }

        var fileInfoProperty = typeof(FileLog).GetProperty("FileInfo", BindingFlags.Instance | BindingFlags.NonPublic);
        var fileDateMethod = typeof(FileLog).GetMethod("FileDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var rotateMethod = typeof(FileLog).GetMethod("Rotate", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(fileInfoProperty, Is.Not.Null);
        Assert.That(fileDateMethod, Is.Not.Null);
        Assert.That(rotateMethod, Is.Not.Null);
        fileInfoProperty.GetValue(fileLog);

        Assert.That((DateTime?)fileDateMethod.Invoke(fileLog, ["another_20200101-000000-000.log"]), Is.Null);
        Assert.That((DateTime?)fileDateMethod.Invoke(fileLog, ["archive_20200101-000000-000.txt"]), Is.Null);
        Assert.DoesNotThrow(() => rotateMethod.Invoke(fileLog, null));
        Assert.That(File.Exists(expiredArchive), Is.False);
        Assert.That(invalidArchives.All(File.Exists), Is.True);
    }

    [Test]
    public void IndividualRetentionDeleteFailuresDoNotStopCleanupOrLoseByteAccounting() {
        Directory.CreateDirectory(TempDir);
        var lockedArchive = Path.Combine(TempDir, "archive_20000101-000000-000.log");
        var firstDeletableArchive = Path.Combine(TempDir, "archive_20200101-000000-000.log");
        var secondDeletableArchive = Path.Combine(TempDir, "archive_20210101-000000-000.log");
        File.WriteAllText(Path.Combine(TempDir, "archive.log"), "x");
        File.WriteAllText(lockedArchive, "locked");
        File.WriteAllText(firstDeletableArchive, "four");
        File.WriteAllText(secondDeletableArchive, "sixsix");
        var fileLog = new FileLog {
            Directory = TempDir,
            Name = "archive.log",
            FileSizeLimit = 1,
            FileAgeLimit = TimeSpan.FromDays(50000),
            TotalSizeLimit = 7
        };
        typeof(FileLog).GetProperty("FileInfo", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fileLog);
        var rotateMethod = typeof(FileLog).GetMethod("Rotate", BindingFlags.Instance | BindingFlags.NonPublic);

        using (File.Open(lockedArchive, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
            Assert.DoesNotThrow(() => rotateMethod.Invoke(fileLog, null));
        }

        Assert.Multiple(() => {
            Assert.That(File.Exists(lockedArchive), Is.True);
            Assert.That(File.Exists(firstDeletableArchive), Is.False);
            Assert.That(File.Exists(secondDeletableArchive), Is.False);
        });
    }

    [Test]
    public void RemovesLogsGreaterThanAgeLimit() {
        var fileDir = TempDir;
        var fileName = $"domore.logs.loggingtest.{nameof(RemovesLogsGreaterThanAgeLimit)}";
        var fileSearchPattern = $"domore.logs.loggingtest_*.{nameof(RemovesLogsGreaterThanAgeLimit)}";
        ConfigFile(@$"
                log[f].service.name = {fileName}
                log[f].service.file size limit = 1
                log[f].service.flush interval = 00:00:00.01
                log[f].service.file age limit = 00:00:00
            ");
        for (var i = 0; i < 10; i++) {
            Log.Info($"{i}");
            Thread.Sleep(100);
        }
        Logging.Complete();
        var files = Directory.GetFiles(fileDir, fileSearchPattern, SearchOption.TopDirectoryOnly);
        var expected = 0;
        var actual = files.Length;
        Assert.That(actual, Is.EqualTo(expected));
        Directory
            .GetFiles(fileDir, fileSearchPattern, SearchOption.TopDirectoryOnly)
            .ToList()
            .ForEach(File.Delete);
    }

    [Test]
    public void LogsToSpecialFolderPath() {
        var id = Guid.NewGuid();
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest", id.ToString());
        try {
            ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest/{id}
                    log[f].service.name = test.log
                    LOG[f].config.default.format = {{sev}}
                ");
            Log.Info("Got the message?");
            Logging.Complete();
            var actual = File.ReadAllText(Path.Combine(dir, "test.log")).Trim();
            var expected = "inf Got the message?";
            Assert.That(actual, Is.EqualTo(expected));
        }
        finally {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public void LogsToFormattedPath() {
        var id = Guid.NewGuid();
        var idDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest", id.ToString());
        var dir = Path.Combine(idDir, AppDomain.CurrentDomain?.FriendlyName);
        try {
            ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest/{id}/{{appDomain.friendlyName}}
                    log[f].service.name = test-{{thread.ManagedThreadID}}.log
                    LOG[f].config.default.format = {{sev}}
                ");
            Log.Info("Got the message?");
            Logging.Complete();
            var files = Directory.GetFiles(dir, "test-*.log", SearchOption.TopDirectoryOnly);
            Assert.That(files, Has.Length.EqualTo(1), "The formatted thread token should produce one log file.");
            var fileName = Path.GetFileNameWithoutExtension(files[0]);
            var threadToken = fileName.Substring("test-".Length);
            Assert.That(int.TryParse(threadToken, NumberStyles.None, CultureInfo.InvariantCulture, out _), Is.True,
                "The expanded thread token should remain a numeric managed thread identifier.");
            var actual = File.ReadAllText(files[0]).Trim();
            var expected = "inf Got the message?";
            Assert.That(actual, Is.EqualTo(expected));
        }
        finally {
            try {
                Logging.Complete();
            }
            finally {
                if (Directory.Exists(idDir)) {
                    Directory.Delete(idDir, recursive: true);
                }
            }
        }
    }

    [Test]
    public void RetriesOnIOException() {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest", Id);
        var file = Path.Combine(dir, "test.log");
        try {
            ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest/{Id}
                    log[f].service.name = test.log
                    LOG[f].config.default.format = {{sev}}
                    log[f].service.io retry delay = 250
                ");
            Directory.CreateDirectory(dir);
            using (var stream = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
                Log.Info("Got the message?");
                Thread.Sleep(500);
            }
            Logging.Complete();
            var actual = File.ReadAllText(Path.Combine(dir, "test.log")).Trim();
            var expected = "inf Got the message?";
            Assert.That(actual, Is.EqualTo(expected));
        }
        finally {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public void DoesNotThrowIfInUse() {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest", Id);
        var file = Path.Combine(dir, "test.log");
        try {
            ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest/{Id}
                    log[f].service.name = test.log
                    log[f].service.io retry limit = 0
                ");
            Directory.CreateDirectory(dir);
            using (var stream = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
                Log.Info("Got the message?");
                Thread.Sleep(250);
            }
            Logging.Complete();
            var actual = File.ReadAllText(Path.Combine(dir, "test.log")).Trim();
            var expected = "";
            Assert.That(actual, Is.EqualTo(expected));
        }
        finally {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public void RecreatesFileIfDeleted() {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest", Id);
        var file = Path.Combine(dir, "test.log");
        try {
            ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest/{Id}
                    log[f].service.name = test.log
                    log[f].service.flush interval = 00:00:00.01
                    LOG[f].config.default.format = {{sev}}
                ");
            Log.Warn("Message 1");
            Thread.Sleep(100);
            Directory.Delete(dir, recursive: true);
            Log.Warn("Message 2");
            Logging.Complete();
            var actual = File.ReadAllText(file).Trim();
            var expected = "wrn Message 2";
            Assert.That(actual, Is.EqualTo(expected));
        }
        finally {
            if (Directory.Exists(dir)) {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Test]
    public void SubscribersReceiveEntriesWhileFileLogging() {
        ConfigFile();
        var mock = new MockLogSubscription();
        var entries = new List<ILogEntry>();
        mock.Threshold = _ => LogSeverity.Info;
        mock.Receive = entries.Add;
        Logging.Subscribe(mock);
        Log.Info("here's some data");
        Logging.Complete();
        var actual = entries.SelectMany(e => e.LogList).ToList();
        var expected = new[] { "here's some data" };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatCanContainDateAndTimeInUtc() {
        ConfigFile("log[f].config.default.format = {dat} {tim}");
        var now = DateTime.UtcNow;
        Log.Critical("What time is it?");
        Logging.Complete();
        var text = ReadFile();
        var parts = text.Split([' '], 3);
        var date = DateTime.Parse(parts[0]);
        var time = TimeSpan.Parse(parts[1]);
        var then = date + time;
        Assert.That(then, Is.EqualTo(now).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void FormatCanContainDateAndTimeInLocal() {
        ConfigFile("log[f].config.default.format = {loc.dat} {loc.tim}");
        var now = DateTime.Now;
        Log.Critical("What time is it?");
        Logging.Complete();
        var text = ReadFile();
        var parts = text.Split([' '], 3);
        var date = DateTime.Parse(parts[0]);
        var time = TimeSpan.Parse(parts[1]);
        var then = date + time;
        Assert.That(then, Is.EqualTo(now).Within(TimeSpan.FromSeconds(1)));
    }
}
