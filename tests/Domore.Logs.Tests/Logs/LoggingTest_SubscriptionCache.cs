using Domore.Logs.Mocks;
using NUnit.Framework;
using System;
using System.Threading;

namespace Domore.Logs;
public sealed partial class LoggingTest {
    [Test]
    public void SubscriptionThresholdChangedDuringCalculationUsesNewValue() {
        var threshold = LogSeverity.Debug;
        var thresholdCalls = 0;
        var received = 0;
        var subscription = new MockLogSubscription {
            Receive = _ => received++,
            Threshold = _ => LogSeverity.Debug
        };
        subscription.Threshold = _ => {
            if (thresholdCalls++ == 0) {
                threshold = LogSeverity.Warn;
                subscription.ThresholdChanged();
                return LogSeverity.Debug;
            }
            return threshold;
        };

        Assert.That(Logging.Subscribe(subscription), Is.True);
        var log = Logging.For(typeof(LoggingTest));
        var enabled = log.Enabled(LogSeverity.Info);
        log.Info("should not be delivered");

        Assert.Multiple(() => {
            Assert.That(enabled, Is.False, "Enabled used the threshold returned before ThresholdChanged");
            Assert.That(received, Is.Zero, "the stale threshold allowed a log entry through");
        });
    }

    [Test]
    public void SubscriptionThresholdThatAlwaysChangesDoesNotHangLogging() {
        var raiseThresholdChanged = 1;
        var completed = new ManualResetEventSlim();
        var failure = default(Exception);
        var enabled = false;
        var received = 0;
        var subscription = new MockLogSubscription {
            Receive = _ => received++
        };
        subscription.Threshold = _ => {
            if (Interlocked.CompareExchange(ref raiseThresholdChanged, 0, 0) != 0) {
                subscription.ThresholdChanged();
                Thread.Sleep(1);
            }
            return LogSeverity.Info;
        };

        Assert.That(Logging.Subscribe(subscription), Is.True);
        var log = Logging.For(typeof(LoggingTest));
        var worker = new Thread(() => {
            try {
                enabled = log.Enabled(LogSeverity.Info);
                log.Info("message");
            }
            catch (Exception ex) {
                failure = ex;
            }
            finally {
                completed.Set();
            }
        });
        worker.IsBackground = true;
        worker.Start();

        var completedInTime = completed.Wait(TimeSpan.FromSeconds(2));
        Interlocked.Exchange(ref raiseThresholdChanged, 0);
        var workerStopped = completed.Wait(WaitTimeout);

        Assert.Multiple(() => {
            Assert.That(completedInTime, Is.True, "logging did not complete while thresholds kept changing");
            Assert.That(workerStopped, Is.True, "the bounded test worker did not stop");
            Assert.That(failure, Is.Null);
            Assert.That(enabled, Is.True);
            Assert.That(received, Is.EqualTo(1));
        });
    }

    [Test]
    public void UnsubscribeRemovesThresholdChangedHandler() {
        var subscription = new CountingLogSubscription();

        Assert.That(Logging.Subscribe(subscription), Is.True);
        Assert.That(subscription.Added, Is.EqualTo(1));
        Assert.That(subscription.HandlerCount, Is.EqualTo(1));

        Assert.That(Logging.Unsubscribe(subscription), Is.True);

        Assert.That(subscription.Removed, Is.EqualTo(1));
        Assert.That(subscription.HandlerCount, Is.Zero);
    }

    [Test]
    public void CompleteRemovesThresholdChangedHandlerExactlyOnce() {
        var subscription = new CountingLogSubscription();

        Assert.That(Logging.Subscribe(subscription), Is.True);
        Logging.Complete();

        Assert.That(subscription.Added, Is.EqualTo(1));
        Assert.That(subscription.Removed, Is.EqualTo(1));
        Assert.That(subscription.HandlerCount, Is.Zero);
    }

    private sealed class CountingLogSubscription : ILogSubscription {
        private event EventHandler OnThresholdChanged;

        public int Added { get; private set; }
        public int HandlerCount => OnThresholdChanged?.GetInvocationList().Length ?? 0;
        public int Removed { get; private set; }

        event EventHandler ILogSubscription.ThresholdChanged {
            add {
                Added++;
                OnThresholdChanged += value;
            }
            remove {
                Removed++;
                OnThresholdChanged -= value;
            }
        }

        LogSeverity ILogSubscription.Threshold(Type type) {
            return LogSeverity.Info;
        }

        void ILogSubscription.Receive(ILogEntry entry) {
        }
    }
}
