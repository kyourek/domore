using Domore.Logs.Mocks;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Domore.Logs;
public sealed partial class LoggingTest {
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

    private static void StartWorker(
        Action action,
        ManualResetEventSlim started,
        ManualResetEventSlim completed,
        Action<Exception> failed) {
        var worker = new Thread(() => {
            started.Set();
            try {
                action();
            }
            catch (Exception ex) {
                failed(ex);
            }
            finally {
                completed.Set();
            }
        });
        worker.IsBackground = true;
        worker.Start();
    }

    [Test]
    public void SubscriptionReceiveDoesNotDeadlockWhenLoggingOnAnotherThread() {
        var log = Logging.For(typeof(LoggingTest));
        var subscription = new MockLogSubscription {
            Threshold = _ => LogSeverity.Info,
            Receive = _ => { }
        };
        var started = new ManualResetEventSlim();
        var completed = new ManualResetEventSlim();
        var failure = default(Exception);
        var receiveEntered = 0;
        var workerStartedInReceive = false;
        var workerCompletedInReceive = false;

        subscription.Receive = _ => {
            if (Interlocked.Exchange(ref receiveEntered, 1) != 0) {
                return;
            }
            StartWorker(
                () => log.Info("inner"),
                started,
                completed,
                ex => failure = ex);
            workerStartedInReceive = started.Wait(WaitTimeout);
            workerCompletedInReceive = workerStartedInReceive && completed.Wait(WaitTimeout);
        };

        Assert.That(Logging.Subscribe(subscription), Is.True);
        log.Info("outer");

        var workerCompleted = completed.Wait(WaitTimeout);
        Assert.That(workerStartedInReceive, Is.True);
        Assert.That(workerCompletedInReceive, Is.True, "worker did not complete before Receive returned");
        Assert.That(workerCompleted, Is.True);
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void SubscriptionThresholdDoesNotDeadlockWhenEnabledRunsOnAnotherThread() {
        var log = Logging.For(typeof(LoggingTest));
        var subscription = new MockLogSubscription {
            Threshold = _ => LogSeverity.Info,
            Receive = _ => { }
        };
        var started = new ManualResetEventSlim();
        var completed = new ManualResetEventSlim();
        var failure = default(Exception);
        var thresholdEntered = 0;
        var innerEnabled = false;
        var workerStartedInThreshold = false;
        var workerCompletedInThreshold = false;

        subscription.Threshold = _ => {
            if (Interlocked.Exchange(ref thresholdEntered, 1) == 0) {
                StartWorker(
                    () => innerEnabled = log.Enabled(LogSeverity.Info),
                    started,
                    completed,
                    ex => failure = ex);
                workerStartedInThreshold = started.Wait(WaitTimeout);
                workerCompletedInThreshold = workerStartedInThreshold && completed.Wait(WaitTimeout);
            }
            return LogSeverity.Info;
        };

        Assert.That(Logging.Subscribe(subscription), Is.True);
        var enabled = log.Enabled(LogSeverity.Info);

        var workerCompleted = completed.Wait(WaitTimeout);
        Assert.That(workerStartedInThreshold, Is.True);
        Assert.That(workerCompletedInThreshold, Is.True, "worker did not complete before Threshold returned");
        Assert.That(workerCompleted, Is.True);
        Assert.That(innerEnabled, Is.True);
        Assert.That(enabled, Is.True);
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void SubscriptionThresholdChangedDoesNotDeadlockWhenRaisedOnAnotherThreadDuringReceive() {
        var subscription = new MockLogSubscription {
            Threshold = _ => LogSeverity.Info,
            Receive = _ => { }
        };
        var started = new ManualResetEventSlim();
        var completed = new ManualResetEventSlim();
        var failure = default(Exception);
        var receiveEntered = 0;
        var workerStartedInReceive = false;
        var workerCompletedInReceive = false;

        subscription.Receive = _ => {
            if (Interlocked.Exchange(ref receiveEntered, 1) != 0) {
                return;
            }
            StartWorker(
                subscription.ThresholdChanged,
                started,
                completed,
                ex => failure = ex);
            workerStartedInReceive = started.Wait(WaitTimeout);
            workerCompletedInReceive = workerStartedInReceive && completed.Wait(WaitTimeout);
        };

        Assert.That(Logging.Subscribe(subscription), Is.True);
        Log.Info("outer");

        var workerCompleted = completed.Wait(WaitTimeout);
        Assert.That(workerStartedInReceive, Is.True);
        Assert.That(workerCompletedInReceive, Is.True, "worker did not complete before Receive returned");
        Assert.That(workerCompleted, Is.True);
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void SubscriptionChangesCanRunConcurrentlyWithLogging() {
        const int loggerCount = 4;
        const int subscriptionCount = 2;
        const int iterations = 500;

        var log = Logging.For(typeof(LoggingTest));
        var subscription = new MockLogSubscription {
            Threshold = _ => LogSeverity.Info,
            Receive = _ => { }
        };
        var ready = new ManualResetEventSlim();
        var completed = new CountdownEvent(loggerCount + subscriptionCount);
        var failures = new ConcurrentQueue<Exception>();

        for (var index = 0; index < subscriptionCount; index++) {
            var worker = new Thread(() => {
                try {
                    if (ready.Wait(WaitTimeout) == false) {
                        throw new TimeoutException("subscription worker did not start");
                    }
                    for (var iteration = 0; iteration < iterations; iteration++) {
                        Logging.Subscribe(subscription);
                        Logging.Unsubscribe(subscription);
                    }
                }
                catch (Exception ex) {
                    failures.Enqueue(ex);
                }
                finally {
                    completed.Signal();
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        for (var index = 0; index < loggerCount; index++) {
            var worker = new Thread(() => {
                try {
                    if (ready.Wait(WaitTimeout) == false) {
                        throw new TimeoutException("logging worker did not start");
                    }
                    for (var iteration = 0; iteration < iterations; iteration++) {
                        log.Info("message");
                    }
                }
                catch (Exception ex) {
                    failures.Enqueue(ex);
                }
                finally {
                    completed.Signal();
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        ready.Set();
        var workersCompleted = completed.Wait(WaitTimeout);
        Assert.That(workersCompleted, Is.True, "concurrent workers did not complete");
        Assert.That(failures.IsEmpty, Is.True, "logging workers threw exceptions");
    }
}
