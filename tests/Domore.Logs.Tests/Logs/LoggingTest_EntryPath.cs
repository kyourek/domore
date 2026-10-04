using Domore.Logs.Mocks;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Logs;

public sealed partial class LoggingTest {
    [Test]
    public void EntryPathLogCallsDoNotThrowWhileCompleteRuns() {
        var log = Logging.For(typeof(LoggingTest));
        var eventCalls = 0;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => Interlocked.Increment(ref eventCalls);
        log.Info("warmup");
        Assert.That(eventCalls, Is.EqualTo(1));

        var exceptions = new ConcurrentQueue<Exception>();
        using var start = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var workers = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => {
                start.Wait();
                while (cancellation.IsCancellationRequested == false) {
                    try {
                        log.Info("message");
                    }
                    catch (Exception ex) {
                        exceptions.Enqueue(ex);
                    }
                    try {
                        log.Enabled(LogSeverity.Info);
                    }
                    catch (Exception ex) {
                        exceptions.Enqueue(ex);
                    }
                    try {
                        log.Info();
                    }
                    catch (Exception ex) {
                        exceptions.Enqueue(ex);
                    }
                }
            }))
            .ToArray();
        var completer = Task.Run(() => {
            start.Wait();
            while (cancellation.IsCancellationRequested == false) {
                Logging.Complete();
            }
        });
        start.Set();

        var tasks = workers.Concat(new[] { completer }).ToArray();
        Assert.That(Task.WaitAll(tasks, TimeSpan.FromSeconds(15)), Is.True, "The stress test exceeded its time limit.");
        Assert.That(exceptions, Is.Empty);
    }

    [Test]
    public void EntryPathContinuesAfterFailingEventHandler() {
        var secondHandlerCalls = 0;
        var subscriberCalls = 0;
        var subscription = new MockLogSubscription {
            Threshold = _ => LogSeverity.Info,
            Receive = _ => subscriberCalls++
        };
        Logging.Subscribe(subscription);
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => throw new InvalidOperationException("Event handler failure.");
        Logging.Event += (_, __) => secondHandlerCalls++;

        Exception exception = null;
        try {
            Logging.For(typeof(LoggingTest)).Info("message");
        }
        catch (Exception ex) {
            exception = ex;
        }

        Assert.Multiple(() => {
            Assert.That(exception, Is.Null);
            Assert.That(secondHandlerCalls, Is.EqualTo(1));
            Assert.That(subscriberCalls, Is.EqualTo(1));
        });
    }

    [TestCase(LogSeverity.Debug)]
    [TestCase(LogSeverity.Info)]
    [TestCase(LogSeverity.Warn)]
    [TestCase(LogSeverity.Error)]
    [TestCase(LogSeverity.Critical)]
    public void EntryPathNullLoggerDoesNotThrow(LogSeverity severity) {
        var subscription = new MockLogSubscription {
            Threshold = _ => LogSeverity.Debug,
            Receive = _ => { }
        };
        Logging.Subscribe(subscription);

        var log = Logging.For(null);
        Exception exception = null;
        try {
            switch (severity) {
                case LogSeverity.Debug:
                    log.Debug("message");
                    break;
                case LogSeverity.Info:
                    log.Info("message");
                    break;
                case LogSeverity.Warn:
                    log.Warn("message");
                    break;
                case LogSeverity.Error:
                    log.Error("message");
                    break;
                case LogSeverity.Critical:
                    log.Critical("message");
                    break;
            }
        }
        catch (Exception ex) {
            exception = ex;
        }

        Assert.That(exception, Is.Null);
    }

    [Test]
    public void EntryPathEventHandlerDoesNotReceiveReentrantEntries() {
        var log = Logging.For(typeof(LoggingTest));
        var calls = 0;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, __) => {
            calls++;
            if (calls < 20) {
                log.Info("nested");
            }
        };

        log.Info("outer");

        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void EntryPathSubscriptionDoesNotReceiveReentrantEntries() {
        var log = Logging.For(typeof(LoggingTest));
        var calls = 0;
        var subscription = new MockLogSubscription {
            Threshold = _ => LogSeverity.Info,
            Receive = _ => {
                calls++;
                if (calls < 20) {
                    log.Info("nested");
                }
            }
        };
        Logging.Subscribe(subscription);

        log.Info("outer");

        Assert.That(calls, Is.EqualTo(1));
    }
}
