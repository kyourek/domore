using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs {
    [TestFixture]
    public sealed class LoggingTest {
        private string TempFile {
            get => _TempFile ?? (_TempFile = Path.GetTempFileName());
            set => _TempFile = value;
        }
        private string _TempFile;

        private ILog Log {
            get => _Log ?? (_Log = Logging.For(typeof(LoggingTest)));
            set => _Log = value;
        }
        private ILog _Log;

        private string Config {
            get => _Config;
            set {
                if (_Config != value) {
                    CONF.Contain(_Config = value).Configure(Logging.Config, key: "");
                }
            }
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
            Assert.AreEqual("crt Some data that will be in a dated log" + Environment.NewLine, File.ReadAllText(datedLog));

            var originalLog = Path.Combine(fileDir, fileName);
            Assert.AreEqual("crt More data that will be in the original log" + Environment.NewLine, File.ReadAllText(originalLog));

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
    }
}
