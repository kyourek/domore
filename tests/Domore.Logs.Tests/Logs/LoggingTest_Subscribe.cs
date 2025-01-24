using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    partial class LoggingTest {
        private sealed class MockLogSubscription : ILogSubscription {
            private event EventHandler OnThresholdChanged;

            public Func<Type, LogSeverity> Threshold { get; set; }
            public Action<ILogEntry> Receive { get; set; }

            public void ThresholdChanged() {
                OnThresholdChanged?.Invoke(this, EventArgs.Empty);
            }

            event EventHandler ILogSubscription.ThresholdChanged {
                add { OnThresholdChanged += value; }
                remove { OnThresholdChanged -= value; }
            }

            void ILogSubscription.Receive(ILogEntry entry) {
                Receive(entry);
            }

            LogSeverity ILogSubscription.Threshold(Type type) {
                return Threshold(type);
            }
        }

        [Test]
        public void SubscribersAreSentLogEntryLogList() {
            var mock = new MockLogSubscription();
            var entry = default(ILogEntry);
            mock.Receive = e => entry = e;
            mock.Threshold = _ => LogSeverity.Info;
            Logging.Subscribe(mock);
            Log.Info("Here's the info.");
            CollectionAssert.AreEqual(new[] { "Here's the info." }, entry.LogList);
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
            CollectionAssert.AreEqual(new[] { "Here's some more." }, entries);
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
            CollectionAssert.IsEmpty(entries);
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

        [Test]
        public void SubscribersReceiveEntriesWhileFileLogging() {
            ConfigFile();
            var mock = new MockLogSubscription();
            var entries = new List<ILogEntry>();
            mock.Threshold = _ => LogSeverity.Info;
            mock.Receive = entries.Add;
            Logging.Subscribe(mock);
            Log.Info("here's some data");
            Logging.Complete();
            var actual = entries.SelectMany(e => e.LogList).ToList();
            var expected = new[] { "here's some data" };
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
