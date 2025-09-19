using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Domore.Logs; 
[TestFixture]
public sealed class LogFormatterTest {
    private LogFormatter Subject {
        get => _Subject ??= new();
        set => _Subject = value;
    }
    private LogFormatter _Subject;

    [SetUp]
    public void SetUp() {
        Subject = null;
    }

    [Test]
    public void ExceptionIsFormatted() {
        try {
            throw new Exception("That didn't work.");
        }
        catch (Exception ex) {
            var actual = Subject.Format(ex);
            var expected = ex.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    [Test]
    public void EnumerableIsFormatted() {
        var list = new List<string> { "log1", "log2", "log3" };
        var actual = Subject.Format(list);
        var expected = new[] { "log1", "log2", "log3" };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void EnumerableIsFormattedDeeply() {
        var list = new List<string> { "log1", "log2\r\nlog3\nlog4\r\nlog5", "log6" };
        var actual = Subject.Format(list);
        var expected = new[] { "log1", "log2", "log3", "log4", "log5", "log6" };
        Assert.That(actual, Is.EqualTo(expected));
    }

    private sealed class SpecialMessage {
    }

    [Test]
    public void CallbackIsUsedToFormatByType() {
        Subject.Format(typeof(SpecialMessage), obj => ["This is a special message"]);
        var actual = Subject.Format(new SpecialMessage());
        var expected = new[] { "This is a special message" };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void CallbackIsUsedForItemsInEnumerable() {
        Subject.Format(typeof(SpecialMessage), obj => ["spcmsg"]);
        var actual = Subject.Format(new SpecialMessage(), new object[] { "another log", new SpecialMessage() });
        var expected = new[] { "spcmsg", "another log", "spcmsg" };
        Assert.That(actual, Is.EqualTo(expected));
    }
}
