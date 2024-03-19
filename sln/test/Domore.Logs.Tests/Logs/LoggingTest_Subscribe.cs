using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Domore.Logs {
    partial class LoggingTest {
        private sealed class TestLogSubscription : ILogSubscription {
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
            var mock = new TestLogSubscription();
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
            var mock = new TestLogSubscription();
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
            var mock = new TestLogSubscription();
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
            var mock = new TestLogSubscription();
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
            var mock = new TestLogSubscription();
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
            var mock = new TestLogSubscription();
            var actual = Logging.Subscribe(mock);
            Assert.That(actual, Is.True);
        }

        [Test]
        public void FalseIsReturnedWhenSubscriberAlreadyExists() {
            var mock = new TestLogSubscription();
            Logging.Subscribe(mock);
            var actual = Logging.Subscribe(mock);
            Assert.That(actual, Is.False);
        }

        [Test]
        public void ThresholdIsSatisfiedBySubscriber() {
            var mock = new TestLogSubscription();
            mock.Threshold = _ => LogSeverity.Debug;
            Logging.Subscribe(mock);
            var actual = Log.Debug();
            Assert.That(actual, Is.True);
        }

        [Test]
        public void ThresholdIsNotSatisfiedAfterSubscriberIsRemoved() {
            var mock = new TestLogSubscription();
            mock.Threshold = _ => LogSeverity.Debug;
            Logging.Subscribe(mock);
            Log.Debug();
            Logging.Unsubscribe(mock);
            var actual = Log.Debug();
            Assert.That(actual, Is.False);
        }

        [Test]
        public void TrueIsReturnedWhenSubscriberIsRemoved() {
            var mock = new TestLogSubscription();
            Logging.Subscribe(mock);
            var actual = Logging.Unsubscribe(mock);
            Assert.That(actual, Is.True);
        }

        [Test]
        public void FalseIsReturnedWhenSubscriberIsNotRemoved() {
            var mock = new TestLogSubscription();
            Logging.Subscribe(mock);
            Logging.Unsubscribe(mock);
            var actual = Logging.Unsubscribe(mock);
            Assert.That(actual, Is.False);
        }
    }
}
