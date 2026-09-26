using NUnit.Framework;
using System;

namespace Domore.Windows.Controls;

[TestFixture]
public class TextReaderTextBuilderTest {
    [Test]
    public void Constructor_ThrowsWhenTextReaderIsNull() {
        var ex = Assert.Throws<ArgumentNullException>(() => new TextReaderTextBuilder(null));
        Assert.That(ex.ParamName, Is.EqualTo("textReader"));
    }

    [Test]
    public void Constructor_SetsTextReader() {
        DispatcherTest.Run(() => {
            var textReader = new TextReader();
            var subject = new TextReaderTextBuilder(textReader);
            Assert.That(subject.TextReader, Is.SameAs(textReader));
            return System.Threading.Tasks.Task.CompletedTask;
        });
    }
}
