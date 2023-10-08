using NUnit.Framework;
using System;

namespace Domore.Conf {
    [TestFixture]
    public class ConfTest {
        private class A {
            public TimeSpan TimeSpan { get; set; }
        }

        private class B {
            public A A { get; set; }
            public DateTime DateTime { get; set; }
        }

        private class C {
            public A A { get; set; }
            public B B { get; set; }
            public string Text { get; set; }
        }

        private void Contain_ReturnsContainerThatCanConfigureFromObjectSource(Action<C, C> test) {
            var container = Conf.Contain((object)@"
                C.A.TimeSpan = 01:11:11
                C.B.DateTime = 01/02/2022
                C.B.A.TimeSpan = 00:01:11
            ");
            var expected = new C {
                A = new A {
                    TimeSpan = TimeSpan.Parse("01:11:11")
                },
                B = new B {
                    A = new A {
                        TimeSpan = TimeSpan.Parse("00:01:11"),
                    },
                    DateTime = DateTime.Parse("01/02/2022")
                }
            };
            var actual = container.Configure(new C());
            test(actual, expected);
        }

        [Test]
        public void Contain_ReturnsContainerThatCanConfigureFromObjectSource_1() {
            Contain_ReturnsContainerThatCanConfigureFromObjectSource((actual, expected) =>
                Assert.That(actual.A.TimeSpan, Is.EqualTo(expected.A.TimeSpan)));
        }

        [Test]
        public void Contain_ReturnsContainerThatCanConfigureFromObjectSource_2() {
            Contain_ReturnsContainerThatCanConfigureFromObjectSource((actual, expected) =>
                Assert.That(actual.B.A.TimeSpan, Is.EqualTo(expected.B.A.TimeSpan)));
        }

        [Test]
        public void Contain_ReturnsContainerThatCanConfigureFromObjectSource_3() {
            Contain_ReturnsContainerThatCanConfigureFromObjectSource((actual, expected) =>
                Assert.That(actual.B.DateTime, Is.EqualTo(expected.B.DateTime)));
        }

        private void Contain_ReturnsContainerThatCanConfigureFromStringSource(Action<C, C> test) {
            var container = Conf.Contain(@"
                C. A.  time span = 02:34:56
                C. B.   date time= 02/13/1987 1:45 PM
                C. B. a.TimeSpan = 12:56:43
                C. text          =        {

        Lorem ipsum dolor sit amet, consectetur adipiscing elit.
            Integer vel iaculis justo. Integer lacinia est ex, eu sagittis ante lobortis vitae. Mauris vitae rutrum dolor, 
id varius nisl. Nam facilisis ultricies leo eget gravida. In consectetur urna lorem, 
                eu suscipit lectus tincidunt faucibus. Vivamus blandit ex id blandit pretium. 
Etiam semper mattis tincidunt. Sed in consequat elit. Fusce orci enim, rhoncus in elementum vitae, molestie et turpis.
     
                } 
            ");
            var expected = new C {
                A = new A {
                    TimeSpan = TimeSpan.Parse("02:34:56")
                },
                B = new B {
                    A = new A {
                        TimeSpan = TimeSpan.Parse("12:56:43"),
                    },
                    DateTime = DateTime.Parse("02/13/1987 1:45 PM")
                },
                Text = @"
        Lorem ipsum dolor sit amet, consectetur adipiscing elit.
            Integer vel iaculis justo. Integer lacinia est ex, eu sagittis ante lobortis vitae. Mauris vitae rutrum dolor, 
id varius nisl. Nam facilisis ultricies leo eget gravida. In consectetur urna lorem, 
                eu suscipit lectus tincidunt faucibus. Vivamus blandit ex id blandit pretium. 
Etiam semper mattis tincidunt. Sed in consequat elit. Fusce orci enim, rhoncus in elementum vitae, molestie et turpis.
     "
            };
            var actual = container.Configure(new C());
            test(actual, expected);
        }

        [Test]
        public void Contain_ReturnsContainerThatCanConfigureFromStringSource_1() {
            Contain_ReturnsContainerThatCanConfigureFromStringSource((actual, expected) =>
                Assert.That(actual.A.TimeSpan, Is.EqualTo(expected.A.TimeSpan)));
        }

        [Test]
        public void Contain_ReturnsContainerThatCanConfigureFromStringSource_2() {
            Contain_ReturnsContainerThatCanConfigureFromStringSource((actual, expected) =>
                Assert.That(actual.B.DateTime, Is.EqualTo(expected.B.DateTime)));
        }

        [Test]
        public void Contain_ReturnsContainerThatCanConfigureFromStringSource_3() {
            Contain_ReturnsContainerThatCanConfigureFromStringSource((actual, expected) =>
                Assert.That(actual.B.A.TimeSpan, Is.EqualTo(expected.B.A.TimeSpan)));
        }

        [Test]
        public void Contain_ReturnsContainerThatCanConfigureFromStringSource_4() {
            Contain_ReturnsContainerThatCanConfigureFromStringSource((actual, expected) =>
                Assert.That(actual.Text, Is.EqualTo(expected.Text)));
        }
    }
}
