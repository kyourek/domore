using Domore.Logs.Mocks;
using Domore.Threading;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;

namespace Domore.Logs;
public sealed partial class LoggingTest {
    [Test]
    public void Review35BackgroundQueueDropsNewestWhenFullWithoutBlocking() {
        const int capacity = 1024;
        using var queue = new BackgroundQueue();
        using var actionStarted = new ManualResetEventSlim();
        using var releaseAction = new ManualResetEventSlim();
        using var producerReturned = new ManualResetEventSlim();
        var processed = new ConcurrentQueue<int>();
        var producer = default(Thread);

        queue.Add(() => {
            actionStarted.Set();
            releaseAction.Wait();
        });

        try {
            Assert.That(actionStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);
            for (var i = 0; i < capacity; i++) {
                var item = i;
                queue.Add(() => processed.Enqueue(item));
            }

            producer = new Thread(() => {
                queue.Add(() => processed.Enqueue(capacity));
                producerReturned.Set();
            }) {
                IsBackground = true
            };
            producer.Start();
            Assert.That(producerReturned.Wait(TimeSpan.FromSeconds(1)), Is.True);
            Assert.That(queue.DroppedCount, Is.EqualTo(1));
        }
        finally {
            releaseAction.Set();
            producer?.Join(TimeSpan.FromSeconds(5));
        }

        Assert.That(queue.Complete(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(processed, Is.EqualTo(Enumerable.Range(0, capacity)));
    }

    [Test]
    public void Review35DroppedQueueEntriesAreReportedAtCompletion() {
        const int capacity = 1024;
        BlockingLogService.Reset();
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var collection = new LogServiceCollection();
        try {
            var proxy = collection["blocking"];
            proxy.Type = typeof(BlockingLogService).AssemblyQualifiedName;
            proxy.Config.Default.Threshold = LogSeverity.Info;

            var entry = new LogEntry(
                typeof(LoggingTest),
                DateTime.UtcNow,
                LogSeverity.Info,
                ["message"]);
            collection.Send(entry);
            Assert.That(BlockingLogService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            for (var i = 0; i <= capacity; i++) {
                collection.Send(entry);
            }

            Console.SetError(output);
            BlockingLogService.Release.Set();
            Assert.That(collection.Complete(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(output.ToString(), Does.Contain("Dropped 1"));
        }
        finally {
            BlockingLogService.Release.Set();
            Console.SetError(originalError);
        }
    }

    [Test]
    public void Review35NotifyWritesToStandardError() {
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try {
            Console.SetOut(output);
            Console.SetError(error);
            Logging.Notify("diagnostic");
        }
        finally {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }

        Assert.Multiple(() => {
            Assert.That(error.ToString(), Does.Contain("diagnostic"));
            Assert.That(output.ToString(), Is.Empty);
        });
    }

    [Test]
    public void Review35LogNamesAndServiceThresholdsUseFullTypeName() {
        var firstType = typeof(FirstLogNamespace.DuplicateType);
        var secondType = typeof(SecondLogNamespace.DuplicateType);
        using var collection = new LogServiceCollection();
        collection["test"].Config[firstType.FullName].Threshold = LogSeverity.Info;

        var entry = new LogEntry(firstType, DateTime.UtcNow, LogSeverity.Info, ["message"]);
        var genericParameter = typeof(GenericLogType<>).GetGenericArguments()[0];
        var genericEntry = new LogEntry(genericParameter, DateTime.UtcNow, LogSeverity.Info, ["message"]);

        Assert.Multiple(() => {
            Assert.That(firstType.Name, Is.EqualTo(secondType.Name));
            Assert.That(firstType.FullName, Is.Not.EqualTo(secondType.FullName));
            Assert.That(entry.LogName, Is.EqualTo(firstType.FullName));
            Assert.That(collection.Send(LogSeverity.Info, firstType), Is.True);
            Assert.That(collection.Send(LogSeverity.Info, secondType), Is.False);
            Assert.That(genericParameter.FullName, Is.Null);
            Assert.That(genericEntry.LogName, Is.EqualTo(genericParameter.Name));
        });
    }

    [Test]
    public void Review35NullParamsArrayLogsAnEmptyLine() {
        string[] lines = null;
        Logging.EventThreshold = LogSeverity.Info;
        Logging.Event += (_, entry) => lines = entry.LogList.ToArray();

        Log.Info(null);

        Assert.That(lines, Is.EqualTo([""]));
    }

    [Test]
    public void Review35ManagerRejectsRegistrationsAfterComplete() {
        using var manager = new LogManager();
        var subscription = new MockLogSubscription();
        subscription.Threshold = _ => LogSeverity.Info;
        subscription.Receive = _ => { };
        Assert.That(manager.Subscribe(subscription), Is.True);
        Assert.That(manager.Complete(TimeSpan.FromSeconds(1)), Is.True);

        var eventRaised = false;
        var subscribedAfterComplete = manager.Subscribe(subscription);
        manager.LogEventThreshold = LogSeverity.Info;
        manager.LogEvent += (_, _) => eventRaised = true;
        manager.Log(LogSeverity.Info, typeof(LoggingTest), ["message"]);

        Assert.Multiple(() => {
            Assert.That(subscribedAfterComplete, Is.False);
            Assert.That(eventRaised, Is.False);
        });
    }

    private sealed class GenericLogType<T> {
    }
}
