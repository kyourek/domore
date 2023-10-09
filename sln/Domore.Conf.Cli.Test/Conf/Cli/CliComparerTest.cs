using NUnit.Framework;

namespace Domore.Conf.Cli {
    [TestFixture]
    public sealed class CliComparerTest {
        private CliComparer Subject {
            get => _Subject ?? (_Subject = new CliComparer());
            set => _Subject = value;
        }
        private CliComparer _Subject;

        [SetUp]
        public void SetUp() {
            Subject = null;
        }

        [Test]
        public void Equals_TrueForSameString() {
            var s1 = "a";
            var s2 = s1;
            Assert.That(Subject.Equals(s1, s2));
        }

        [TestCase("a b", "a  b")]
        [TestCase("a  b", "a\t  b")]
        [TestCase(" a  b  ", " a\t  b\t")]
        public void Equals_TrueIgnoringSpace(string s1, string s2) {
            Assert.That(Subject.Equals(s1, s2));
        }

        public void Equals_FalseForDifferentStrings() {
            Assert.That(Subject.Equals("a", "b"), Is.False);
        }

        [TestCase("A B", "a b")]
        [TestCase("a B", "a b")]
        [TestCase("A b", "a B")]
        [TestCase("a b[c]", "a B[c]")]
        public void Equals_TrueIgnoringCase(string s1, string s2) {
            Assert.That(Subject.Equals(s1, s2));
        }

        [TestCase("a b[c]", "a b[C]")]
        public void Equals_FalseConsideringCaseInBrackets(string s1, string s2) {
            Assert.That(Subject.Equals(s1, s2), Is.False);
        }

        [TestCase("cmd arg1\tand-THIS=Param ", "\tCMD  arG1 and-this=Param")]
        public void Equals_TrueFormEquivalentLines(string s1, string s2) {
            Assert.That(Subject.Equals(s1, s2));
        }

        [TestCase("cmd arg1 param=value", "cmd arg1 param=Value")]
        public void Equals_FalseConsideringCaseInValues(string s1, string s2) {
            Assert.That(Subject.Equals(s1, s2), Is.False);
        }
    }
}
