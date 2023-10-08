//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//using NUnit.Framework;

//namespace Domore.ComponentModel {
//    [TestFixture]
//    public class NotifyPropertyChangedHelperTest {
//        private class Implementation : NotifyPropertyChangedImplementation {
//        }

//        private NotifyPropertyChangedHelper Subject {
//            get => _Subject ?? (_Subject = new NotifyPropertyChangedHelper(new Implementation()));
//            set => _Subject = value;
//        }
//        private NotifyPropertyChangedHelper _Subject;

//        [SetUp]
//        public void SetUp() {
//            Subject = null;
//        }

//        [TestCase(default, default)]
//        [TestCase(true, true)]
//        [TestCase(false, false)]
//        [TestCase(0, 0)]
//        [TestCase(1, 1)]
//        [TestCase(1.1, 1.1)]
//        [TestCase('a', 'a')]
//        [TestCase("abc", "abc")]
//        public void Property_ReturnsFalseIfValuesAreEqual(object oldValue, object newValue) {
//            Assert.That(Subject.Property(ref oldValue, newValue, ""), Is.False);
//        }

//        [TestCase(default, "")]
//        [TestCase(true, false)]
//        [TestCase(false, true)]
//        [TestCase(0, 1)]
//        [TestCase(1, 0)]
//        [TestCase(1.1, 1.2)]
//        [TestCase('a', 'b')]
//        [TestCase("abc", "def")]
//        public void Property_ReturnsTrueIfValuesAreNotEqual(object oldValue, object newValue) {
//            Assert.That(Subject.Property(ref oldValue, newValue, ""), Is.True);
//        }
//    }
//}
