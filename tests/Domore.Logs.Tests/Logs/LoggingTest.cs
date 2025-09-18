using Domore.Conf.Logs;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs {
    [TestFixture]
    public sealed partial class LoggingTest {
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

        [SetUp]
        public void SetUp() {
            Log = null;
        }

        [TearDown]
        public void TearDown() {
            Logging.Complete();
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
}
