using Domore.Conf.Logs;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs {
    [TestFixture]
    public sealed partial class LoggingTest {
        private string TempFile {
            get => _TempFile ??= Path.GetTempFileName();
            set => _TempFile = value;
        }
        private string _TempFile;

        private ILog Log {
            get => _Log ??= Logging.For(typeof(LoggingTest));
            set => _Log = value;
        }
        private ILog _Log;

        private string Config {
            get => _Config;
            set => CONF.Contain(_Config = value).Configure(Logging.Config, key: "");
        }
        private string _Config;

        private void ConfigFile(string config = null) {
            Config = $@"
                Log[f].type = file
                Log[f].service.directory = {Path.GetDirectoryName(TempFile)}
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
            TempFile = null;
            Log = null;
            Config = null;
        }

        [TearDown]
        public void TearDown() {
            Logging.Complete();
            File.Delete(TempFile);
        }

        [Test]
        public void FileLogsData() {
            ConfigFile();
            Log.Info("here's some data");
            Logging.Complete();
            var actual = ReadFile();
            var expected = "LoggingTest [inf] here's some data";
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void FileLogsDataWhileSubscriptionsAreActive() {
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
        public void FileDoesNotLogDataIfSeverityNotMet() {
            ConfigFile("log[f].config[LoggingTest].severity = warn");
            Log.Info("here's some data");
            Logging.Complete();
            var actual = ReadFile();
            Assert.That(actual, Is.Empty);
        }

        [Test]
        public void FileLogsDataIfSeverityIsChanged() {
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
        public void FileLogsLotsOfData() {
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
        public void FileRotatesLogFile() {
            var fileDir = Path.GetDirectoryName(TempFile);
            var fileName = $"domore.logs.loggingtest.{nameof(FileRotatesLogFile)}";
            var fileSearchPattern = $"{fileName}_*";
            File.Delete(Path.Combine(fileDir, fileName));
            Directory
                .GetFiles(fileDir, fileSearchPattern, SearchOption.TopDirectoryOnly)
                .ToList()
                .ForEach(File.Delete);
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

            File.Delete(Path.Combine(fileDir, fileName));
            Directory
                .GetFiles(fileDir, fileSearchPattern, SearchOption.TopDirectoryOnly)
                .ToList()
                .ForEach(File.Delete);
        }

        [Test]
        public void FileRotatesLogsManyTimes() {
            var fileDir = Path.GetDirectoryName(TempFile);
            var fileName = $"domore.logs.loggingtest.{nameof(FileRotatesLogsManyTimes)}";
            var fileSearchPattern = $"{fileName}_*";
            Directory
                .GetFiles(fileDir, fileSearchPattern, SearchOption.TopDirectoryOnly)
                .ToList()
                .ForEach(File.Delete);
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
            Directory
                .GetFiles(fileDir, fileSearchPattern, SearchOption.TopDirectoryOnly)
                .ToList()
                .ForEach(File.Delete);
        }

        [Test]
        public void FileRespectsTotalSizeLimit() {
            var fileDir = Path.GetDirectoryName(TempFile);
            var fileName = $"domore.logs.loggingtest.{nameof(FileRespectsTotalSizeLimit)}";
            var fileSearchPattern = $"{fileName}_*";
            Directory
                .GetFiles(fileDir, fileSearchPattern, SearchOption.TopDirectoryOnly)
                .ToList()
                .ForEach(File.Delete);
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
        public void FileRemovesLogsGreaterThanAgeLimit() {
            var fileDir = Path.GetDirectoryName(TempFile);
            var fileName = $"domore.logs.loggingtest.{nameof(FileRemovesLogsGreaterThanAgeLimit)}";
            var fileSearchPattern = $"{fileName}_*";
            Directory
                .GetFiles(fileDir, fileSearchPattern, SearchOption.TopDirectoryOnly)
                .ToList()
                .ForEach(File.Delete);
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
        public void FileLogsToSpecialFolderPath() {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest");
            try {
                ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest
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
        public void FileLogsToFormattedPath() {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest", AppDomain.CurrentDomain?.FriendlyName);
            try {
                ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest/{{appDomain.friendlyName}}
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
                Directory.Delete(dir, recursive: true);
            }
        }

        [Test]
        public void FileRetriesOnIOException() {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest");
            var file = Path.Combine(dir, "test.log");
            try {
                ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest
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
        public void FileDoesNotThrowIfInUse() {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest");
            var file = Path.Combine(dir, "test.log");
            try {
                ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest
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
        public void FileRecreatesFileIfDeleted() {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Domore", "Domore.Logs.LoggingTest");
            var file = Path.Combine(dir, "test.log");
            try {
                ConfigFile($@"
                    Log[f].service.directory = {{LocalApplicationData}}/Domore/Domore.Logs.LoggingTest
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
            CollectionAssert.AreEqual(new[] { "log1", "log2", "log3" }, message);
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

        [Test]
        public void CustomLogServiceIsInstantiated() {
            ConfigTest();
            Log.Info("Some message");
            Logging.Complete();
            var actual = TestLogService.Instance.Items.Select(i => i.Data);
            var expected = new[] { "LoggingTest [inf] Some message" };
            CollectionAssert.AreEqual(expected, actual);
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
}
