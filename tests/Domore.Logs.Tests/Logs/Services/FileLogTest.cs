using Domore.Logs.Mocks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs.Services {
    [TestFixture]
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
                var actual = File.ReadAllText(Path.Combine(dir, $"test-{Thread.CurrentThread.ManagedThreadId}.log")).Trim();
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
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
