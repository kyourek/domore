using Domore.Logs.Mocks;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Domore.Logs;

partial class LoggingTest {
    private static bool MessageMutationIsBlocked(IEnumerable<string> messages) {
        try {
            ((IList<string>)messages)[0] = "changed";
            return false;
        }
        catch (NotSupportedException) {
            return true;
        }
    }

    [Test]
    public void ObserverMutationCannotChangeLaterEventsSubscriptionsOrServices() {
        HealthyCompleteLogService.Reset();
        Config = $@"
            log[healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
            log[healthy].config.default.severity = info
        ";
        Logging.EventThreshold = LogSeverity.Info;
        IEnumerable<string> eventMessages = null;
        var eventMutationBlocked = false;
        var subscriptionMutationBlocked = false;
        string[] laterEventMessages = null;
        Logging.Event += (_, e) => {
            eventMessages = e.LogList;
            eventMutationBlocked = MessageMutationIsBlocked(e.LogList);
        };
        Logging.Event += (_, e) => laterEventMessages = e.LogList.ToArray();
        var mutatingSubscription = new MockLogSubscription {
            Threshold = _ => LogSeverity.Info,
            Receive = entry => subscriptionMutationBlocked = MessageMutationIsBlocked(entry.LogList)
        };
        var laterSubscription = new CompletionWindowSubscription();
        Logging.Subscribe(mutatingSubscription);
        Logging.Subscribe(laterSubscription);

        Log.Info("original", "second");
        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(eventMutationBlocked, Is.True);
            Assert.That(subscriptionMutationBlocked, Is.True);
            Assert.That(eventMessages, Is.EqualTo(["original", "second"]));
            Assert.That(laterEventMessages, Is.EqualTo(["original", "second"]));
            Assert.That(laterSubscription.Entries.ToArray(), Is.EqualTo(["original", "second"]));
            Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["original" + Environment.NewLine + "second"]));
        });
    }

    private sealed class GatedEntryIsolationService : ILogService {
        public static ManualResetEventSlim Entered { get; } = new();
        public static ManualResetEventSlim Release { get; } = new();
        public static ConcurrentQueue<string> Entries { get; } = new();

        public static void Reset() {
            Entered.Reset();
            Release.Reset();
            while (Entries.TryDequeue(out _)) { }
        }

        public void Log(string name, string data, LogSeverity severity) {
            if (data == "block worker") {
                Entered.Set();
                if (Release.Wait(TimeSpan.FromSeconds(5)) == false) {
                    throw new TimeoutException("The test did not release the service worker.");
                }
            }
            Entries.Enqueue(data);
        }

        public void Complete() { }
    }

    [Test]
    public void RetainedMessageListCannotChangePendingServiceDelivery() {
        GatedEntryIsolationService.Reset();
        Config = $@"
            log[gated].type = {typeof(GatedEntryIsolationService).AssemblyQualifiedName}
            log[gated].config.default.severity = info
        ";
        IEnumerable<string> retained = null;
        var subscription = new MockLogSubscription {
            Threshold = _ => LogSeverity.Info,
            Receive = entry => retained = entry.LogList
        };
        Logging.Subscribe(subscription);
        var mutationBlocked = false;
        try {
            Log.Info("block worker");
            Assert.That(GatedEntryIsolationService.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Log.Info("original");
            mutationBlocked = MessageMutationIsBlocked(retained);
        }
        finally {
            GatedEntryIsolationService.Release.Set();
            Logging.Complete();
        }

        Assert.Multiple(() => {
            Assert.That(mutationBlocked, Is.True);
            Assert.That(retained, Is.EqualTo(["original"]));
            Assert.That(GatedEntryIsolationService.Entries.ToArray(), Is.EqualTo(["block worker", "original"]));
        });
    }

    private sealed class EntryIsolationFormattedValue { }

    [Test]
    public void CustomFormatterRetainingItsArrayCannotChangeAnExistingEntry() {
        HealthyCompleteLogService.Reset();
        Config = $@"
            log[healthy].type = {typeof(HealthyCompleteLogService).AssemblyQualifiedName}
            log[healthy].config.default.severity = info
        ";
        var formatterMessages = new[] { "original" };
        Logging.Format(typeof(EntryIsolationFormattedValue), _ => formatterMessages);
        var subscription = new MockLogSubscription {
            Threshold = _ => LogSeverity.Info
        };
        IEnumerable<string> retained = null;
        subscription.Receive = entry => retained = entry.LogList;
        Logging.Subscribe(subscription);

        Log.Info(new EntryIsolationFormattedValue());
        formatterMessages[0] = "changed";
        Logging.Complete();

        Assert.Multiple(() => {
            Assert.That(retained, Is.EqualTo(["original"]));
            Assert.That(HealthyCompleteLogService.Entries.ToArray(), Is.EqualTo(["original"]));
        });
    }
}
