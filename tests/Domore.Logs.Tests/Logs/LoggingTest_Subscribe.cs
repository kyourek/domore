using Domore.Logs.Mocks;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Domore.Logs; 
partial class LoggingTest {
    [Test]
    public void SubscribersAreSentLogEntryLogList() {
        var mock = new MockLogSubscription();
        var entry = default(ILogEntry);
        mock.Receive = e => entry = e;
        mock.Threshold = _ => LogSeverity.Info;
        Logging.Subscribe(mock);
        Log.Info("Here's the info.");
        Assert.That(entry.LogList, Is.EqualTo(["Here's the info."]));
        Logging.Complete();
    }

    [Test]
    public void SubscribersAreSentLogEntryLogSeverity() {
        var mock = new MockLogSubscription();
        var entry = default(ILogEntry);
        mock.Receive = e => entry = e;
        mock.Threshold = _ => LogSeverity.Info;
        Logging.Subscribe(mock);
        Log.Info("Here's the info.");
        Assert.That(entry.LogSeverity, Is.EqualTo(LogSeverity.Info));
        Logging.Complete();
    }

    [Test]
    public void SubscribersAreNotSentLogEntryIfThresholdIsNotMet() {
        var mock = new MockLogSubscription();
        var entry = default(ILogEntry);
        mock.Receive = e => entry = e;
        mock.Threshold = _ => LogSeverity.Warn;
        Logging.Subscribe(mock);
        Log.Info("Here's the info.");
        Assert.That(entry, Is.Null);
        Logging.Complete();
    }

    [Test]
    public void SubscribersAreSentLogEntryAfterThresholdIsChanged() {
        var mock = new MockLogSubscription();
        var entries = new List<string>();
        mock.Receive = e => entries.AddRange(e.LogList);
        mock.Threshold = _ => LogSeverity.Warn;
        Logging.Subscribe(mock);
        Log.Info("Here's the info.");
        mock.Threshold = _ => LogSeverity.Info;
        mock.ThresholdChanged();
        Log.Info("Here's some more.");
        Assert.That(entries, Is.EqualTo(["Here's some more."]));
        Logging.Complete();
    }

    [Test]
    public void SubscribersAreNotSentLogEntryBeforeThresholdIsChanged() {
        var mock = new MockLogSubscription();
        var entries = new List<string>();
        mock.Receive = e => entries.AddRange(e.LogList);
        mock.Threshold = _ => LogSeverity.Warn;
        Logging.Subscribe(mock);
        Log.Info("Here's the info.");
        mock.Threshold = _ => LogSeverity.Info;
        Log.Info("Here's some more.");
        Assert.That(entries, Is.Empty);
        Logging.Complete();
    }

    [Test]
    public void TrueIsReturnedWhenSubscriberIsAdded() {
        var mock = new MockLogSubscription();
        var actual = Logging.Subscribe(mock);
        Assert.That(actual, Is.True);
    }

    [Test]
    public void FalseIsReturnedWhenSubscriberAlreadyExists() {
        var mock = new MockLogSubscription();
        Logging.Subscribe(mock);
        var actual = Logging.Subscribe(mock);
        Assert.That(actual, Is.False);
    }

    [Test]
    public void ThresholdIsSatisfiedBySubscriber() {
        var mock = new MockLogSubscription();
        mock.Threshold = _ => LogSeverity.Debug;
        Logging.Subscribe(mock);
        var actual = Log.Debug();
        Assert.That(actual, Is.True);
    }

    [Test]
    public void ThresholdIsNotSatisfiedAfterSubscriberIsRemoved() {
        var mock = new MockLogSubscription();
        mock.Threshold = _ => LogSeverity.Debug;
        Logging.Subscribe(mock);
        Log.Debug();
        Logging.Unsubscribe(mock);
        var actual = Log.Debug();
        Assert.That(actual, Is.False);
    }

    [Test]
    public void TrueIsReturnedWhenSubscriberIsRemoved() {
        var mock = new MockLogSubscription();
        Logging.Subscribe(mock);
        var actual = Logging.Unsubscribe(mock);
        Assert.That(actual, Is.True);
    }

    [Test]
    public void FalseIsReturnedWhenSubscriberIsNotRemoved() {
        var mock = new MockLogSubscription();
        Logging.Subscribe(mock);
        Logging.Unsubscribe(mock);
        var actual = Logging.Unsubscribe(mock);
        Assert.That(actual, Is.False);
    }

    [Test]
    public void ExceptionIsCaughtWhenThrownFromThreshold() {
        var mock = new MockLogSubscription();
        var ex = default(Exception);
        mock.Threshold = _ => throw (ex = new Exception());
        Logging.Subscribe(mock);
        Log.Critical();
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void ExceptionIsCaughtWhenThrownFromReceive() {
        var mock = new MockLogSubscription();
        var ex = default(Exception);
        mock.Threshold = _ => LogSeverity.Debug;
        mock.Receive = _ => throw (ex = new Exception());
        Logging.Subscribe(mock);
        Log.Critical("message");
        Assert.That(ex, Is.Not.Null);
    }
}
