using NUnit.Framework;
using System;
using System.Threading;

namespace Domore.Logs;
public sealed partial class LoggingTest {
    private void ConfigureService(string name, Type serviceType) {
        Config = $"log[{name}].type = {serviceType.AssemblyQualifiedName}" + Environment.NewLine +
            $"log[{name}].config.default.severity = info";
    }

    [Test]
    public void ReviewServiceLogDoesNotHoldTheEnabledLock() {
        BlockingLogService.Reset();
        ConfigureService("blocking", typeof(BlockingLogService));
        var log = Logging.For(typeof(LoggingTest));
        log.Info("block");

        var entered = BlockingLogService.Entered.Wait(WaitTimeout);
        var started = new ManualResetEventSlim();
        var completed = new ManualResetEventSlim();
        var enabled = false;
        var failure = default(Exception);
        var worker = new Thread(() => {
            started.Set();
            try {
                enabled = log.Enabled(LogSeverity.Info);
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
        var returnedWhileBlocked = false;
        var serviceReturnedAfterRelease = false;

        try {
            if (entered) {
                worker.Start();
                returnedWhileBlocked = started.Wait(WaitTimeout) && completed.Wait(TimeSpan.FromSeconds(1));
            }
        }
        finally {
            BlockingLogService.Release.Set();
            completed.Wait(WaitTimeout);
            serviceReturnedAfterRelease = BlockingLogService.Exited.Wait(WaitTimeout);
        }

        Assert.That(entered, Is.True, "the service did not enter its blocking Log call");
        Assert.That(returnedWhileBlocked, Is.True, "Enabled waited for the blocked service");
        Assert.That(enabled, Is.True);
        Assert.That(failure, Is.Null);
        Assert.That(serviceReturnedAfterRelease, Is.True);
    }

    [Test]
    public void ReviewServiceCompletionContinuesAfterFailureAndFreshLoggingWorks() {
        ThrowingCompleteLogService.CompleteCount = 0;
        RecordingCompleteLogService.Completed.Reset();
        var log = Logging.For(typeof(LoggingTest));
        Config = $@"
log[a].type = {typeof(ThrowingCompleteLogService).AssemblyQualifiedName}
log[a].config.default.severity = info
log[b].type = {typeof(RecordingCompleteLogService).AssemblyQualifiedName}
log[b].config.default.severity = info
";
        log.Info("before complete");

        var failure = default(Exception);
        try {
            Logging.Complete();
        }
        catch (Exception ex) {
            failure = ex;
        }

        var laterServiceCompleted = RecordingCompleteLogService.Completed.Wait(WaitTimeout);
        FreshLogService.Reset();
        ConfigureService("fresh", typeof(FreshLogService));
        log.Info("after complete");
        var freshMessageDelivered = FreshLogService.Received.Wait(WaitTimeout);

        Assert.Multiple(() => {
            Assert.That(failure, Is.Null, "Logging.Complete propagated a service failure");
            Assert.That(ThrowingCompleteLogService.CompleteCount, Is.EqualTo(1));
            Assert.That(laterServiceCompleted, Is.True, "a later service was not completed");
            Assert.That(freshMessageDelivered, Is.True, "logging did not use the fresh manager");
            Assert.That(FreshLogService.LastMessage, Is.EqualTo("after complete"));
        });
    }

    [Test]
    public void ReviewLoggingCompleteReturnsWhenServiceLogIsBlocked() {
        BlockingLogService.Reset();
        ConfigureService("blocking", typeof(BlockingLogService));
        var log = Logging.For(typeof(LoggingTest));
        log.Info("block");

        var entered = BlockingLogService.Entered.Wait(WaitTimeout);
        var started = new ManualResetEventSlim();
        var completed = new ManualResetEventSlim();
        var failure = default(Exception);
        var worker = new Thread(() => {
            started.Set();
            try {
                Logging.Complete();
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
        var returnedWithinTimeout = false;
        var serviceReturnedAfterRelease = false;

        try {
            if (entered) {
                worker.Start();
                returnedWithinTimeout = started.Wait(WaitTimeout) && completed.Wait(TimeSpan.FromSeconds(7));
            }
        }
        finally {
            BlockingLogService.Release.Set();
            completed.Wait(WaitTimeout);
            serviceReturnedAfterRelease = BlockingLogService.Exited.Wait(WaitTimeout);
        }

        Assert.That(entered, Is.True, "the service did not enter its blocking Log call");
        Assert.That(returnedWithinTimeout, Is.True, "Logging.Complete did not respect its queue timeout");
        Assert.That(failure, Is.Null);
        Assert.That(serviceReturnedAfterRelease, Is.True);
    }

    [Test]
    public void ReviewLoggingCompleteReturnsWhenCalledFromTheServiceThread() {
        SelfCompletingLogService.Reset();
        ConfigureService("self", typeof(SelfCompletingLogService));
        Logging.For(typeof(LoggingTest)).Info("complete from service");

        var entered = SelfCompletingLogService.Entered.Wait(WaitTimeout);
        var returnedBeforeInterrupt = false;
        try {
            returnedBeforeInterrupt = entered && SelfCompletingLogService.Returned.Wait(TimeSpan.FromSeconds(2));
        }
        finally {
            if (entered && returnedBeforeInterrupt == false) {
                var worker = SelfCompletingLogService.Worker;
                if (worker != null) {
                    try {
                        worker.Interrupt();
                    }
                    catch (ThreadStateException) {
                    }
                }
                SelfCompletingLogService.Returned.Wait(WaitTimeout);
            }
        }

        Assert.That(entered, Is.True, "the service did not enter its Log call");
        Assert.That(returnedBeforeInterrupt, Is.True, "Logging.Complete joined its own service thread");
        Assert.That(SelfCompletingLogService.Failure, Is.Null);
    }

    [Test]
    public void ReviewLoggingCompleteReturnsFalseWhenQueueDoesNotDrain() {
        BlockingLogService.Reset();
        ConfigureService("blocking", typeof(BlockingLogService));
        Logging.For(typeof(LoggingTest)).Info("block");

        var entered = BlockingLogService.Entered.Wait(WaitTimeout);
        var queueDrained = true;
        var serviceReturnedAfterRelease = false;
        var failure = default(Exception);
        try {
            queueDrained = Logging.Complete(TimeSpan.FromMilliseconds(100));
        }
        catch (Exception ex) {
            failure = ex;
        }
        finally {
            BlockingLogService.Release.Set();
            serviceReturnedAfterRelease = BlockingLogService.Exited.Wait(WaitTimeout);
        }

        Assert.Multiple(() => {
            Assert.That(entered, Is.True, "the service did not enter its blocking Log call");
            Assert.That(queueDrained, Is.False);
            Assert.That(failure, Is.Null);
            Assert.That(serviceReturnedAfterRelease, Is.True);
        });
    }

    [Test]
    public void ReviewLoggingCompleteReturnsTrueWhenQueueDrains() {
        FreshLogService.Reset();
        ConfigureService("fresh", typeof(FreshLogService));
        Logging.For(typeof(LoggingTest)).Info("drain");

        var queueDrained = Logging.Complete(TimeSpan.FromSeconds(5));

        Assert.Multiple(() => {
            Assert.That(queueDrained, Is.True);
            Assert.That(FreshLogService.Received.IsSet, Is.True);
            Assert.That(FreshLogService.LastMessage, Is.EqualTo("drain"));
        });
    }

    private sealed class BlockingLogService : ILogService {
        public static ManualResetEventSlim Entered { get; } = new();
        public static ManualResetEventSlim Exited { get; } = new();
        public static ManualResetEventSlim Release { get; } = new();

        public static void Reset() {
            Entered.Reset();
            Exited.Reset();
            Release.Reset();
        }

        public void Log(string name, string data, LogSeverity severity) {
            Entered.Set();
            try {
                Release.Wait(TimeSpan.FromSeconds(30));
            }
            finally {
                Exited.Set();
            }
        }

        public void Complete() {
        }
    }

    private sealed class ThrowingCompleteLogService : ILogService {
        public static int CompleteCount;

        public void Log(string name, string data, LogSeverity severity) {
        }

        public void Complete() {
            Interlocked.Increment(ref CompleteCount);
            throw new InvalidOperationException("service complete failed");
        }
    }

    private sealed class RecordingCompleteLogService : ILogService {
        public static ManualResetEventSlim Completed { get; } = new();

        public void Log(string name, string data, LogSeverity severity) {
        }

        public void Complete() {
            Completed.Set();
        }
    }

    private sealed class FreshLogService : ILogService {
        public static ManualResetEventSlim Received { get; } = new();
        public static string LastMessage { get; private set; }

        public static void Reset() {
            LastMessage = null;
            Received.Reset();
        }

        public void Log(string name, string data, LogSeverity severity) {
            LastMessage = data;
            Received.Set();
        }

        public void Complete() {
        }
    }

    private sealed class SelfCompletingLogService : ILogService {
        public static ManualResetEventSlim Entered { get; } = new();
        public static ManualResetEventSlim Returned { get; } = new();
        public static Thread Worker { get; private set; }
        public static Exception Failure { get; private set; }

        public static void Reset() {
            Worker = null;
            Failure = null;
            Entered.Reset();
            Returned.Reset();
        }

        public void Log(string name, string data, LogSeverity severity) {
            Worker = Thread.CurrentThread;
            Entered.Set();
            try {
                Logging.Complete();
            }
            catch (ThreadInterruptedException) {
            }
            catch (Exception ex) {
                Failure = ex;
            }
            finally {
                Returned.Set();
            }
        }

        public void Complete() {
        }
    }
}
