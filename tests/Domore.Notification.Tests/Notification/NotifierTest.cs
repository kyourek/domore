using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Domore.Notification;

[TestFixture]
public partial class NotifierTest {
    private Notifier Subject {
        get => field ??= new Notifier();
        set => field = value;
    }

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
    public void NotifiedPropertyRaisesPropertyChanging() {
        var subject = new Subject1();
        var entered = 0;
        subject.PropertyChanging += (s, e) => {
            if (e.PropertyName == nameof(subject.Foo)) {
                entered++;
            }
        };
        subject.Foo = "bar";
        Assert.That(entered, Is.EqualTo(1));
    }

    [Test]
    public void NotifiedPropertyRaisesPropertyChangingBeforePropertyChanged() {
        var subject = new Subject1();
        var entered = 0;
        subject.PropertyChanged += (s, e) => {
            Assert.That(entered, Is.EqualTo(1));
        };
        subject.PropertyChanging += (s, e) => {
            if (e.PropertyName == nameof(subject.Foo)) {
                entered++;
            }
        };
        subject.Foo = "bar";
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
    public void NotifiedPropertyDoesNotRaisePropertyChanging() {
        var subject = new Subject1();
        var entered = 0;
        subject.PropertyChanging += (s, e) => {
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
    public void NotifiedPropertyRaisesPropertyChangingForDependents() {
        var events = new List<string>();
        var subject = new Subject2();
        subject.PropertyChanging += (s, e) => {
            events.Add(e.PropertyName);
        };
        subject.Bar = 1;
        Assert.That(events, Is.EqualTo(new[] { "Bar", "Foo" }));
    }

    [Test]
    public void NotifiedPropertyRaisesPropertyChangingForDependentsBeforePropertyChanged() {
        var events = new List<string>();
        var subject = new Subject2();
        subject.PropertyChanged += (s, e) => {
            Assert.That(events, Has.One.EqualTo(e.PropertyName));
        };
        subject.PropertyChanging += (s, e) => {
            events.Add(e.PropertyName);
        };
        subject.Bar = 1;
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

    [Test]
    public void NotifiedPropertyDoesNotRaisesPropertyChangingIfValueDoesNotChange() {
        var events = new List<string>();
        var subject = new Subject2();
        subject.PropertyChanging += (s, e) => {
            events.Add(e.PropertyName);
        };
        subject.Bar = 0;
        Assert.That(events, Is.EqualTo(new string[] { }));
    }

    private class Subject3 : Notifier {
        public bool FooChanged { get; private set; }

        public double Foo {
            get => _Foo.Value;
            set => FooChanged = Change(_Foo, value);
        }
        private readonly Notified<double> _Foo = new(nameof(Foo));
    }

    [Test]
    public void ChangeReturnsTrueWhenNotifiedObjectChanges() {
        var subject = new Subject3();
        subject.Foo = 0.1;
        Assert.That(subject.FooChanged, Is.True);
    }

    [Test]
    public void ChangeReturnsFalseWhenNotifiedObjectDoesNotChange() {
        var subject = new Subject3();
        subject.Foo = 0.0;
        Assert.That(subject.FooChanged, Is.False);
    }

    [Test]
    public void Change_DoesNotRaisePropertyChangedIfPrevented() {
        var subject = new NotifierSubject1();
        var count = 0;
        subject.PropertyChanged += (s, e) => {
            count++;
        };
        subject.PreviewPropertyChangeLookup[nameof(subject.P_int_)] = false;
        subject.P_int_ = 1;
        subject.P_int_ = 2;
        Assert.That(count, Is.Zero);
    }

    [Test]
    public void Change_DoesNotRaisePropertyChangingIfPrevented() {
        var subject = new NotifierSubject1();
        var count = 0;
        subject.PropertyChanging += (s, e) => {
            count++;
        };
        subject.PreviewPropertyChangeLookup[nameof(subject.P_int_)] = false;
        subject.P_int_ = 1;
        subject.P_int_ = 2;
        Assert.That(count, Is.Zero);
    }

    [Test]
    public void Change_RaisesPropertyChangedIfNotPrevented() {
        var subject = new NotifierSubject1();
        var count = 0;
        subject.PropertyChanged += (s, e) => {
            count++;
        };
        subject.PreviewPropertyChangeLookup[nameof(subject.P_int_)] = true;
        subject.P_int_ = 1;
        subject.P_int_ = 2;
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void Change_RaisesPropertyChangingIfNotPrevented() {
        var subject = new NotifierSubject1();
        var count = 0;
        subject.PropertyChanging += (s, e) => {
            count++;
        };
        subject.PreviewPropertyChangeLookup[nameof(subject.P_int_)] = true;
        subject.P_int_ = 1;
        subject.P_int_ = 2;
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void Change_DoesNotRaisePropertyChangedIfPreventedWhenChangingNotified() {
        var subject = new NotifierSubject1();
        var count = 0;
        subject.PropertyChanged += (s, e) => {
            count++;
        };
        subject.PreviewPropertyChangeLookup[nameof(subject.N_string_)] = false;
        subject.N_string_ = "1";
        subject.N_string_ = "2";
        Assert.That(count, Is.Zero);
    }

    [Test]
    public void Change_DoesNotRaisePropertyChangingIfPreventedWhenChangingNotified() {
        var subject = new NotifierSubject1();
        var count = 0;
        subject.PropertyChanging += (s, e) => {
            count++;
        };
        subject.PreviewPropertyChangeLookup[nameof(subject.N_string_)] = false;
        subject.N_string_ = "1";
        subject.N_string_ = "2";
        Assert.That(count, Is.Zero);
    }

    [Test]
    public void Change_RaisesPropertyChangedIfNotPreventedWhenChangingNotified() {
        var subject = new NotifierSubject1();
        var count = 0;
        subject.PropertyChanged += (s, e) => {
            count++;
        };
        subject.PreviewPropertyChangeLookup[nameof(subject.N_string_)] = true;
        subject.N_string_ = "1";
        subject.N_string_ = "2";
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void Change_RaisesPropertyChangingIfNotPreventedWhenChangingNotified() {
        var subject = new NotifierSubject1();
        var count = 0;
        subject.PropertyChanging += (s, e) => {
            count++;
        };
        subject.PreviewPropertyChangeLookup[nameof(subject.N_string_)] = true;
        subject.N_string_ = "1";
        subject.N_string_ = "2";
        Assert.That(count, Is.EqualTo(2));
    }

#if NET45_OR_GREATER || NETCOREAPP
    private class Subject4 : Notifier.WithErrorInfo {
        private void ValidateMyNum() {
            ClearErrors(nameof(MyNum));
            if (!int.TryParse(MyNum, out _)) {
                AddError(nameof(MyNum), "This has to be a num!");
            }
        }

        public string MyNum {
            get => _MyNum;
            set {
                if (Change(ref _MyNum, value)) {
                    ValidateMyNum();
                }
            }
        }
        private string _MyNum;
    }

    [Test]
    public void HasErrors_IsInitiallyFalse() {
        var subject = new Subject4();
        Assert.That(((INotifyDataErrorInfo)subject).HasErrors, Is.False);
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("MyNum")]
    public void GetErrors_IsInitiallyNull(string propertyName) {
        var subject = new Subject4();
        Assert.That(((INotifyDataErrorInfo)subject).GetErrors(propertyName), Is.Null);
    }

    [Test]
    public void ErrorsChanged_IsRaisedOnError() {
        var subject = new Subject4();
        var count = 0;
        ((INotifyDataErrorInfo)subject).ErrorsChanged += (s, e) => {
            count++;
        };
        subject.MyNum = "nope";
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void ErrorsChanged_IsRaisedWithPropertyName() {
        var subject = new Subject4();
        var actual = "";
        ((INotifyDataErrorInfo)subject).ErrorsChanged += (s, e) => {
            actual = e.PropertyName;
        };
        subject.MyNum = "nope";
        Assert.That(actual, Is.EqualTo(nameof(subject.MyNum)));
    }

    [Test]
    public void ErrorsChanged_IsRaisedWithError() {
        var subject = new Subject4();
        var actual = default(object);
        ((INotifyDataErrorInfo)subject).ErrorsChanged += (s, e) => {
            actual = ((INotifyDataErrorInfo)subject).GetErrors(nameof(subject.MyNum)).OfType<object>().Single();
        };
        subject.MyNum = "nope";
        Assert.That(actual, Is.EqualTo("This has to be a num!"));
    }

    [Test]
    public void ErrorsChanged_IsNotRaisedIfThereIsNoError() {
        var subject = new Subject4();
        var count = 0;
        ((INotifyDataErrorInfo)subject).ErrorsChanged += (s, e) => {
            count++;
        };
        subject.MyNum = "1234";
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void GetErrors_IsNullIfThereAreNoErrors() {
        var subject = new Subject4();
        subject.MyNum = "1234";
        var actual = ((INotifyDataErrorInfo)subject).GetErrors(nameof(subject.MyNum));
        Assert.That(actual, Is.Null);
    }

    [Test]
    public void HasErrors_IsTrueIfThereAreErrors() {
        var subject = new Subject4();
        subject.MyNum = "1234-nope";
        var actual = ((INotifyDataErrorInfo)subject).HasErrors;
        Assert.That(actual, Is.True);
    }

    [Test]
    public void HasErrors_IsFalseIfThereAreNoErrors() {
        var subject = new Subject4();
        subject.MyNum = "1234-nope";
        subject.MyNum = "1234";
        var actual = ((INotifyDataErrorInfo)subject).HasErrors;
        Assert.That(actual, Is.False);
    }

    [Test]
    public void ClearErrors_RemovesExistingErrors() {
        var subject = new Subject4();
        subject.MyNum = "1234-nope";
        subject.MyNum = "1234";
        var actual = ((INotifyDataErrorInfo)subject).HasErrors;
        Assert.That(actual, Is.False);
    }

    private class Subject5 : Subject4 {
        private void ValidateMyVal() {
            RemoveError("", "Val and num are wrong.");
            RemoveError(nameof(MyVal), "Val must be equal to num.");
            RemoveError(nameof(MyNum), "Val must be equal to num.");
            if (!double.TryParse(MyVal, out var val) || !double.TryParse(MyNum, out var num) || val != num) {
                AddError("", "Val and num are wrong.");
                AddError(nameof(MyNum), "Val must be equal to num.");
                AddError(nameof(MyVal), "Val must be equal to num.");
            }
        }

        public string MyVal {
            get => _MyVal;
            set {
                if (Change(ref _MyVal, value)) {
                    ValidateMyVal();
                }
            }
        }
        private string _MyVal;
    }

    [Test]
    public void GetErrors_GetsMultipleErrorsForProperty() {
        var subject = new Subject5();
        subject.MyNum = "hello";
        subject.MyVal = "world";
        var actual = ((INotifyDataErrorInfo)subject).GetErrors(nameof(subject.MyNum)).OfType<object>().Count();
        Assert.That(actual, Is.EqualTo(2));
    }

    [Test]
    public void GetErrors_GetsCorrectErrorsForProperty() {
        var subject = new Subject5();
        subject.MyNum = "hello";
        subject.MyVal = "world";
        var actual = ((INotifyDataErrorInfo)subject).GetErrors(nameof(subject.MyNum));
        var expected = new[] { "This has to be a num!", "Val must be equal to num." };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RemoveError_RemovesTheEquivalentError() {
        var subject = new Subject5();
        subject.MyNum = "hello";
        subject.MyVal = "world";
        subject.MyNum = "12";
        subject.MyVal = "12";
        var actual = ((INotifyDataErrorInfo)subject).GetErrors(nameof(subject.MyNum));
        Assert.That(actual, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    public void GetErrors_ReturnsEntityErrorsIfProvidedEmptyString(string propertyName) {
        var subject = new Subject5();
        subject.MyNum = "1234";
        subject.MyVal = "2345";
        var actual = ((INotifyDataErrorInfo)subject).GetErrors(propertyName);
        var expected = new[] { "Val and num are wrong." };
        Assert.That(actual, Is.EqualTo(expected));
    }

    private class Subject6 : Subject5 {
        private void ValidateFoo() {
            ClearErrors(nameof(Foo));
            if (Foo != null) {
                AddError(nameof(Foo), Foo);
                AddError(nameof(Foo), "Foo");
            }
        }

        public object Foo {
            get => _Foo;
            set {
                if (Change(ref _Foo, value)) {
                    ValidateFoo();
                }
            }
        }
        private object _Foo;
    }

    [Test]
    public void GetErrors_GetsAllErrorsOfDifferentTypes() {
        var subject = new Subject6();
        subject.Foo = new Exception();
        var actual = ((INotifyDataErrorInfo)subject).GetErrors("Foo");
        var expected = new object[] { subject.Foo, "Foo" };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ErrorsChanged_IsRaisedWithException() {
        var subject = new Subject6();
        var count = 0;
        ((INotifyDataErrorInfo)subject).ErrorsChanged += (s, e) => {
            count++;
        };
        subject.Foo = new object();
        Assert.That(count, Is.EqualTo(2));
    }
#endif
}
