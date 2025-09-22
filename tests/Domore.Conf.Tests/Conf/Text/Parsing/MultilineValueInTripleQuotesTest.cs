using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Text.Parsing {
    [TestFixture]
    internal sealed class MultilineValueInTripleQuotesTest {
        private static string Quote(char q, string newLine, string s) {
            return $"{q}{q}{q}" + newLine + s + newLine + $"{q}{q}{q}";
        }

        private static string[] NewLineSource => [
            "\n",
            "\r\n",
        ];

        private static string[] StringValueSource => [
            "Hello",
            " \n Goodbye \r\n\t",
            " Several\n\r\n lines \"\"\" \r\n\n",
            " Several\n\r\n lines ''' \r\n\n",
            " \"\" \" more \n lines \r\n\" \"\" ",
            " '' ' more \n lines \r\n' '' ",
            "{\nbraced\n}"
        ];

        private static IEnumerable<(string value, string conf)> QuoteSource(char quoteChar) =>
            NewLineSource.Select(newLine =>
                StringValueSource.Select(stringValue =>
                (stringValue, Quote(quoteChar, newLine, stringValue))))
            .SelectMany(s => s);

        private static IEnumerable<(string value, string conf)> SingleQuoteSource => QuoteSource('\'');
        private static IEnumerable<(string value, string conf)> DoubleQuoteSource => QuoteSource('"');

        class ObjWithString {
            public string StringProp { get; set; }
        }

        private static object[][] ObjWithString_CanConfigureFromTripleQuotedValue_Source =>
            [.. new[] { 
                "string prop =",
                "stringprop = \t  "
            }
                .Select(key =>
                    SingleQuoteSource
                        .Concat(DoubleQuoteSource)
                        .Select(item => (conf: key + item.conf, item.value)))
                .SelectMany(items => items)
                .Select(item => new object[] {item.conf, item.value})];

        [TestCaseSource(nameof(ObjWithString_CanConfigureFromTripleQuotedValue_Source))]
        public void TripleQuotesEnclosesMultilineValue(string conf, string value) {
            var obj = new ObjWithString();
            Conf.Contain(conf).Configure(obj, key: "");
            Assert.That(obj.StringProp, Is.EqualTo(value));
        }

        [TestCase('\'')]
        [TestCase('"')]
        public void TripleQuotesTriggersMultilineValue(char q) {
            var obj = new ObjWithString();
            Conf.Contain($"ObjWithString.StringProp = {q}{q}{q}\nthe-value\n{q}{q}{q}").Configure(obj);
            Assert.That(obj.StringProp, Is.EqualTo("the-value"));
        }

        [TestCase('\'')]
        [TestCase('"')]
        public void SingleQuoteDoesNotTriggersMultilineValue(char q) {
            var obj = new ObjWithString();
            Conf.Contain($"ObjWithString.StringProp = {q}\nthe-value\n{q}{q}{q}").Configure(obj);
            Assert.That(obj.StringProp, Is.EqualTo($"{q}"));
        }

        [TestCase('\'')]
        [TestCase('"')]
        public void DoubleQuotesDoesNotTriggersMultilineValue(char q) {
            var obj = new ObjWithString();
            Conf.Contain($"ObjWithString.StringProp = {q}{q}\nthe-value\n{q}{q}{q}").Configure(obj);
            Assert.That(obj.StringProp, Is.EqualTo($"{q}{q}"));
        }

        [TestCase('\'')]
        [TestCase('"')]
        public void AlmostTripleQuotesDoesNotTriggersMultilineValue(char q) {
            var obj = new ObjWithString();
            Conf.Contain($"ObjWithString.StringProp = {q}{q} {q}\nthe-value\n{q}{q}{q}").Configure(obj);
            Assert.That(obj.StringProp, Is.EqualTo($"{q}{q} {q}"));
        }

        [TestCase('\'')]
        [TestCase('"')]
        public void QuadrupleQuotesDoesNotTriggersMultilineValue(char q) {
            var obj = new ObjWithString();
            Conf.Contain($"ObjWithString.StringProp = {q}{q}{q}{q}\nthe-value\n{q}{q}{q}").Configure(obj);
            Assert.That(obj.StringProp, Is.EqualTo($"{q}{q}{q}{q}"));
        }

        [TestCase('\'')]
        [TestCase('"')]
        public void TripleQuotesMustBeOnSameLineAsKey(char q) {
            var obj = new ObjWithString();
            Conf.Contain($"ObjWithString.StringProp = \n{q}{q}{q}\nthe-value\n{q}{q}{q}").Configure(obj);
            Assert.That(obj.StringProp, Is.Null);
        }

        [TestCase('\'', '"')]
        [TestCase('"', '\'')]
        public void TripleQuotesMustBeClosedWithSameQuoteCha1r(char q1, char q2) {
            var obj = new ObjWithString();
            Conf.Contain($"ObjWithString.StringProp = {q1}{q1}{q1}\n{q2}{q2}{q2}\nthe-value\n{q2}{q2}{q2}\n{q1}{q1}{q1}").Configure(obj);
            Assert.That(obj.StringProp, Is.EqualTo($"{q2}{q2}{q2}\nthe-value\n{q2}{q2}{q2}"));
        }

        [TestCase('\'')]
        [TestCase('"')]
        public void TripleQuotesCanContainBraces(char q) {
            var obj = new ObjWithString();
            Conf.Contain($"ObjWithString.StringProp = {q}{q}{q}\n{{\nthe-value\n}}\n{q}{q}{q}").Configure(obj);
            Assert.That(obj.StringProp, Is.EqualTo("{\nthe-value\n}"));
        }
    }
}
