using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Domore.Notification {
    public partial class NotifierTest {
        [Test]
        public void Change_byte_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, "expected");
        }

        [Test]
        public void Change_byte_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (byte)0;
            Subject.Change(ref field, (byte)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_byte_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (byte)0;
            Subject.Change(ref field, (byte)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_byte_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (byte)0;
            Subject.Change(ref field, (byte)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_byte_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (byte)0;
            Subject.Change(ref field, (byte)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_byte_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (byte)0;
            Subject.Change(ref field, (byte)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_byte_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (byte)0;
            Subject.Change(ref field, (byte)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_byte_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (byte)0;
            Subject.Change(ref field, (byte)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_byte_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (byte)0;
            Subject.Change(ref field, (byte)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_byte_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (byte)0;
            Subject.Change(ref field, (byte)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_byte_1_ChangesValue() {
            var field = (byte)0;
            Subject.Change(ref field, (byte)1, "expected");
            Assert.That(field, Is.EqualTo((byte)1));
        }

        [Test]
        public void Change_byte_1_ReturnsFalse() {
            var field = (byte)0;
            var actual = Subject.Change(ref field, (byte)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_byte_1_ReturnsTrue() {
            var field = (byte)0;
            var actual = Subject.Change(ref field, (byte)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_byte_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_byte_"] = false;
            var field = (byte)0;
            var actual = subject.Change(ref field, (byte)1, "P_byte_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_byte_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_byte_"] = true;
            var field = (byte)0;
            var actual = subject.Change(ref field, (byte)1, "P_byte_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_byten_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, "expected");
        }

        [Test]
        public void Change_byten_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_byten_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_byten_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_byten_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_byten_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_byten_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_byten_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_byten_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_byten_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (byte?)0;
            Subject.Change(ref field, (byte?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_byten_1_ChangesValue() {
            var field = (byte?)0;
            Subject.Change(ref field, (byte?)1, "expected");
            Assert.That(field, Is.EqualTo((byte?)1));
        }

        [Test]
        public void Change_byten_1_ReturnsFalse() {
            var field = (byte?)0;
            var actual = Subject.Change(ref field, (byte?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_byten_1_ReturnsTrue() {
            var field = (byte?)0;
            var actual = Subject.Change(ref field, (byte?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_byten_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_byten_"] = false;
            var field = (byte?)0;
            var actual = subject.Change(ref field, (byte?)1, "P_byten_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_byten_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_byten_"] = true;
            var field = (byte?)0;
            var actual = subject.Change(ref field, (byte?)1, "P_byten_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_sbyte_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, "expected");
        }

        [Test]
        public void Change_sbyte_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_sbyte_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_sbyte_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_sbyte_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_sbyte_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_sbyte_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_sbyte_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_sbyte_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_sbyte_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_sbyte_1_ChangesValue() {
            var field = (sbyte)0;
            Subject.Change(ref field, (sbyte)1, "expected");
            Assert.That(field, Is.EqualTo((sbyte)1));
        }

        [Test]
        public void Change_sbyte_1_ReturnsFalse() {
            var field = (sbyte)0;
            var actual = Subject.Change(ref field, (sbyte)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_sbyte_1_ReturnsTrue() {
            var field = (sbyte)0;
            var actual = Subject.Change(ref field, (sbyte)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_sbyte_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_sbyte_"] = false;
            var field = (sbyte)0;
            var actual = subject.Change(ref field, (sbyte)1, "P_sbyte_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_sbyte_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_sbyte_"] = true;
            var field = (sbyte)0;
            var actual = subject.Change(ref field, (sbyte)1, "P_sbyte_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_sbyten_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, "expected");
        }

        [Test]
        public void Change_sbyten_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_sbyten_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_sbyten_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_sbyten_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_sbyten_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_sbyten_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_sbyten_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_sbyten_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_sbyten_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_sbyten_1_ChangesValue() {
            var field = (sbyte?)0;
            Subject.Change(ref field, (sbyte?)1, "expected");
            Assert.That(field, Is.EqualTo((sbyte?)1));
        }

        [Test]
        public void Change_sbyten_1_ReturnsFalse() {
            var field = (sbyte?)0;
            var actual = Subject.Change(ref field, (sbyte?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_sbyten_1_ReturnsTrue() {
            var field = (sbyte?)0;
            var actual = Subject.Change(ref field, (sbyte?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_sbyten_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_sbyten_"] = false;
            var field = (sbyte?)0;
            var actual = subject.Change(ref field, (sbyte?)1, "P_sbyten_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_sbyten_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_sbyten_"] = true;
            var field = (sbyte?)0;
            var actual = subject.Change(ref field, (sbyte?)1, "P_sbyten_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_char_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (char)0;
            Subject.Change(ref field, (char)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_char_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (char)0;
            Subject.Change(ref field, (char)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_char_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (char)0;
            Subject.Change(ref field, (char)1, "expected");
        }

        [Test]
        public void Change_char_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (char)0;
            Subject.Change(ref field, (char)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_char_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (char)0;
            Subject.Change(ref field, (char)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_char_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (char)0;
            Subject.Change(ref field, (char)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_char_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (char)0;
            Subject.Change(ref field, (char)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_char_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (char)0;
            Subject.Change(ref field, (char)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_char_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (char)0;
            Subject.Change(ref field, (char)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_char_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (char)0;
            Subject.Change(ref field, (char)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_char_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (char)0;
            Subject.Change(ref field, (char)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_char_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (char)0;
            Subject.Change(ref field, (char)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_char_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (char)0;
            Subject.Change(ref field, (char)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_char_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (char)0;
            Subject.Change(ref field, (char)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_char_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (char)0;
            Subject.Change(ref field, (char)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_char_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (char)0;
            Subject.Change(ref field, (char)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_char_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (char)0;
            Subject.Change(ref field, (char)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_char_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (char)0;
            Subject.Change(ref field, (char)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_char_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (char)0;
            Subject.Change(ref field, (char)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_char_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (char)0;
            Subject.Change(ref field, (char)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_char_1_ChangesValue() {
            var field = (char)0;
            Subject.Change(ref field, (char)1, "expected");
            Assert.That(field, Is.EqualTo((char)1));
        }

        [Test]
        public void Change_char_1_ReturnsFalse() {
            var field = (char)0;
            var actual = Subject.Change(ref field, (char)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_char_1_ReturnsTrue() {
            var field = (char)0;
            var actual = Subject.Change(ref field, (char)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_char_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_char_"] = false;
            var field = (char)0;
            var actual = subject.Change(ref field, (char)1, "P_char_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_char_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_char_"] = true;
            var field = (char)0;
            var actual = subject.Change(ref field, (char)1, "P_char_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_charn_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, "expected");
        }

        [Test]
        public void Change_charn_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (char?)0;
            Subject.Change(ref field, (char?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_charn_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (char?)0;
            Subject.Change(ref field, (char?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_charn_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (char?)0;
            Subject.Change(ref field, (char?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_charn_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (char?)0;
            Subject.Change(ref field, (char?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_charn_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (char?)0;
            Subject.Change(ref field, (char?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_charn_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (char?)0;
            Subject.Change(ref field, (char?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_charn_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (char?)0;
            Subject.Change(ref field, (char?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_charn_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (char?)0;
            Subject.Change(ref field, (char?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_charn_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (char?)0;
            Subject.Change(ref field, (char?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_charn_1_ChangesValue() {
            var field = (char?)0;
            Subject.Change(ref field, (char?)1, "expected");
            Assert.That(field, Is.EqualTo((char?)1));
        }

        [Test]
        public void Change_charn_1_ReturnsFalse() {
            var field = (char?)0;
            var actual = Subject.Change(ref field, (char?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_charn_1_ReturnsTrue() {
            var field = (char?)0;
            var actual = Subject.Change(ref field, (char?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_charn_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_charn_"] = false;
            var field = (char?)0;
            var actual = subject.Change(ref field, (char?)1, "P_charn_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_charn_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_charn_"] = true;
            var field = (char?)0;
            var actual = subject.Change(ref field, (char?)1, "P_charn_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_decimal_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, "expected");
        }

        [Test]
        public void Change_decimal_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_decimal_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_decimal_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_decimal_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_decimal_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_decimal_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_decimal_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_decimal_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_decimal_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (decimal)0;
            Subject.Change(ref field, (decimal)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_decimal_1_ChangesValue() {
            var field = (decimal)0;
            Subject.Change(ref field, (decimal)1, "expected");
            Assert.That(field, Is.EqualTo((decimal)1));
        }

        [Test]
        public void Change_decimal_1_ReturnsFalse() {
            var field = (decimal)0;
            var actual = Subject.Change(ref field, (decimal)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_decimal_1_ReturnsTrue() {
            var field = (decimal)0;
            var actual = Subject.Change(ref field, (decimal)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_decimal_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_decimal_"] = false;
            var field = (decimal)0;
            var actual = subject.Change(ref field, (decimal)1, "P_decimal_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_decimal_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_decimal_"] = true;
            var field = (decimal)0;
            var actual = subject.Change(ref field, (decimal)1, "P_decimal_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_decimaln_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, "expected");
        }

        [Test]
        public void Change_decimaln_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_decimaln_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_decimaln_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_decimaln_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_decimaln_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_decimaln_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_decimaln_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_decimaln_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_decimaln_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_decimaln_1_ChangesValue() {
            var field = (decimal?)0;
            Subject.Change(ref field, (decimal?)1, "expected");
            Assert.That(field, Is.EqualTo((decimal?)1));
        }

        [Test]
        public void Change_decimaln_1_ReturnsFalse() {
            var field = (decimal?)0;
            var actual = Subject.Change(ref field, (decimal?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_decimaln_1_ReturnsTrue() {
            var field = (decimal?)0;
            var actual = Subject.Change(ref field, (decimal?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_decimaln_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_decimaln_"] = false;
            var field = (decimal?)0;
            var actual = subject.Change(ref field, (decimal?)1, "P_decimaln_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_decimaln_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_decimaln_"] = true;
            var field = (decimal?)0;
            var actual = subject.Change(ref field, (decimal?)1, "P_decimaln_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_double_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (double)0;
            Subject.Change(ref field, (double)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_double_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (double)0;
            Subject.Change(ref field, (double)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_double_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (double)0;
            Subject.Change(ref field, (double)1, "expected");
        }

        [Test]
        public void Change_double_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (double)0;
            Subject.Change(ref field, (double)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_double_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (double)0;
            Subject.Change(ref field, (double)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_double_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (double)0;
            Subject.Change(ref field, (double)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_double_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (double)0;
            Subject.Change(ref field, (double)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_double_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (double)0;
            Subject.Change(ref field, (double)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_double_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (double)0;
            Subject.Change(ref field, (double)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_double_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (double)0;
            Subject.Change(ref field, (double)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_double_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (double)0;
            Subject.Change(ref field, (double)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_double_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (double)0;
            Subject.Change(ref field, (double)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_double_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (double)0;
            Subject.Change(ref field, (double)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_double_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (double)0;
            Subject.Change(ref field, (double)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_double_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (double)0;
            Subject.Change(ref field, (double)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_double_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (double)0;
            Subject.Change(ref field, (double)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_double_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (double)0;
            Subject.Change(ref field, (double)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_double_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (double)0;
            Subject.Change(ref field, (double)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_double_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (double)0;
            Subject.Change(ref field, (double)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_double_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (double)0;
            Subject.Change(ref field, (double)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_double_1_ChangesValue() {
            var field = (double)0;
            Subject.Change(ref field, (double)1, "expected");
            Assert.That(field, Is.EqualTo((double)1));
        }

        [Test]
        public void Change_double_1_ReturnsFalse() {
            var field = (double)0;
            var actual = Subject.Change(ref field, (double)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_double_1_ReturnsTrue() {
            var field = (double)0;
            var actual = Subject.Change(ref field, (double)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_double_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_double_"] = false;
            var field = (double)0;
            var actual = subject.Change(ref field, (double)1, "P_double_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_double_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_double_"] = true;
            var field = (double)0;
            var actual = subject.Change(ref field, (double)1, "P_double_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_doublen_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, "expected");
        }

        [Test]
        public void Change_doublen_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (double?)0;
            Subject.Change(ref field, (double?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_doublen_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (double?)0;
            Subject.Change(ref field, (double?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_doublen_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (double?)0;
            Subject.Change(ref field, (double?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_doublen_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (double?)0;
            Subject.Change(ref field, (double?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_doublen_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (double?)0;
            Subject.Change(ref field, (double?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_doublen_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (double?)0;
            Subject.Change(ref field, (double?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_doublen_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (double?)0;
            Subject.Change(ref field, (double?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_doublen_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (double?)0;
            Subject.Change(ref field, (double?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_doublen_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (double?)0;
            Subject.Change(ref field, (double?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_doublen_1_ChangesValue() {
            var field = (double?)0;
            Subject.Change(ref field, (double?)1, "expected");
            Assert.That(field, Is.EqualTo((double?)1));
        }

        [Test]
        public void Change_doublen_1_ReturnsFalse() {
            var field = (double?)0;
            var actual = Subject.Change(ref field, (double?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_doublen_1_ReturnsTrue() {
            var field = (double?)0;
            var actual = Subject.Change(ref field, (double?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_doublen_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_doublen_"] = false;
            var field = (double?)0;
            var actual = subject.Change(ref field, (double?)1, "P_doublen_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_doublen_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_doublen_"] = true;
            var field = (double?)0;
            var actual = subject.Change(ref field, (double?)1, "P_doublen_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_float_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (float)0;
            Subject.Change(ref field, (float)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_float_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (float)0;
            Subject.Change(ref field, (float)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_float_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (float)0;
            Subject.Change(ref field, (float)1, "expected");
        }

        [Test]
        public void Change_float_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (float)0;
            Subject.Change(ref field, (float)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_float_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (float)0;
            Subject.Change(ref field, (float)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_float_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (float)0;
            Subject.Change(ref field, (float)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_float_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (float)0;
            Subject.Change(ref field, (float)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_float_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (float)0;
            Subject.Change(ref field, (float)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_float_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (float)0;
            Subject.Change(ref field, (float)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_float_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (float)0;
            Subject.Change(ref field, (float)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_float_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (float)0;
            Subject.Change(ref field, (float)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_float_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (float)0;
            Subject.Change(ref field, (float)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_float_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (float)0;
            Subject.Change(ref field, (float)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_float_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (float)0;
            Subject.Change(ref field, (float)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_float_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (float)0;
            Subject.Change(ref field, (float)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_float_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (float)0;
            Subject.Change(ref field, (float)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_float_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (float)0;
            Subject.Change(ref field, (float)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_float_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (float)0;
            Subject.Change(ref field, (float)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_float_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (float)0;
            Subject.Change(ref field, (float)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_float_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (float)0;
            Subject.Change(ref field, (float)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_float_1_ChangesValue() {
            var field = (float)0;
            Subject.Change(ref field, (float)1, "expected");
            Assert.That(field, Is.EqualTo((float)1));
        }

        [Test]
        public void Change_float_1_ReturnsFalse() {
            var field = (float)0;
            var actual = Subject.Change(ref field, (float)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_float_1_ReturnsTrue() {
            var field = (float)0;
            var actual = Subject.Change(ref field, (float)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_float_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_float_"] = false;
            var field = (float)0;
            var actual = subject.Change(ref field, (float)1, "P_float_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_float_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_float_"] = true;
            var field = (float)0;
            var actual = subject.Change(ref field, (float)1, "P_float_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_floatn_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, "expected");
        }

        [Test]
        public void Change_floatn_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (float?)0;
            Subject.Change(ref field, (float?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_floatn_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (float?)0;
            Subject.Change(ref field, (float?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_floatn_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (float?)0;
            Subject.Change(ref field, (float?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_floatn_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (float?)0;
            Subject.Change(ref field, (float?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_floatn_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (float?)0;
            Subject.Change(ref field, (float?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_floatn_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (float?)0;
            Subject.Change(ref field, (float?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_floatn_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (float?)0;
            Subject.Change(ref field, (float?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_floatn_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (float?)0;
            Subject.Change(ref field, (float?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_floatn_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (float?)0;
            Subject.Change(ref field, (float?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_floatn_1_ChangesValue() {
            var field = (float?)0;
            Subject.Change(ref field, (float?)1, "expected");
            Assert.That(field, Is.EqualTo((float?)1));
        }

        [Test]
        public void Change_floatn_1_ReturnsFalse() {
            var field = (float?)0;
            var actual = Subject.Change(ref field, (float?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_floatn_1_ReturnsTrue() {
            var field = (float?)0;
            var actual = Subject.Change(ref field, (float?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_floatn_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_floatn_"] = false;
            var field = (float?)0;
            var actual = subject.Change(ref field, (float?)1, "P_floatn_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_floatn_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_floatn_"] = true;
            var field = (float?)0;
            var actual = subject.Change(ref field, (float?)1, "P_floatn_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_int_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (int)0;
            Subject.Change(ref field, (int)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_int_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (int)0;
            Subject.Change(ref field, (int)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_int_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (int)0;
            Subject.Change(ref field, (int)1, "expected");
        }

        [Test]
        public void Change_int_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (int)0;
            Subject.Change(ref field, (int)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_int_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (int)0;
            Subject.Change(ref field, (int)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_int_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (int)0;
            Subject.Change(ref field, (int)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_int_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (int)0;
            Subject.Change(ref field, (int)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_int_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (int)0;
            Subject.Change(ref field, (int)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_int_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (int)0;
            Subject.Change(ref field, (int)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_int_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (int)0;
            Subject.Change(ref field, (int)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_int_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (int)0;
            Subject.Change(ref field, (int)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_int_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (int)0;
            Subject.Change(ref field, (int)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_int_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (int)0;
            Subject.Change(ref field, (int)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_int_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (int)0;
            Subject.Change(ref field, (int)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_int_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (int)0;
            Subject.Change(ref field, (int)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_int_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (int)0;
            Subject.Change(ref field, (int)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_int_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (int)0;
            Subject.Change(ref field, (int)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_int_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (int)0;
            Subject.Change(ref field, (int)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_int_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (int)0;
            Subject.Change(ref field, (int)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_int_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (int)0;
            Subject.Change(ref field, (int)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_int_1_ChangesValue() {
            var field = (int)0;
            Subject.Change(ref field, (int)1, "expected");
            Assert.That(field, Is.EqualTo((int)1));
        }

        [Test]
        public void Change_int_1_ReturnsFalse() {
            var field = (int)0;
            var actual = Subject.Change(ref field, (int)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_int_1_ReturnsTrue() {
            var field = (int)0;
            var actual = Subject.Change(ref field, (int)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_int_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_int_"] = false;
            var field = (int)0;
            var actual = subject.Change(ref field, (int)1, "P_int_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_int_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_int_"] = true;
            var field = (int)0;
            var actual = subject.Change(ref field, (int)1, "P_int_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_intn_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, "expected");
        }

        [Test]
        public void Change_intn_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (int?)0;
            Subject.Change(ref field, (int?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_intn_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (int?)0;
            Subject.Change(ref field, (int?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_intn_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (int?)0;
            Subject.Change(ref field, (int?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_intn_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (int?)0;
            Subject.Change(ref field, (int?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_intn_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (int?)0;
            Subject.Change(ref field, (int?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_intn_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (int?)0;
            Subject.Change(ref field, (int?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_intn_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (int?)0;
            Subject.Change(ref field, (int?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_intn_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (int?)0;
            Subject.Change(ref field, (int?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_intn_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (int?)0;
            Subject.Change(ref field, (int?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_intn_1_ChangesValue() {
            var field = (int?)0;
            Subject.Change(ref field, (int?)1, "expected");
            Assert.That(field, Is.EqualTo((int?)1));
        }

        [Test]
        public void Change_intn_1_ReturnsFalse() {
            var field = (int?)0;
            var actual = Subject.Change(ref field, (int?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_intn_1_ReturnsTrue() {
            var field = (int?)0;
            var actual = Subject.Change(ref field, (int?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_intn_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_intn_"] = false;
            var field = (int?)0;
            var actual = subject.Change(ref field, (int?)1, "P_intn_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_intn_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_intn_"] = true;
            var field = (int?)0;
            var actual = subject.Change(ref field, (int?)1, "P_intn_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_uint_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, "expected");
        }

        [Test]
        public void Change_uint_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (uint)0;
            Subject.Change(ref field, (uint)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_uint_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (uint)0;
            Subject.Change(ref field, (uint)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_uint_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (uint)0;
            Subject.Change(ref field, (uint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_uint_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (uint)0;
            Subject.Change(ref field, (uint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_uint_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (uint)0;
            Subject.Change(ref field, (uint)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_uint_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (uint)0;
            Subject.Change(ref field, (uint)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_uint_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (uint)0;
            Subject.Change(ref field, (uint)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_uint_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (uint)0;
            Subject.Change(ref field, (uint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_uint_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (uint)0;
            Subject.Change(ref field, (uint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_uint_1_ChangesValue() {
            var field = (uint)0;
            Subject.Change(ref field, (uint)1, "expected");
            Assert.That(field, Is.EqualTo((uint)1));
        }

        [Test]
        public void Change_uint_1_ReturnsFalse() {
            var field = (uint)0;
            var actual = Subject.Change(ref field, (uint)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_uint_1_ReturnsTrue() {
            var field = (uint)0;
            var actual = Subject.Change(ref field, (uint)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_uint_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_uint_"] = false;
            var field = (uint)0;
            var actual = subject.Change(ref field, (uint)1, "P_uint_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_uint_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_uint_"] = true;
            var field = (uint)0;
            var actual = subject.Change(ref field, (uint)1, "P_uint_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_uintn_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, "expected");
        }

        [Test]
        public void Change_uintn_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_uintn_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_uintn_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_uintn_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_uintn_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_uintn_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_uintn_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_uintn_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_uintn_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (uint?)0;
            Subject.Change(ref field, (uint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_uintn_1_ChangesValue() {
            var field = (uint?)0;
            Subject.Change(ref field, (uint?)1, "expected");
            Assert.That(field, Is.EqualTo((uint?)1));
        }

        [Test]
        public void Change_uintn_1_ReturnsFalse() {
            var field = (uint?)0;
            var actual = Subject.Change(ref field, (uint?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_uintn_1_ReturnsTrue() {
            var field = (uint?)0;
            var actual = Subject.Change(ref field, (uint?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_uintn_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_uintn_"] = false;
            var field = (uint?)0;
            var actual = subject.Change(ref field, (uint?)1, "P_uintn_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_uintn_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_uintn_"] = true;
            var field = (uint?)0;
            var actual = subject.Change(ref field, (uint?)1, "P_uintn_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_nint_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, "expected");
        }

        [Test]
        public void Change_nint_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (nint)0;
            Subject.Change(ref field, (nint)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_nint_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (nint)0;
            Subject.Change(ref field, (nint)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_nint_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (nint)0;
            Subject.Change(ref field, (nint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nint_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nint)0;
            Subject.Change(ref field, (nint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_nint_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (nint)0;
            Subject.Change(ref field, (nint)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_nint_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nint)0;
            Subject.Change(ref field, (nint)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_nint_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nint)0;
            Subject.Change(ref field, (nint)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_nint_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (nint)0;
            Subject.Change(ref field, (nint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nint_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nint)0;
            Subject.Change(ref field, (nint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nint_1_ChangesValue() {
            var field = (nint)0;
            Subject.Change(ref field, (nint)1, "expected");
            Assert.That(field, Is.EqualTo((nint)1));
        }

        [Test]
        public void Change_nint_1_ReturnsFalse() {
            var field = (nint)0;
            var actual = Subject.Change(ref field, (nint)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_nint_1_ReturnsTrue() {
            var field = (nint)0;
            var actual = Subject.Change(ref field, (nint)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_nint_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_nint_"] = false;
            var field = (nint)0;
            var actual = subject.Change(ref field, (nint)1, "P_nint_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_nint_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_nint_"] = true;
            var field = (nint)0;
            var actual = subject.Change(ref field, (nint)1, "P_nint_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_nintn_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, "expected");
        }

        [Test]
        public void Change_nintn_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_nintn_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_nintn_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nintn_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_nintn_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_nintn_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_nintn_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_nintn_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nintn_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nint?)0;
            Subject.Change(ref field, (nint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nintn_1_ChangesValue() {
            var field = (nint?)0;
            Subject.Change(ref field, (nint?)1, "expected");
            Assert.That(field, Is.EqualTo((nint?)1));
        }

        [Test]
        public void Change_nintn_1_ReturnsFalse() {
            var field = (nint?)0;
            var actual = Subject.Change(ref field, (nint?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_nintn_1_ReturnsTrue() {
            var field = (nint?)0;
            var actual = Subject.Change(ref field, (nint?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_nintn_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_nintn_"] = false;
            var field = (nint?)0;
            var actual = subject.Change(ref field, (nint?)1, "P_nintn_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_nintn_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_nintn_"] = true;
            var field = (nint?)0;
            var actual = subject.Change(ref field, (nint?)1, "P_nintn_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_nuint_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, "expected");
        }

        [Test]
        public void Change_nuint_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_nuint_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_nuint_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nuint_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_nuint_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_nuint_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_nuint_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_nuint_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nuint_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nuint)0;
            Subject.Change(ref field, (nuint)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nuint_1_ChangesValue() {
            var field = (nuint)0;
            Subject.Change(ref field, (nuint)1, "expected");
            Assert.That(field, Is.EqualTo((nuint)1));
        }

        [Test]
        public void Change_nuint_1_ReturnsFalse() {
            var field = (nuint)0;
            var actual = Subject.Change(ref field, (nuint)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_nuint_1_ReturnsTrue() {
            var field = (nuint)0;
            var actual = Subject.Change(ref field, (nuint)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_nuint_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_nuint_"] = false;
            var field = (nuint)0;
            var actual = subject.Change(ref field, (nuint)1, "P_nuint_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_nuint_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_nuint_"] = true;
            var field = (nuint)0;
            var actual = subject.Change(ref field, (nuint)1, "P_nuint_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_nuintn_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, "expected");
        }

        [Test]
        public void Change_nuintn_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_nuintn_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_nuintn_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nuintn_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_nuintn_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_nuintn_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_nuintn_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_nuintn_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nuintn_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_nuintn_1_ChangesValue() {
            var field = (nuint?)0;
            Subject.Change(ref field, (nuint?)1, "expected");
            Assert.That(field, Is.EqualTo((nuint?)1));
        }

        [Test]
        public void Change_nuintn_1_ReturnsFalse() {
            var field = (nuint?)0;
            var actual = Subject.Change(ref field, (nuint?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_nuintn_1_ReturnsTrue() {
            var field = (nuint?)0;
            var actual = Subject.Change(ref field, (nuint?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_nuintn_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_nuintn_"] = false;
            var field = (nuint?)0;
            var actual = subject.Change(ref field, (nuint?)1, "P_nuintn_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_nuintn_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_nuintn_"] = true;
            var field = (nuint?)0;
            var actual = subject.Change(ref field, (nuint?)1, "P_nuintn_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_long_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (long)0;
            Subject.Change(ref field, (long)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_long_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (long)0;
            Subject.Change(ref field, (long)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_long_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (long)0;
            Subject.Change(ref field, (long)1, "expected");
        }

        [Test]
        public void Change_long_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (long)0;
            Subject.Change(ref field, (long)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_long_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (long)0;
            Subject.Change(ref field, (long)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_long_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (long)0;
            Subject.Change(ref field, (long)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_long_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (long)0;
            Subject.Change(ref field, (long)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_long_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (long)0;
            Subject.Change(ref field, (long)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_long_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (long)0;
            Subject.Change(ref field, (long)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_long_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (long)0;
            Subject.Change(ref field, (long)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_long_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (long)0;
            Subject.Change(ref field, (long)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_long_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (long)0;
            Subject.Change(ref field, (long)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_long_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (long)0;
            Subject.Change(ref field, (long)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_long_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (long)0;
            Subject.Change(ref field, (long)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_long_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (long)0;
            Subject.Change(ref field, (long)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_long_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (long)0;
            Subject.Change(ref field, (long)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_long_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (long)0;
            Subject.Change(ref field, (long)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_long_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (long)0;
            Subject.Change(ref field, (long)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_long_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (long)0;
            Subject.Change(ref field, (long)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_long_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (long)0;
            Subject.Change(ref field, (long)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_long_1_ChangesValue() {
            var field = (long)0;
            Subject.Change(ref field, (long)1, "expected");
            Assert.That(field, Is.EqualTo((long)1));
        }

        [Test]
        public void Change_long_1_ReturnsFalse() {
            var field = (long)0;
            var actual = Subject.Change(ref field, (long)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_long_1_ReturnsTrue() {
            var field = (long)0;
            var actual = Subject.Change(ref field, (long)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_long_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_long_"] = false;
            var field = (long)0;
            var actual = subject.Change(ref field, (long)1, "P_long_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_long_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_long_"] = true;
            var field = (long)0;
            var actual = subject.Change(ref field, (long)1, "P_long_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_longn_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, "expected");
        }

        [Test]
        public void Change_longn_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (long?)0;
            Subject.Change(ref field, (long?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_longn_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (long?)0;
            Subject.Change(ref field, (long?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_longn_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (long?)0;
            Subject.Change(ref field, (long?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_longn_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (long?)0;
            Subject.Change(ref field, (long?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_longn_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (long?)0;
            Subject.Change(ref field, (long?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_longn_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (long?)0;
            Subject.Change(ref field, (long?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_longn_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (long?)0;
            Subject.Change(ref field, (long?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_longn_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (long?)0;
            Subject.Change(ref field, (long?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_longn_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (long?)0;
            Subject.Change(ref field, (long?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_longn_1_ChangesValue() {
            var field = (long?)0;
            Subject.Change(ref field, (long?)1, "expected");
            Assert.That(field, Is.EqualTo((long?)1));
        }

        [Test]
        public void Change_longn_1_ReturnsFalse() {
            var field = (long?)0;
            var actual = Subject.Change(ref field, (long?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_longn_1_ReturnsTrue() {
            var field = (long?)0;
            var actual = Subject.Change(ref field, (long?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_longn_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_longn_"] = false;
            var field = (long?)0;
            var actual = subject.Change(ref field, (long?)1, "P_longn_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_longn_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_longn_"] = true;
            var field = (long?)0;
            var actual = subject.Change(ref field, (long?)1, "P_longn_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_ulong_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, "expected");
        }

        [Test]
        public void Change_ulong_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_ulong_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_ulong_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ulong_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_ulong_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_ulong_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_ulong_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_ulong_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ulong_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ulong)0;
            Subject.Change(ref field, (ulong)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ulong_1_ChangesValue() {
            var field = (ulong)0;
            Subject.Change(ref field, (ulong)1, "expected");
            Assert.That(field, Is.EqualTo((ulong)1));
        }

        [Test]
        public void Change_ulong_1_ReturnsFalse() {
            var field = (ulong)0;
            var actual = Subject.Change(ref field, (ulong)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_ulong_1_ReturnsTrue() {
            var field = (ulong)0;
            var actual = Subject.Change(ref field, (ulong)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_ulong_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_ulong_"] = false;
            var field = (ulong)0;
            var actual = subject.Change(ref field, (ulong)1, "P_ulong_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_ulong_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_ulong_"] = true;
            var field = (ulong)0;
            var actual = subject.Change(ref field, (ulong)1, "P_ulong_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_ulongn_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, "expected");
        }

        [Test]
        public void Change_ulongn_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_ulongn_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_ulongn_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ulongn_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_ulongn_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_ulongn_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_ulongn_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_ulongn_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ulongn_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ulongn_1_ChangesValue() {
            var field = (ulong?)0;
            Subject.Change(ref field, (ulong?)1, "expected");
            Assert.That(field, Is.EqualTo((ulong?)1));
        }

        [Test]
        public void Change_ulongn_1_ReturnsFalse() {
            var field = (ulong?)0;
            var actual = Subject.Change(ref field, (ulong?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_ulongn_1_ReturnsTrue() {
            var field = (ulong?)0;
            var actual = Subject.Change(ref field, (ulong?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_ulongn_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_ulongn_"] = false;
            var field = (ulong?)0;
            var actual = subject.Change(ref field, (ulong?)1, "P_ulongn_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_ulongn_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_ulongn_"] = true;
            var field = (ulong?)0;
            var actual = subject.Change(ref field, (ulong?)1, "P_ulongn_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_short_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (short)0;
            Subject.Change(ref field, (short)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_short_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (short)0;
            Subject.Change(ref field, (short)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_short_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (short)0;
            Subject.Change(ref field, (short)1, "expected");
        }

        [Test]
        public void Change_short_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (short)0;
            Subject.Change(ref field, (short)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_short_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (short)0;
            Subject.Change(ref field, (short)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_short_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (short)0;
            Subject.Change(ref field, (short)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_short_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (short)0;
            Subject.Change(ref field, (short)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_short_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (short)0;
            Subject.Change(ref field, (short)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_short_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (short)0;
            Subject.Change(ref field, (short)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_short_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (short)0;
            Subject.Change(ref field, (short)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_short_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (short)0;
            Subject.Change(ref field, (short)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_short_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (short)0;
            Subject.Change(ref field, (short)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_short_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (short)0;
            Subject.Change(ref field, (short)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_short_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (short)0;
            Subject.Change(ref field, (short)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_short_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (short)0;
            Subject.Change(ref field, (short)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_short_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (short)0;
            Subject.Change(ref field, (short)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_short_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (short)0;
            Subject.Change(ref field, (short)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_short_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (short)0;
            Subject.Change(ref field, (short)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_short_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (short)0;
            Subject.Change(ref field, (short)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_short_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (short)0;
            Subject.Change(ref field, (short)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_short_1_ChangesValue() {
            var field = (short)0;
            Subject.Change(ref field, (short)1, "expected");
            Assert.That(field, Is.EqualTo((short)1));
        }

        [Test]
        public void Change_short_1_ReturnsFalse() {
            var field = (short)0;
            var actual = Subject.Change(ref field, (short)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_short_1_ReturnsTrue() {
            var field = (short)0;
            var actual = Subject.Change(ref field, (short)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_short_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_short_"] = false;
            var field = (short)0;
            var actual = subject.Change(ref field, (short)1, "P_short_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_short_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_short_"] = true;
            var field = (short)0;
            var actual = subject.Change(ref field, (short)1, "P_short_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_shortn_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, "expected");
        }

        [Test]
        public void Change_shortn_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (short?)0;
            Subject.Change(ref field, (short?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_shortn_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (short?)0;
            Subject.Change(ref field, (short?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_shortn_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (short?)0;
            Subject.Change(ref field, (short?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_shortn_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (short?)0;
            Subject.Change(ref field, (short?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_shortn_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (short?)0;
            Subject.Change(ref field, (short?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_shortn_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (short?)0;
            Subject.Change(ref field, (short?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_shortn_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (short?)0;
            Subject.Change(ref field, (short?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_shortn_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (short?)0;
            Subject.Change(ref field, (short?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_shortn_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (short?)0;
            Subject.Change(ref field, (short?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_shortn_1_ChangesValue() {
            var field = (short?)0;
            Subject.Change(ref field, (short?)1, "expected");
            Assert.That(field, Is.EqualTo((short?)1));
        }

        [Test]
        public void Change_shortn_1_ReturnsFalse() {
            var field = (short?)0;
            var actual = Subject.Change(ref field, (short?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_shortn_1_ReturnsTrue() {
            var field = (short?)0;
            var actual = Subject.Change(ref field, (short?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_shortn_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_shortn_"] = false;
            var field = (short?)0;
            var actual = subject.Change(ref field, (short?)1, "P_shortn_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_shortn_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_shortn_"] = true;
            var field = (short?)0;
            var actual = subject.Change(ref field, (short?)1, "P_shortn_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_ushort_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, "expected");
        }

        [Test]
        public void Change_ushort_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_ushort_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_ushort_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ushort_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_ushort_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_ushort_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_ushort_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_ushort_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ushort_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ushort)0;
            Subject.Change(ref field, (ushort)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ushort_1_ChangesValue() {
            var field = (ushort)0;
            Subject.Change(ref field, (ushort)1, "expected");
            Assert.That(field, Is.EqualTo((ushort)1));
        }

        [Test]
        public void Change_ushort_1_ReturnsFalse() {
            var field = (ushort)0;
            var actual = Subject.Change(ref field, (ushort)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_ushort_1_ReturnsTrue() {
            var field = (ushort)0;
            var actual = Subject.Change(ref field, (ushort)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_ushort_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_ushort_"] = false;
            var field = (ushort)0;
            var actual = subject.Change(ref field, (ushort)1, "P_ushort_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_ushort_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_ushort_"] = true;
            var field = (ushort)0;
            var actual = subject.Change(ref field, (ushort)1, "P_ushort_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_ushortn_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, "expected");
        }

        [Test]
        public void Change_ushortn_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_ushortn_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_ushortn_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ushortn_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_ushortn_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_ushortn_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_ushortn_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_ushortn_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ushortn_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_ushortn_1_ChangesValue() {
            var field = (ushort?)0;
            Subject.Change(ref field, (ushort?)1, "expected");
            Assert.That(field, Is.EqualTo((ushort?)1));
        }

        [Test]
        public void Change_ushortn_1_ReturnsFalse() {
            var field = (ushort?)0;
            var actual = Subject.Change(ref field, (ushort?)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_ushortn_1_ReturnsTrue() {
            var field = (ushort?)0;
            var actual = Subject.Change(ref field, (ushort?)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_ushortn_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_ushortn_"] = false;
            var field = (ushort?)0;
            var actual = subject.Change(ref field, (ushort?)1, "P_ushortn_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_ushortn_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_ushortn_"] = true;
            var field = (ushort?)0;
            var actual = subject.Change(ref field, (ushort?)1, "P_ushortn_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_object_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = (object)0;
            Subject.Change(ref field, (object)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_object_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = (object)0;
            Subject.Change(ref field, (object)1, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_object_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = (object)0;
            Subject.Change(ref field, (object)1, "expected");
        }

        [Test]
        public void Change_object_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = (object)0;
            Subject.Change(ref field, (object)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_object_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = (object)0;
            Subject.Change(ref field, (object)0, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_object_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (object)0;
            Subject.Change(ref field, (object)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_object_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (object)0;
            Subject.Change(ref field, (object)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_object_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (object)0;
            Subject.Change(ref field, (object)1, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_object_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = (object)0;
            Subject.Change(ref field, (object)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_object_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = (object)0;
            Subject.Change(ref field, (object)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_object_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (object)0;
            Subject.Change(ref field, (object)1, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_object_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (object)0;
            Subject.Change(ref field, (object)1, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_object_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (object)0;
            Subject.Change(ref field, (object)1, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_object_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = (object)0;
            Subject.Change(ref field, (object)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_object_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = (object)0;
            Subject.Change(ref field, (object)0, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_object_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (object)0;
            Subject.Change(ref field, (object)1, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_object_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (object)0;
            Subject.Change(ref field, (object)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_object_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (object)0;
            Subject.Change(ref field, (object)1, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_object_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = (object)0;
            Subject.Change(ref field, (object)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_object_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = (object)0;
            Subject.Change(ref field, (object)0, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_object_1_ChangesValue() {
            var field = (object)0;
            Subject.Change(ref field, (object)1, "expected");
            Assert.That(field, Is.EqualTo((object)1));
        }

        [Test]
        public void Change_object_1_ReturnsFalse() {
            var field = (object)0;
            var actual = Subject.Change(ref field, (object)0, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_object_1_ReturnsTrue() {
            var field = (object)0;
            var actual = Subject.Change(ref field, (object)1, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_object_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_object_"] = false;
            var field = (object)0;
            var actual = subject.Change(ref field, (object)1, "P_object_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_object_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_object_"] = true;
            var field = (object)0;
            var actual = subject.Change(ref field, (object)1, "P_object_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_bool_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = false;
            Subject.Change(ref field, true, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = false;
            Subject.Change(ref field, true, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = false;
            Subject.Change(ref field, true, "expected");
        }

        [Test]
        public void Change_bool_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = false;
            Subject.Change(ref field, false, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_bool_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = false;
            Subject.Change(ref field, false, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = false;
            Subject.Change(ref field, true, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = false;
            Subject.Change(ref field, true, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = false;
            Subject.Change(ref field, true, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_bool_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = false;
            Subject.Change(ref field, false, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_bool_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = false;
            Subject.Change(ref field, false, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = false;
            Subject.Change(ref field, true, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = false;
            Subject.Change(ref field, true, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = false;
            Subject.Change(ref field, true, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_bool_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = false;
            Subject.Change(ref field, false, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_bool_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = false;
            Subject.Change(ref field, false, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = false;
            Subject.Change(ref field, true, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = false;
            Subject.Change(ref field, true, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_bool_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = false;
            Subject.Change(ref field, true, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_bool_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = false;
            Subject.Change(ref field, false, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_bool_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = false;
            Subject.Change(ref field, false, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_bool_1_ChangesValue() {
            var field = false;
            Subject.Change(ref field, true, "expected");
            Assert.That(field, Is.EqualTo(true));
        }

        [Test]
        public void Change_bool_1_ReturnsFalse() {
            var field = false;
            var actual = Subject.Change(ref field, false, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_bool_1_ReturnsTrue() {
            var field = false;
            var actual = Subject.Change(ref field, true, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_bool_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_bool_"] = false;
            var field = false;
            var actual = subject.Change(ref field, true, "P_bool_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_bool_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_bool_"] = true;
            var field = false;
            var actual = subject.Change(ref field, true, "P_bool_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_string_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = "0";
            Subject.Change(ref field, "1", "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_string_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = "0";
            Subject.Change(ref field, "1", "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_string_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = "0";
            Subject.Change(ref field, "1", "expected");
        }

        [Test]
        public void Change_string_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = "0";
            Subject.Change(ref field, "0", "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_string_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = "0";
            Subject.Change(ref field, "0", "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_string_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = "0";
            Subject.Change(ref field, "1", expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_string_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = "0";
            Subject.Change(ref field, "1", expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_string_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = "0";
            Subject.Change(ref field, "1", expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_string_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = "0";
            Subject.Change(ref field, "0", expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_string_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = "0";
            Subject.Change(ref field, "0", expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_string_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = "0";
            Subject.Change(ref field, "1", expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_string_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = "0";
            Subject.Change(ref field, "1", new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_string_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = "0";
            Subject.Change(ref field, "1", new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_string_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = "0";
            Subject.Change(ref field, "0", new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_string_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = "0";
            Subject.Change(ref field, "0", new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_string_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = "0";
            Subject.Change(ref field, "1", expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_string_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = "0";
            Subject.Change(ref field, "1", new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_string_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = "0";
            Subject.Change(ref field, "1", new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_string_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = "0";
            Subject.Change(ref field, "0", expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_string_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = "0";
            Subject.Change(ref field, "0", expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_string_1_ChangesValue() {
            var field = "0";
            Subject.Change(ref field, "1", "expected");
            Assert.That(field, Is.EqualTo("1"));
        }

        [Test]
        public void Change_string_1_ReturnsFalse() {
            var field = "0";
            var actual = Subject.Change(ref field, "0", "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_string_1_ReturnsTrue() {
            var field = "0";
            var actual = Subject.Change(ref field, "1", "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_string_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_string_"] = false;
            var field = "0";
            var actual = subject.Change(ref field, "1", "P_string_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_string_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_string_"] = true;
            var field = "0";
            var actual = subject.Change(ref field, "1", "P_string_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_DateTime_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, "expected");
        }

        [Test]
        public void Change_DateTime_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MinValue, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_DateTime_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MinValue, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_DateTime_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MinValue, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_DateTime_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MinValue, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_DateTime_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MinValue, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_DateTime_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MinValue, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_DateTime_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_DateTime_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MinValue, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_DateTime_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MinValue, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_DateTime_1_ChangesValue() {
            var field = DateTime.MinValue;
            Subject.Change(ref field, DateTime.MaxValue, "expected");
            Assert.That(field, Is.EqualTo(DateTime.MaxValue));
        }

        [Test]
        public void Change_DateTime_1_ReturnsFalse() {
            var field = DateTime.MinValue;
            var actual = Subject.Change(ref field, DateTime.MinValue, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_DateTime_1_ReturnsTrue() {
            var field = DateTime.MinValue;
            var actual = Subject.Change(ref field, DateTime.MaxValue, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_DateTime_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_DateTime_"] = false;
            var field = DateTime.MinValue;
            var actual = subject.Change(ref field, DateTime.MaxValue, "P_DateTime_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_DateTime_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_DateTime_"] = true;
            var field = DateTime.MinValue;
            var actual = subject.Change(ref field, DateTime.MaxValue, "P_DateTime_");
            Assert.That(actual, Is.True);
        }
        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = e.PropertyName;

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, "expected");

            Assert.That(actual, Is.EqualTo("expected"));
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChangingBeforePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = e.PropertyName;
            Subject.PropertyChanging += (s, e) => Assert.That(actual, Is.Empty);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, "expected");
        }

        [Test]
        public void Change_TimeSpan_1_DoesNotRaisePropertyChanged() {
            var actual = "";
            Subject.PropertyChanged += (s, e) => actual = "fail";

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.Zero, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_TimeSpan_1_DoesNotRaisePropertyChanging() {
            var actual = "";
            Subject.PropertyChanging += (s, e) => actual = "fail";

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.Zero, "expected");

            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChangingDependentsBeforePropertyChanged() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => Assert.That(actual.Contains(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, expected[0], expected[1], expected[2]);
        }

        [Test]
        public void Change_TimeSpan_1_DoesNotRaisePropertyChangedDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanged += (s, e) => actual.Add(e.PropertyName);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.Zero, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_TimeSpan_1_DoesNotRaisePropertyChangingDependents() {
            var actual = new List<string>();
            var expected = new List<string> { "1", "2", "3" };
            Subject.PropertyChanging += (s, e) => actual.Add(e.PropertyName);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.Zero, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            var expected = new PropertyChangedEventArgs("expected");
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, expected);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, new PropertyChangedEventArgs(expected.PropertyName));

            Assert.That(actual.PropertyName, Is.SameAs(expected.PropertyName));
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChangingEventArgsBeforePropertyChanged() {
            var actual = default(PropertyChangingEventArgs);
            var expected = new PropertyChangingEventArgs("expected");
            Subject.PropertyChanged += (s, e) => Assert.That(actual.PropertyName, Is.EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, new PropertyChangedEventArgs(expected.PropertyName));
        }

        [Test]
        public void Change_TimeSpan_1_DoesNotRaisePropertyChangedEventArgs() {
            var actual = default(PropertyChangedEventArgs);
            Subject.PropertyChanged += (s, e) => actual = e;

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.Zero, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_TimeSpan_1_DoesNotRaisePropertyChangingEventArgs() {
            var actual = default(PropertyChangingEventArgs);
            Subject.PropertyChanging += (s, e) => actual = e;

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.Zero, new PropertyChangedEventArgs("fail"));

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, expected[0], expected[1], expected[2]);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));

            CollectionAssert.AreEqual(expected.Select(e => e.PropertyName), actual.Select(e => e.PropertyName));
        }

        [Test]
        public void Change_TimeSpan_1_RaisesPropertyChangingEventArgsDependentsBeforePropertyChanged() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangingEventArgs("1"), new PropertyChangingEventArgs("2"), new PropertyChangingEventArgs("3") };
            Subject.PropertyChanged += (s, e) => Assert.That(actual, Has.One.Property("PropertyName").EqualTo(e.PropertyName));
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, new PropertyChangedEventArgs(expected[0].PropertyName), new PropertyChangedEventArgs(expected[1].PropertyName), new PropertyChangedEventArgs(expected[2].PropertyName));
        }

        [Test]
        public void Change_TimeSpan_1_DoesNotRaisePropertyChangedEventArgsDependents() {
            var actual = new List<PropertyChangedEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanged += (s, e) => actual.Add(e);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.Zero, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_TimeSpan_1_DoesNotRaisePropertyChangingEventArgsDependents() {
            var actual = new List<PropertyChangingEventArgs>();
            var expected = new[] { new PropertyChangedEventArgs("1"), new PropertyChangedEventArgs("2"), new PropertyChangedEventArgs("3") };
            Subject.PropertyChanging += (s, e) => actual.Add(e);

            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.Zero, expected[0], expected[1], expected[2]);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void Change_TimeSpan_1_ChangesValue() {
            var field = TimeSpan.Zero;
            Subject.Change(ref field, TimeSpan.MaxValue, "expected");
            Assert.That(field, Is.EqualTo(TimeSpan.MaxValue));
        }

        [Test]
        public void Change_TimeSpan_1_ReturnsFalse() {
            var field = TimeSpan.Zero;
            var actual = Subject.Change(ref field, TimeSpan.Zero, "");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_TimeSpan_1_ReturnsTrue() {
            var field = TimeSpan.Zero;
            var actual = Subject.Change(ref field, TimeSpan.MaxValue, "");
            Assert.That(actual, Is.True);
        }

        [Test]
        public void Change_TimeSpan_1_ReturnsFalseIfPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_TimeSpan_"] = false;
            var field = TimeSpan.Zero;
            var actual = subject.Change(ref field, TimeSpan.MaxValue, "P_TimeSpan_");
            Assert.That(actual, Is.False);
        }

        [Test]
        public void Change_TimeSpan_1_ReturnsTrueIfNotPrevented() {
            var subject = new NotifierSubject1();
            subject.PreviewPropertyChangeLookup["P_TimeSpan_"] = true;
            var field = TimeSpan.Zero;
            var actual = subject.Change(ref field, TimeSpan.MaxValue, "P_TimeSpan_");
            Assert.That(actual, Is.True);
        }
    }
}
