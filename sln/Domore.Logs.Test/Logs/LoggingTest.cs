using NUnit.Framework;
using System.IO;
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
    }
}
