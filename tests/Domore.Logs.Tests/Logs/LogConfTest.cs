using Domore.Conf;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using CONF = Domore.Conf.Conf;

namespace Domore.Logs;

[TestFixture]
[NonParallelizable]
public sealed class LogConfTest {
    private static readonly object Locker = new();
    private static readonly List<string> Messages = new();

    private static string Configuration(string severity = "info") =>
        $"log[test].type = {typeof(TestLogService).AssemblyQualifiedName}{Environment.NewLine}" +
        $"log[test].config.default.severity = {severity}";

    private static string CreateConfigurationFile(string content) {
        var path = Path.Combine(Environment.CurrentDirectory, $"LogConfTest-{Guid.NewGuid():N}.conf");
        File.WriteAllText(path, content);
        return path;
    }

    private static void DeleteConfigurationFile(string path) {
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    private static string[] GetMessages() {
        lock (Locker) {
            return Messages.ToArray();
        }
    }

    private static void ResetLogConf() {
        var field = typeof(Log.Conf).GetField("File", BindingFlags.NonPublic | BindingFlags.Static);
        var file = field.GetValue(null) as IDisposable;
        field.SetValue(null, null);
        file?.Dispose();
    }

    [SetUp]
    public void SetUp() {
        lock (Locker) {
            Messages.Clear();
        }
    }

    [TearDown]
    public void TearDown() {
        Logging.Complete();
        ResetLogConf();
    }

    [Test]
    public void LoggingCompleteResetsConfAndAllowsConfigurationAgain() {
        var path = CreateConfigurationFile(Configuration());
        try {
            Assert.That(Log.Conf.Configure(path), Is.True);
            var log = Logging.For(typeof(LogConfTest));
            log.Info("before complete");
            Logging.Complete();

            var configuredAfterComplete = Log.Conf.Configured;
            var configuredAgain = Log.Conf.Configure(path);
            log.Info("after complete");
            Logging.Complete();
            var messages = GetMessages();

            Assert.Multiple(() => {
                Assert.That(configuredAfterComplete, Is.False);
                Assert.That(configuredAgain, Is.True);
                Assert.That(messages.Any(message => message.Contains("before complete")), Is.True);
                Assert.That(messages.Any(message => message.Contains("after complete")), Is.True);
            });
        }
        finally {
            Logging.Complete();
            DeleteConfigurationFile(path);
        }
    }

    [Test]
    public void FailedConfigurationCanBeRetried() {
        var missingPath = Path.Combine(
            Environment.CurrentDirectory,
            $"LogConfTest-Missing-{Guid.NewGuid():N}",
            "missing.conf");
        var validPath = CreateConfigurationFile(Configuration());
        try {
            var error = Assert.Catch<Exception>(() => Log.Conf.Configure(missingPath));
            Assert.That(error, Is.Not.Null);
            Assert.That(Log.Conf.Configured, Is.False);
            Assert.That(Log.Conf.Configure(validPath), Is.True);
            Assert.That(Log.Conf.Configured, Is.True);
        }
        finally {
            Logging.Complete();
            DeleteConfigurationFile(validPath);
        }
    }

    [Test]
    public void NullConfigurationPathThrowsWithoutPublishingFile() {
        Assert.Throws<ArgumentNullException>(() => Log.Conf.Configure(null));
        Assert.That(Log.Conf.Configured, Is.False);
    }

    [Test]
    public void ReloadConfigurationErrorsAreReported() {
        const string invalidSeverity = "notaseverity";
        var invalidConfiguration = Configuration(invalidSeverity);
        using (var manager = new LogManager()) {
            var target = new { Log = manager };
            var error = Assert.Catch<Exception>(
                () => CONF.Contain(invalidConfiguration).Configure(target, key: ""));
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Message, Does.Contain(invalidSeverity));
        }

        var path = CreateConfigurationFile(Configuration());
        var listener = new CaptureTraceListener();
        try {
            Assert.That(Log.Conf.Configure(path), Is.True);
            Trace.Listeners.Add(listener);
            File.WriteAllText(path, invalidConfiguration);
            Assert.That(
                listener.WaitFor(invalidSeverity, TimeSpan.FromSeconds(5)),
                Is.True,
                "the reload error was not written to Trace");
            Assert.That(listener.Output, Does.Contain(invalidSeverity));
        }
        finally {
            Trace.Listeners.Remove(listener);
            listener.Dispose();
            Logging.Complete();
            DeleteConfigurationFile(path);
        }
    }

    private sealed class CaptureTraceListener : TraceListener {
        private readonly object Locker = new();
        private readonly StringBuilder OutputBuilder = new();

        public string Output {
            get {
                lock (Locker) {
                    return OutputBuilder.ToString();
                }
            }
        }

        public bool WaitFor(string value, TimeSpan timeout) {
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < timeout) {
                if (Output.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) {
                    return true;
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(25));
            }
            return Output.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public override void Write(string message) {
            lock (Locker) {
                OutputBuilder.Append(message);
            }
        }

        public override void WriteLine(string message) {
            Write(message);
        }
    }

    private sealed class TestLogService : ILogService {
        public void Log(string name, string data, LogSeverity severity) {
            lock (Locker) {
                Messages.Add(data);
            }
        }

        public void Complete() {
        }
    }
}
