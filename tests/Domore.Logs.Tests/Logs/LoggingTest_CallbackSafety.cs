using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Logs;

partial class LoggingTest {
    [Test]
    public void NullTypeLoggerContainsAllInterfaceMethods() {
        var logger = Logging.For(null);
        var emissionFailures = 0;
        var checkFailures = 0;

        void Emission(Action action) {
            try { action(); }
            catch (Exception) { emissionFailures++; }
        }

        void Check(Func<bool> action) {
            try {
                if (action()) checkFailures++;
            }
            catch (Exception) { checkFailures++; }
        }

        Emission(() => logger.Data(LogSeverity.Info, "message"));
        Emission(() => logger.Debug("message"));
        Emission(() => logger.Info("message"));
        Emission(() => logger.Warn("message"));
        Emission(() => logger.Error("message"));
        Emission(() => logger.Critical("message"));
        Check(() => logger.Enabled(LogSeverity.Info));
        Check(logger.Debug);
        Check(logger.Info);
        Check(logger.Warn);
        Check(logger.Error);
        Check(logger.Critical);

        Assert.Multiple(() => {
            Assert.That(emissionFailures, Is.Zero);
            Assert.That(checkFailures, Is.Zero);
        });
    }

    [TestCase("Data")]
    [TestCase("Debug")]
    [TestCase("Info")]
    [TestCase("Warn")]
    [TestCase("Error")]
    [TestCase("Critical")]
    [TestCase("Enabled")]
    [TestCase("Debug check")]
    [TestCase("Info check")]
    [TestCase("Warn check")]
    [TestCase("Error check")]
    [TestCase("Critical check")]
    public void PublicInterfaceMethodContainsCallbackFailures(string method) {
        var logger = Logging.For(typeof(LoggingTest));
        var isCheck = method.Contains("check") || method == "Enabled";
        Logging.EventThreshold = isCheck ? LogSeverity.None : LogSeverity.Info;
        Logging.Event += (_, __) => throw new InvalidOperationException("Expected event callback failure.");
        Logging.Subscribe(new ThrowingCallbackSubscription());
        var previousError = Console.Error;
        Console.SetError(TextWriter.Null);
        try {
            Action action = method switch {
                "Data" => () => logger.Data(LogSeverity.Info, "message"),
                "Debug" => () => logger.Debug("message"),
                "Info" => () => logger.Info("message"),
                "Warn" => () => logger.Warn("message"),
                "Error" => () => logger.Error("message"),
                "Critical" => () => logger.Critical("message"),
                "Enabled" => () => logger.Enabled(LogSeverity.Info),
                "Debug check" => () => logger.Debug(),
                "Info check" => () => logger.Info(),
                "Warn check" => () => logger.Warn(),
                "Error check" => () => logger.Error(),
                "Critical check" => () => logger.Critical(),
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };
            Assert.DoesNotThrow(action);
        }
        finally {
            Console.SetError(previousError);
        }
    }

    private sealed class ThrowingTypeName : TypeDelegator {
        public ThrowingTypeName() : base(typeof(LoggingTest)) {
        }

        public override string Name => throw new InvalidOperationException("Expected type-name failure.");
    }

    [TestCase("Data")]
    [TestCase("Debug")]
    [TestCase("Info")]
    [TestCase("Warn")]
    [TestCase("Error")]
    [TestCase("Critical")]
    [TestCase("Enabled")]
    [TestCase("Debug check")]
    [TestCase("Info check")]
    [TestCase("Warn check")]
    [TestCase("Error check")]
    [TestCase("Critical check")]
    public void PublicInterfaceMethodContainsThresholdPathFailures(string method) {
        Config = $@"
            log[threshold-fault].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
            log[threshold-fault].config.default.severity = info
            log[threshold-fault].config[LoggingTest].severity = warn
        ";
        var logger = Logging.For(new ThrowingTypeName());
        var isCheck = method.Contains("check") || method == "Enabled";
        bool? enabled = null;
        Action action = method switch {
            "Data" => () => logger.Data(LogSeverity.Info, "message"),
            "Debug" => () => logger.Debug("message"),
            "Info" => () => logger.Info("message"),
            "Warn" => () => logger.Warn("message"),
            "Error" => () => logger.Error("message"),
            "Critical" => () => logger.Critical("message"),
            "Enabled" => () => enabled = logger.Enabled(LogSeverity.Info),
            "Debug check" => () => enabled = logger.Debug(),
            "Info check" => () => enabled = logger.Info(),
            "Warn check" => () => enabled = logger.Warn(),
            "Error check" => () => enabled = logger.Error(),
            "Critical check" => () => enabled = logger.Critical(),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
        var previousError = Console.Error;
        Console.SetError(TextWriter.Null);
        try {
            Assert.DoesNotThrow(action);
        }
        finally {
            Console.SetError(previousError);
        }

        if (isCheck) {
            Assert.That(enabled, Is.False);
        }
    }

    [Test]
    public void EventCallbackSuppressesNestedLoggingAndRestoresGuard() {
        var logger = Logging.For(typeof(LoggingTest));
        var events = 0;
        var nestedEnabled = true;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, e) => {
            Interlocked.Increment(ref events);
            if (e.LogList.FirstOrDefault() == "outer") {
                nestedEnabled = logger.Info();
                logger.Info("nested");
            }
        };

        logger.Info("outer");
        var enabledAfterCallback = logger.Info();
        logger.Info("after");

        Assert.Multiple(() => {
            Assert.That(events, Is.EqualTo(2));
            Assert.That(nestedEnabled, Is.False);
            Assert.That(enabledAfterCallback, Is.True);
        });
    }

    [Test]
    public void EventCallbackGuardFlowsToTasksAndSuppressesTheirLogging() {
        var logger = Logging.For(typeof(LoggingTest));
        var events = 0;
        var taskEnabled = true;
        var taskCompleted = false;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, e) => {
            Interlocked.Increment(ref events);
            if (e.LogList.FirstOrDefault() == "outer") {
                var task = Task.Run(() => {
                    logger.Info("async nested");
                    taskEnabled = logger.Info();
                });
                taskCompleted = task.Wait(TimeSpan.FromSeconds(5));
            }
        };

        logger.Info("outer");

        Assert.Multiple(() => {
            Assert.That(taskCompleted, Is.True);
            Assert.That(taskEnabled, Is.False);
            Assert.That(events, Is.EqualTo(1));
        });
    }

    private sealed class FormatterFeedbackValue {
    }

    [Test]
    public void DisabledEmissionSkipsFormatterCallbacks() {
        var logger = Logging.For(typeof(LoggingTest));
        var formatterCalls = 0;
        Logging.Format(typeof(FormatterFeedbackValue), _ => {
            Interlocked.Increment(ref formatterCalls);
            return ["should not be formatted"];
        });

        logger.Info(new FormatterFeedbackValue());

        Assert.That(formatterCalls, Is.Zero);
    }

    private sealed class ToStringFeedbackValue {
        private readonly ILog Logger;
        private readonly Action Callback;

        public ToStringFeedbackValue(ILog logger, Action callback) {
            Logger = logger;
            Callback = callback;
        }

        public override string ToString() {
            Callback();
            return "formatted by ToString";
        }
    }

    [Test]
    public void FormatterCallbackSuppressesSynchronousAndFlowingFeedback() {
        var logger = Logging.For(typeof(LoggingTest));
        var events = 0;
        var syncEnabled = true;
        var asyncEnabled = true;
        var asyncCompleted = false;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => Interlocked.Increment(ref events);
        Logging.Format(typeof(FormatterFeedbackValue), _ => {
            syncEnabled = logger.Info();
            logger.Info("from formatter");
            var task = Task.Run(() => {
                asyncEnabled = logger.Info();
                logger.Info("from formatter task");
            });
            asyncCompleted = task.Wait(TimeSpan.FromSeconds(5));
            return ["formatted"];
        });

        logger.Info(new FormatterFeedbackValue());
        var enabledAfterCallback = logger.Info();

        Assert.Multiple(() => {
            Assert.That(asyncCompleted, Is.True);
            Assert.That(syncEnabled, Is.False);
            Assert.That(asyncEnabled, Is.False);
            Assert.That(enabledAfterCallback, Is.True);
            Assert.That(events, Is.EqualTo(1));
        });
    }

    [Test]
    public void ThrowingFormatterCallbackRestoresLoggingGuard() {
        var logger = Logging.For(typeof(LoggingTest));
        var nestedEnabled = true;
        var events = 0;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => Interlocked.Increment(ref events);
        Logging.Format(typeof(FormatterFeedbackValue), _ => {
            nestedEnabled = logger.Info();
            logger.Info("from throwing formatter");
            throw new InvalidOperationException("Expected formatter callback failure.");
        });

        Assert.DoesNotThrow(() => logger.Info(new FormatterFeedbackValue()));
        var enabledAfterCallback = logger.Info();
        logger.Info("after formatter");

        Assert.Multiple(() => {
            Assert.That(nestedEnabled, Is.False);
            Assert.That(enabledAfterCallback, Is.True);
            Assert.That(events, Is.EqualTo(2));
        });
    }

    [Test]
    public void ToStringCallbackSuppressesSynchronousAndFlowingFeedback() {
        var logger = Logging.For(typeof(LoggingTest));
        var events = 0;
        var syncEnabled = true;
        var asyncEnabled = true;
        var asyncCompleted = false;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => Interlocked.Increment(ref events);
        var value = new ToStringFeedbackValue(logger, () => {
            syncEnabled = logger.Info();
            logger.Info("from ToString");
            var task = Task.Run(() => {
                asyncEnabled = logger.Info();
                logger.Info("from ToString task");
            });
            asyncCompleted = task.Wait(TimeSpan.FromSeconds(5));
        });

        logger.Info(value);
        var enabledAfterCallback = logger.Info();

        Assert.Multiple(() => {
            Assert.That(asyncCompleted, Is.True);
            Assert.That(syncEnabled, Is.False);
            Assert.That(asyncEnabled, Is.False);
            Assert.That(enabledAfterCallback, Is.True);
            Assert.That(events, Is.EqualTo(1));
        });
    }

    [Test]
    public void LoggingOutsideCallbackExecutionContextRemainsEnabled() {
        var logger = Logging.For(typeof(LoggingTest));
        using var callbackEntered = new ManualResetEventSlim();
        using var releaseCallback = new ManualResetEventSlim();
        var outerCallCompleted = false;
        var events = 0;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, e) => {
            Interlocked.Increment(ref events);
            if (e.LogList.FirstOrDefault() == "outer") {
                callbackEntered.Set();
                if (releaseCallback.Wait(TimeSpan.FromSeconds(5)) == false) {
                    throw new TimeoutException("The test did not release the event callback.");
                }
            }
        };
        var outer = Task.Run(() => logger.Info("outer"));
        try {
            Assert.That(callbackEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            logger.Info("unrelated");
            releaseCallback.Set();
            outerCallCompleted = outer.Wait(TimeSpan.FromSeconds(5));
        }
        finally {
            releaseCallback.Set();
            if (outer.IsCompleted == false) outer.Wait(TimeSpan.FromSeconds(5));
        }

        Assert.Multiple(() => {
            Assert.That(outerCallCompleted, Is.True);
            Assert.That(events, Is.EqualTo(2));
        });
    }

    private sealed class CallbackFeedbackSubscription : ILogSubscription {
        private readonly Action ThresholdCallback;
        private readonly Action ReceiveCallback;

        public CallbackFeedbackSubscription(Action thresholdCallback, Action receiveCallback) {
            ThresholdCallback = thresholdCallback;
            ReceiveCallback = receiveCallback;
        }

        event EventHandler ILogSubscription.ThresholdChanged {
            add { }
            remove { }
        }

        public LogSeverity Threshold(Type type) {
            ThresholdCallback?.Invoke();
            return LogSeverity.Info;
        }

        public void Receive(ILogEntry entry) {
            ReceiveCallback?.Invoke();
        }
    }

    [Test]
    public void SubscriberThresholdAndReceiveCallbacksSuppressNestedLogging() {
        var logger = Logging.For(typeof(LoggingTest));
        var events = 0;
        var thresholdEnabled = true;
        var receiveEnabled = true;
        var thresholdFeedbackSent = 0;
        var receiveFeedbackSent = 0;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => Interlocked.Increment(ref events);
        Logging.Subscribe(new CallbackFeedbackSubscription(
            thresholdCallback: () => {
                if (Interlocked.Exchange(ref thresholdFeedbackSent, 1) == 0) {
                    thresholdEnabled = logger.Info();
                    logger.Info("from threshold");
                }
            },
            receiveCallback: () => {
                if (Interlocked.Exchange(ref receiveFeedbackSent, 1) == 0) {
                    receiveEnabled = logger.Info();
                    logger.Info("from receive");
                }
            }));

        logger.Info("outer");

        Assert.Multiple(() => {
            Assert.That(thresholdEnabled, Is.False);
            Assert.That(receiveEnabled, Is.False);
            Assert.That(events, Is.EqualTo(1));
        });
    }

    private sealed class CallbackFeedbackService : ILogService {
        public static Action Constructing;
        public static Action Logging;
        public static bool FactoryEnabled;
        public static bool ServiceEnabled;
        public static int LogCalls;
        public static ManualResetEventSlim CallbackFinished { get; } = new();

        public CallbackFeedbackService() {
            Constructing?.Invoke();
        }

        public void Log(string name, string data, LogSeverity severity) {
            if (Interlocked.Increment(ref LogCalls) == 1) {
                Logging?.Invoke();
                CallbackFinished.Set();
            }
        }

        public void Complete() {
        }
    }

    [Test]
    public void FactoryAndServiceCallbacksSuppressNestedLogging() {
        var logger = Logging.For(typeof(LoggingTest));
        var events = 0;
        CallbackFeedbackService.FactoryEnabled = true;
        CallbackFeedbackService.ServiceEnabled = true;
        CallbackFeedbackService.LogCalls = 0;
        CallbackFeedbackService.CallbackFinished.Reset();
        CallbackFeedbackService.Constructing = () => {
            CallbackFeedbackService.FactoryEnabled = logger.Info();
            logger.Info("from factory");
        };
        CallbackFeedbackService.Logging = () => {
            CallbackFeedbackService.ServiceEnabled = logger.Info();
            logger.Info("from service");
        };
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => Interlocked.Increment(ref events);
        Config = $@"
            log[feedback].type = {typeof(CallbackFeedbackService).AssemblyQualifiedName}
            log[feedback].config.default.severity = info
        ";

        try {
            logger.Info("outer");
            Assert.That(CallbackFeedbackService.CallbackFinished.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Logging.Complete();
        }
        finally {
            CallbackFeedbackService.Constructing = null;
            CallbackFeedbackService.Logging = null;
        }

        Assert.Multiple(() => {
            Assert.That(CallbackFeedbackService.FactoryEnabled, Is.False);
            Assert.That(CallbackFeedbackService.ServiceEnabled, Is.False);
            Assert.That(events, Is.EqualTo(1));
            Assert.That(CallbackFeedbackService.LogCalls, Is.EqualTo(1));
        });
    }

    private sealed class HealthyCallbackSubscription : ILogSubscription {
        public int Receives { get; private set; }

        event EventHandler ILogSubscription.ThresholdChanged {
            add { }
            remove { }
        }

        public LogSeverity Threshold(Type type) => LogSeverity.Info;

        public void Receive(ILogEntry entry) {
            Receives++;
        }
    }

    private sealed class ThrowingCallbackSubscription : ILogSubscription {
        event EventHandler ILogSubscription.ThresholdChanged {
            add { }
            remove { }
        }

        public LogSeverity Threshold(Type type) => throw new InvalidOperationException("Expected threshold failure.");

        public void Receive(ILogEntry entry) => throw new InvalidOperationException("Expected subscriber failure.");
    }

    [Test]
    public void ThrowingSubscriberDoesNotPreventLaterSubscriber() {
        var healthy = new HealthyCallbackSubscription();
        Logging.Subscribe(new ThrowingCallbackSubscription());
        Logging.Subscribe(healthy);
        var logger = Logging.For(typeof(LoggingTest));

        var enabled = logger.Info();
        logger.Info("survives subscriber failure");

        Assert.Multiple(() => {
            Assert.That(enabled, Is.True);
            Assert.That(healthy.Receives, Is.EqualTo(1));
        });
    }

    [Test]
    public void FirstPublishedDefaultThresholdContinuesUpdatingEnabledSnapshot() {
        var logger = Logging.For(typeof(LoggingTest));
        var config = Logging.Config;
        var manager = (LogManager)config.GetType().GetProperty("Log").GetValue(config, null);
        var proxy = manager["threshold publication"];
        proxy.Type = typeof(HealthyCompleteLogService).AssemblyQualifiedName;

        proxy.Config.Default.Threshold = LogSeverity.None;
        var disabled = logger.Info();
        proxy.Config.Default.Threshold = LogSeverity.Info;
        var enabled = logger.Info();

        Assert.Multiple(() => {
            Assert.That(disabled, Is.False);
            Assert.That(enabled, Is.True);
        });
    }

    [Test]
    public void DefaultConfigReferenceUsesSafePublication() {
        var field = typeof(LogServiceConfig).GetField("_Default", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field.GetRequiredCustomModifiers(), Does.Contain(typeof(IsVolatile)));
    }

    [Test]
    public void DiagnosticWriterCannotRecursivelyLog() {
        var original = Console.Error;
        var logger = Logging.For(typeof(LoggingTest));
        var events = 0;
        var nestedDiagnosticRequested = 0;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => Interlocked.Increment(ref events);
        var writer = new LoggingDiagnosticWriter(() => {
            logger.Info("from diagnostic");
            if (Interlocked.Exchange(ref nestedDiagnosticRequested, 1) == 0) {
                var task = Task.Run(() => Logging.Notify("nested diagnostic"));
                if (task.Wait(TimeSpan.FromSeconds(5)) == false) {
                    throw new TimeoutException("The nested diagnostic task did not finish.");
                }
            }
        });
        try {
            Console.SetError(writer);
            Logging.Notify("diagnostic");
        }
        finally {
            Console.SetError(original);
        }

        Assert.That(events, Is.Zero);
        Assert.That(writer.Writes, Is.EqualTo(1));
    }

    private sealed class ThrowingDiagnosticValue {
        public override string ToString() => throw new InvalidOperationException("Expected diagnostic formatting failure.");
    }

    private sealed class ThrowingDiagnosticWriter : StringWriter {
        public override void WriteLine(string value) => throw new InvalidOperationException("Expected diagnostic writer failure.");
    }

    [Test]
    public void DiagnosticFormattingAndWriterFailuresAreContained() {
        var original = Console.Error;
        try {
            Console.SetError(new ThrowingDiagnosticWriter());
            Assert.DoesNotThrow(() => Logging.Notify("writer failure"));
            Assert.DoesNotThrow(() => Logging.Notify(new ThrowingDiagnosticValue()));
        }
        finally {
            Console.SetError(original);
        }
    }

    private sealed class LoggingDiagnosticWriter : StringWriter {
        private readonly Action WriteCallback;
        public int Writes { get; private set; }

        public LoggingDiagnosticWriter(Action writeCallback) {
            WriteCallback = writeCallback;
        }

        public override void WriteLine(string value) {
            Writes++;
            WriteCallback();
        }
    }
}
