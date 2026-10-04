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

    private sealed class FallbackMessage {
        public override string ToString() => "default formatting";
    }

    private sealed class ThrowingMessage {
        public override string ToString() => throw new InvalidOperationException("to string failed");
    }

    private static IEnumerable<object> CreateModifiedEnumerable() {
        var items = new List<object> { "first" };
        foreach (var item in items) {
            yield return item;
            items.Add("changed");
        }
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

    [Test]
    public void TopLevelFormattingFailureDoesNotDiscardOtherItems() {
        var actual = Subject.Format("a", new ThrowingMessage(), "b");
        var expected = new[] {
            "a",
            "<format error: InvalidOperationException: to string failed>",
            "b"
        };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void NullCallbackResultFallsBackToDefaultFormatting() {
        Subject.Format(typeof(FallbackMessage), _ => null);
        var actual = Subject.Format(new FallbackMessage());
        Assert.That(actual, Is.EqualTo(new[] { "default formatting" }));

        Subject.Format(typeof(List<string>), _ => null);
        var enumerable = Subject.Format(new List<string> { "first", "second" });
        Assert.That(enumerable, Is.EqualTo(new[] { "first", "second" }));
    }

    [Test]
    public void CallbackFailureDoesNotDiscardOtherItems() {
        Subject.Format(typeof(FallbackMessage), _ => throw new FormatException("formatter failed"));
        var actual = Subject.Format("before", new FallbackMessage(), "after");
        var expected = new[] {
            "before",
            "<format error: FormatException: formatter failed>",
            "after"
        };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void EnumerableElementFailureDoesNotDiscardOtherItems() {
        var actual = Subject.Format(new object[] { "first", new ThrowingMessage(), "last" }, "after");
        var expected = new[] {
            "first",
            "<format error: InvalidOperationException: to string failed>",
            "last",
            "after"
        };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void EnumerableFailureDoesNotDiscardOtherItems() {
        var actual = Subject.Format(CreateModifiedEnumerable(), "after");
        Assert.That(actual, Has.Length.EqualTo(3));
        Assert.That(actual[0], Is.EqualTo("first"));
        Assert.That(actual[1], Does.StartWith("<format error: InvalidOperationException: "));
        Assert.That(actual[2], Is.EqualTo("after"));
    }
}
