using NUnit.Framework;
using System.Collections.Generic;

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

        private class Subject1 : Notifier {
            public string Foo {
                get => _Foo.Value;
                set => Change(_Foo, value);
            }
            protected readonly Notified<string> _Foo = new(nameof(Foo));
        }

        [Test]
        public void NotifiedPropertyHasDefaultValue() {
            var subject = new Subject1();
            Assert.That(subject.Foo, Is.EqualTo(default(string)));
        }

        [Test]
        public void NotifiedPropertyRaisesPropertyChanged() {
            var subject = new Subject1();
            var entered = 0;
            subject.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(subject.Foo)) {
                    entered++;
                }
            };
            subject.Foo = "bar";
            Assert.That(entered, Is.EqualTo(1));
        }

        [Test]
        public void NotifiedPropertyDoesNotRaisePropertyChanged() {
            var subject = new Subject1();
            var entered = 0;
            subject.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(subject.Foo)) {
                    entered++;
                }
            };
            subject.Foo = null;
            Assert.That(entered, Is.EqualTo(0));
        }

        [Test]
        public void NotifiedPropertyReturnsNewValue() {
            var subject = new Subject1();
            subject.Foo = "bar";
            Assert.That(subject.Foo, Is.EqualTo("bar"));
        }

        private class Subject2 : Subject1 {
            public int Bar {
                get => _Bar.Value;
                set => Change(_Bar, value, _Foo);
            }
            private readonly Notified<int> _Bar = new(nameof(Bar));
        }

        [Test]
        public void NotifiedPropertyHasDefaultValueOfInt32() {
            Assert.That(new Subject2().Bar, Is.EqualTo(default(int)));
        }

        [Test]
        public void NotifiedPropertyRaisesPropertyChangedForDependents() {
            var events = new List<string>();
            var subject = new Subject2();
            subject.PropertyChanged += (s, e) => {
                events.Add(e.PropertyName);
            };
            subject.Bar = 1;
            Assert.That(events, Is.EqualTo(new[] { "Bar", "Foo" }));
        }

        [Test]
        public void NotifiedPropertyDoesNotRaisesPropertyChangedIfValueDoesNotChange() {
            var events = new List<string>();
            var subject = new Subject2();
            subject.PropertyChanged += (s, e) => {
                events.Add(e.PropertyName);
            };
            subject.Bar = 0;
            Assert.That(events, Is.EqualTo(new string[] { }));
        }
    }
}
