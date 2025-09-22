using NUnit.Framework;

namespace Domore.Conf.Text.Parsing {
    [TestFixture]
    internal sealed class MultilineValueInBracesTest {
        class ObjWithString {
            public string StringProp { get; set; }
        }

        [TestCase('\'')]
        [TestCase('"')]
        public void BracesCanContainTripleQuotes(char q) {
            var obj = new ObjWithString();
            Conf.Contain($"ObjWithString.StringProp = {{\n{q}{q}{q}\nthe-value\n{q}{q}{q}\n}}").Configure(obj);
            Assert.That(obj.StringProp, Is.EqualTo($"{q}{q}{q}\nthe-value\n{q}{q}{q}"));
        }
    }
}
