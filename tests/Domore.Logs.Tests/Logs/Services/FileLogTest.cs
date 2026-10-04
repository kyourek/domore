using Domore.Logs.Mocks;
using Domore.Logs.Service;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs.Services; 
[TestFixture]
[NonParallelizable]
internal sealed class FileLogTest {
    private string Id {
        get => _Id ??= Guid.NewGuid().ToString();
        set => _Id = value;
    }
    private string _Id;

    private string TempDir {
        get => _TempDir ??= Path.Combine(Path.GetTempPath(), "domore.logs.loggingtest", Id);
        set => _TempDir = value;
    }
    private string _TempDir;

    public string TempFile {
        get => _TempFile ??= new Func<string>(() => {
            if (Directory.Exists(TempDir) == false) {
                Directory.CreateDirectory(TempDir);
            }
            var path = Path.Combine(TempDir, "domore.logs.loggingtest");
            using (File.Create(path)) {
                return path;
            }
        })();
        set => _TempFile = value;
    }
    private string _TempFile;

    private string Config {
        get => _Config;
        set => CONF.Contain(_Config = value).Configure(Logging.Config, key: "");
    }
    private string _Config;

    private ILog Log {
        get => _Log ??= Logging.For(typeof(LoggingTest));
        set => _Log = value;
    }
    private ILog _Log;

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

    private bool WaitForFileContains(string value, TimeSpan timeout) {
        var end = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < end) {
            try {
                if (File.Exists(TempFile) && File.ReadAllText(TempFile).Contains(value)) {
                    return true;
                }
            }
            catch (IOException) {
            }
            Thread.Sleep(10);
        }
        return File.Exists(TempFile) && File.ReadAllText(TempFile).Contains(value);
    }

    private static bool WaitFor(Func<bool> condition, TimeSpan timeout) {
        var end = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < end) {
            try {
                if (condition()) {
                    return true;
                }
            }
            catch (IOException) {
            }
            Thread.Sleep(10);
        }
        try {
            return condition();
        }
        catch (IOException) {
            return false;
        }
    }

    private static void Rotate(FileLog log) {
        typeof(FileLog)
            .GetMethod("Rotate", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(log, null);
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

    [Test]
    public void FileLogTimerClampsSettings() {
        var log = new FileLog();

        log.LogCountLimit = 0;
        Assert.That(log.LogCountLimit, Is.EqualTo(1));
        log.LogCountLimit = -1;
        Assert.That(log.LogCountLimit, Is.EqualTo(1));

        log.IORetryLimit = 0;
        Assert.That(log.IORetryLimit, Is.EqualTo(1));
        log.IORetryLimit = -1;
        Assert.That(log.IORetryLimit, Is.EqualTo(1));

        log.IORetryDelay = -5;
        Assert.That(log.IORetryDelay, Is.Zero);

        log.FlushInterval = TimeSpan.FromMilliseconds(-1);
        Assert.That(log.FlushInterval, Is.EqualTo(TimeSpan.FromMilliseconds(1)));
        log.FlushInterval = TimeSpan.Zero;
        Assert.That(log.FlushInterval, Is.EqualTo(TimeSpan.FromMilliseconds(1)));
        log.FlushInterval = TimeSpan.MaxValue;
        Assert.That(log.FlushInterval, Is.EqualTo(TimeSpan.FromMilliseconds(int.MaxValue)));
    }

    [Test]
    public void FileLogTimerFlushesZeroCountLimitBeforeComplete() {
        ConfigFile("log[f].service.log count limit = 0");
        Log.Info("zero count limit line");

        Assert.That(
            WaitForFileContains("zero count limit line", TimeSpan.FromSeconds(5)),
            Is.True);
    }

    [Test]
    public void FileLogTimerFlushesInvalidFlushIntervalBeforeComplete() {
        ConfigFile("log[f].service.flush interval = -00:00:01");
        Log.Info("invalid flush interval line");

        Assert.That(
            WaitForFileContains("invalid flush interval line", TimeSpan.FromSeconds(5)),
            Is.True);
    }

    [Test]
    public void FileLogTimerFlushesZeroIntervalBeforeComplete() {
        var output = new StringWriter();
        var original = Console.Out;
        try {
            Console.SetOut(output);
            ConfigFile("log[f].service.flush interval = 00:00:00");
            for (var i = 0; i < 5; i++) {
                Log.Info($"zero flush interval line {i}");
                Thread.Sleep(25);
            }

            Assert.That(
                WaitForFileContains("zero flush interval line 4", TimeSpan.FromSeconds(5)),
                Is.True);
            Assert.That(output.ToString(), Is.Empty);
        }
        finally {
            Console.SetOut(original);
        }
    }

    [Test]
    public void FileLogTimerReusesTimerForZeroInterval() {
        var log = new FileLog {
            Directory = TempDir,
            Name = Path.GetFileName(TempFile),
            FlushInterval = TimeSpan.Zero
        };
        var service = (ILogService)log;
        try {
            service.Log(nameof(FileLogTimerReusesTimerForZeroInterval), "zero interval timer line", LogSeverity.Info);
            var timerField = typeof(FileLog).GetField("Timer", BindingFlags.Instance | BindingFlags.NonPublic);
            var timer = (Timer)timerField.GetValue(log);

            Assert.That(
                WaitForFileContains("zero interval timer line", TimeSpan.FromSeconds(5)),
                Is.True);
            Thread.Sleep(100);

            Assert.That(timerField.GetValue(log), Is.SameAs(timer));
        }
        finally {
            service.Complete();
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

    [Test]
    public void RotatedFileNamesEncodeUtc() {
        if (TimeZoneInfo.Local.GetUtcOffset(DateTime.Now) == TimeSpan.Zero) {
            Assert.Inconclusive("The local time zone is UTC, so local and UTC rotation names are indistinguishable.");
        }

        var log = new FileLog {
            Directory = TempDir,
            Name = "utc.log",
            FileSizeLimit = 1,
            FileAgeLimit = TimeSpan.FromHours(1),
            FlushInterval = TimeSpan.FromDays(1)
        };
        var service = (ILogService)log;
        try {
            service.Log(nameof(RotatedFileNamesEncodeUtc), "utc rotation", LogSeverity.Info);
            service.Complete();
            Rotate(log);

            var rotated = Directory.GetFiles(TempDir, "utc_*.log", SearchOption.TopDirectoryOnly).Single();
            var timestamp = Path.GetFileNameWithoutExtension(rotated).Substring("utc_".Length);
            var parsed = DateTime.TryParseExact(
                timestamp,
                "yyyyMMdd-HHmmss-fff",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var encoded);

            Assert.That(parsed, Is.True);
            Assert.That(Math.Abs((DateTime.UtcNow - encoded).TotalSeconds), Is.LessThan(5));
        }
        finally {
            service.Complete();
        }
    }

    [Test]
    public void InvalidRotationFileNameDoesNotStopAgeCleanup() {
        Directory.CreateDirectory(TempDir);
        var expired = Path.Combine(TempDir, "cleanup_20200101-000000-000.log");
        var invalid = Path.Combine(TempDir, "cleanup_20261399-250000-000.log");
        File.WriteAllText(expired, "old");
        File.WriteAllText(invalid, "invalid");

        var log = new FileLog {
            Directory = TempDir,
            Name = "cleanup.log",
            FileSizeLimit = 1,
            FileAgeLimit = TimeSpan.Zero,
            TotalSizeLimit = long.MaxValue,
            FlushInterval = TimeSpan.FromDays(1)
        };
        var service = (ILogService)log;
        try {
            service.Log(nameof(InvalidRotationFileNameDoesNotStopAgeCleanup), "active", LogSeverity.Info);
            service.Complete();

            Exception failure = null;
            try {
                Rotate(log);
            }
            catch (Exception ex) {
                failure = ex;
            }

            Assert.That(failure, Is.Null);
            Assert.That(File.Exists(expired), Is.False);
            Assert.That(File.Exists(invalid), Is.True);
        }
        finally {
            service.Complete();
        }
    }

    [Test]
    public void LockedExpiredRotationFileDoesNotStopAgeCleanup() {
        Directory.CreateDirectory(TempDir);
        var locked = Path.Combine(TempDir, "cleanup_20190101-000000-000.log");
        var expired = Path.Combine(TempDir, "cleanup_20200101-000000-000.log");
        File.WriteAllText(locked, "locked");
        File.WriteAllText(expired, "expired");

        var log = new FileLog {
            Directory = TempDir,
            Name = "cleanup.log",
            FileSizeLimit = 1,
            FileAgeLimit = TimeSpan.Zero,
            TotalSizeLimit = long.MaxValue,
            FlushInterval = TimeSpan.FromDays(1)
        };
        var service = (ILogService)log;
        FileStream lockedStream = null;
        try {
            lockedStream = File.Open(locked, FileMode.Open, FileAccess.Read, FileShare.None);
            service.Log(nameof(LockedExpiredRotationFileDoesNotStopAgeCleanup), "active", LogSeverity.Info);
            service.Complete();

            Exception failure = null;
            try {
                Rotate(log);
            }
            catch (Exception ex) {
                failure = ex;
            }

            Assert.That(failure, Is.Null);
            Assert.That(File.Exists(expired), Is.False);
        }
        finally {
            lockedStream?.Dispose();
            service.Complete();
        }
    }

    [Test]
    public void ResolvesThreadPathTokensBeforeFlushAndReusesPathAfterRotation() {
        var callerThreadId = Thread.CurrentThread.ManagedThreadId;
        var baseName = $"x-{callerThreadId}";
        var active = Path.Combine(TempDir, $"{baseName}.log");
        var log = new FileLog {
            Directory = TempDir,
            Name = "x-{Thread.ManagedThreadId}.log",
            FileSizeLimit = 1,
            TotalSizeLimit = long.MaxValue,
            FileAgeLimit = TimeSpan.FromDays(90),
            FlushInterval = TimeSpan.FromMilliseconds(5)
        };
        var service = (ILogService)log;
        try {
            service.Log(
                nameof(ResolvesThreadPathTokensBeforeFlushAndReusesPathAfterRotation),
                "first",
                LogSeverity.Info);
            var firstRotationObserved = WaitFor(
                () => Directory.GetFiles(TempDir, "x-*_*.log", SearchOption.TopDirectoryOnly).Length >= 1,
                TimeSpan.FromSeconds(5));
            Thread.Sleep(20);

            service.Log(
                nameof(ResolvesThreadPathTokensBeforeFlushAndReusesPathAfterRotation),
                "second",
                LogSeverity.Info);
            var secondRotationObserved = WaitFor(
                () => Directory.GetFiles(TempDir, "x-*_*.log", SearchOption.TopDirectoryOnly).Length >= 2,
                TimeSpan.FromSeconds(5));

            log.FileSizeLimit = long.MaxValue;
            service.Log(
                nameof(ResolvesThreadPathTokensBeforeFlushAndReusesPathAfterRotation),
                "third",
                LogSeverity.Info);
            var activeObserved = WaitFor(
                () => File.Exists(active) && File.ReadAllText(active).Contains("third"),
                TimeSpan.FromSeconds(5));
            var rotated = Directory.GetFiles(TempDir, $"{baseName}_*.log", SearchOption.TopDirectoryOnly);
            var allRotated = Directory.GetFiles(TempDir, "x-*_*.log", SearchOption.TopDirectoryOnly);

            Assert.Multiple(() => {
                Assert.That(firstRotationObserved, Is.True, "The first log should rotate.");
                Assert.That(secondRotationObserved, Is.True, "The second log should rotate.");
                Assert.That(activeObserved, Is.True, "The active file should retain the caller-thread name.");
                Assert.That(rotated.Length, Is.GreaterThanOrEqualTo(2));
                Assert.That(
                    allRotated.Length,
                    Is.EqualTo(rotated.Length),
                    "Every rotated file should use the cached base name.");
            });
        }
        finally {
            service.Complete();
        }
    }

    [Test]
    public void FileLogSettingsCanBeReadAndWrittenWhileLogging() {
        var log = new FileLog {
            Directory = TempDir,
            Name = "settings.log",
            FileSizeLimit = 1024,
            TotalSizeLimit = 100000,
            FileAgeLimit = TimeSpan.FromDays(1),
            FlushInterval = TimeSpan.FromMilliseconds(10)
        };
        var service = (ILogService)log;
        var failures = new List<Exception>();
        var mutate = new Thread(() => {
            try {
                for (var i = 0; i < 1000; i++) {
                    log.FileSizeLimit = (i & 1) == 0 ? 1 : long.MaxValue;
                    log.TotalSizeLimit = (i & 1) == 0 ? 1 : long.MaxValue;
                    log.FileAgeLimit = (i & 1) == 0 ? TimeSpan.Zero : TimeSpan.MaxValue;
                    log.FlushInterval = TimeSpan.FromMilliseconds(1 + (i % 10));
                }
            }
            catch (Exception ex) {
                lock (failures) {
                    failures.Add(ex);
                }
            }
        }) {
            IsBackground = true
        };
        var write = new Thread(() => {
            try {
                for (var i = 0; i < 100; i++) {
                    service.Log(nameof(FileLogSettingsCanBeReadAndWrittenWhileLogging), $"{i}", LogSeverity.Info);
                }
            }
            catch (Exception ex) {
                lock (failures) {
                    failures.Add(ex);
                }
            }
        }) {
            IsBackground = true
        };

        try {
            mutate.Start();
            write.Start();
            Assert.That(mutate.Join(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(write.Join(TimeSpan.FromSeconds(5)), Is.True);
        }
        finally {
            if (mutate.IsAlive) {
                mutate.Join(TimeSpan.FromSeconds(1));
            }
            if (write.IsAlive) {
                write.Join(TimeSpan.FromSeconds(1));
            }
            service.Complete();
        }

        lock (failures) {
            Assert.That(failures, Is.Empty);
        }
    }

    [Test]
    public void FileLogLogUsesLockFreeFastPathAfterPathResolution() {
        var log = new FileLog {
            Directory = TempDir,
            Name = "fast-path.log",
            FlushInterval = TimeSpan.FromDays(1)
        };
        var service = (ILogService)log;
        var locker = typeof(FileLog)
            .GetField("Locker", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(log);
        var completed = new ManualResetEvent(false);
        Exception failure = null;
        var writer = new Thread(() => {
            try {
                service.Log(nameof(FileLogLogUsesLockFreeFastPathAfterPathResolution), "second", LogSeverity.Info);
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
        var started = false;
        try {
            service.Log(nameof(FileLogLogUsesLockFreeFastPathAfterPathResolution), "first", LogSeverity.Info);
            Assert.That(log.Started, Is.True);
            lock (locker) {
                writer.Start();
                started = true;
                Assert.That(completed.WaitOne(TimeSpan.FromSeconds(5)), Is.True);
            }
            Assert.That(failure, Is.Null);
        }
        finally {
            if (started && writer.IsAlive) {
                writer.Join(TimeSpan.FromSeconds(5));
            }
            service.Complete();
            completed.Dispose();
        }
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
        var datedLogs = Directory.GetFiles(TempDir, $"{name}_????????-??????-???", SearchOption.TopDirectoryOnly);
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
        var datedLogs = Directory.GetFiles(TempDir, $"{name}_????????-??????-???.{extension}", SearchOption.TopDirectoryOnly);
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
        var datedLogs = Directory.GetFiles(TempDir, $"{name}_????????-??????-???", SearchOption.TopDirectoryOnly);
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
        var datedLogs = Directory.GetFiles(TempDir, $"{name}_????????-??????-???.{extension}", SearchOption.TopDirectoryOnly);
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
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest", id.ToString(), AppDomain.CurrentDomain?.FriendlyName);
        try {
            ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest/{id}/{{appDomain.friendlyName}}
                    log[f].service.name = test-{{thread.ManagedThreadID}}.log
                    LOG[f].config.default.format = {{sev}}
                ");
            Log.Info("Got the message?");
            Logging.Complete();
            var files = Directory.GetFiles(dir, "test-*.log", SearchOption.TopDirectoryOnly);
            Assert.That(files, Has.Length.EqualTo(1));
            Assert.That(Path.GetFileName(files[0]), Does.Match(@"^test-\d+\.log$"));
            var actual = File.ReadAllText(files[0]).Trim();
            var expected = "inf Got the message?";
            Assert.That(actual, Is.EqualTo(expected));
        }
        finally {
            Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest"), recursive: true);
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
