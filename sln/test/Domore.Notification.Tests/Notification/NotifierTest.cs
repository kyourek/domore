using NUnit.Framework;

namespace Domore.Notification {
    [TestFixture]
    public partial class NotifierTest {
        private Notifier Subject {
            get => _Subject ?? (_Subject = new Notifier());
            set => _Subject = value;
        }
        private Notifier _Subject;

        [SetUp]
        public void SetUp() {
            Subject = null;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NotifyState_EnablesOrDisablesNotification(bool value) {
            var field = 0;
            var notified = false;
            Subject.NotifyState = value;
            Subject.PropertyChanged += (_, _) => {
                notified = true;
            };
            Subject.Change(ref field, 1, "PropName");
            Assert.That(notified, Is.EqualTo(value));
        }
    }
}
