using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Domore.Logs;

[TestFixture]
public sealed class LogFormatterRepairTest {
    private sealed class BrokenToString {
        public override string ToString() => throw new InvalidOperationException("to string failed");
    }

    private sealed class BrokenFormatter {
        public override string ToString() => "fallback";
    }

    private sealed class BrokenEnumerable : IEnumerable {
        public IEnumerator GetEnumerator() => new BrokenEnumerator();

        private sealed class BrokenEnumerator : IEnumerator, IDisposable {
            private int Position;
            public object Current => Position switch {
                1 => "before failure",
                2 => throw new InvalidOperationException("current failed"),
                3 => "after failure",
                _ => throw new InvalidOperationException("no current item")
            };
            public bool MoveNext() => ++Position <= 3;
            public void Reset() => throw new NotSupportedException();
            public void Dispose() { }
        }
    }

    [Test]
    public void ThrowingFormatterAndToStringDoNotDiscardLaterArguments() {
        var formatter = new LogFormatter();
        formatter.Format(typeof(BrokenFormatter), _ => throw new InvalidOperationException("formatter failed"));

        var actual = formatter.Format(new BrokenFormatter(), new BrokenToString(), "later argument");

        Assert.That(actual, Does.Contain("later argument"));
        Assert.That(actual, Has.Some.Contains("formatter failed"));
        Assert.That(actual, Has.Some.Contains("to string failed"));
    }

    [Test]
    public void BrokenEnumerablePreservesEarlierItemsAndLaterArguments() {
        var actual = new LogFormatter().Format(new BrokenEnumerable(), "after enumerable");

        Assert.That(actual, Does.Contain("before failure"));
        Assert.That(actual, Has.Some.Contains("current failed"));
        Assert.That(actual, Does.Contain("after failure"));
        Assert.That(actual, Does.Contain("after enumerable"));
    }

    [Test]
    public void NullFormatterResultFallsBackToNormalFormatting() {
        var formatter = new LogFormatter();
        formatter.Format(typeof(BrokenFormatter), _ => null);

        var actual = formatter.Format(new BrokenFormatter(), "after null formatter");

        Assert.That(actual, Is.EqualTo(["fallback", "after null formatter"]));
    }

    [Test]
    public void NullParameterArrayFormatsAsOneEmptyLine() {
        var actual = new LogFormatter().Format((object[])null);
        Assert.That(actual, Is.EqualTo([""]));
    }

    [Test]
    public void NullParameterArrayThroughILogRaisesOneEmptyMessageLine() {
        var previousThreshold = Logging.EventThreshold;
        string[] received = null;
        LogEventHandler handler = (_, args) => received = new List<string>(args.LogList).ToArray();
        try {
            Logging.EventThreshold = LogSeverity.Info;
            Logging.Event += handler;
            Logging.For(typeof(LogFormatterRepairTest)).Info((object[])null);
            Assert.That(received, Is.EqualTo([""]));
        }
        finally {
            Logging.Event -= handler;
            Logging.EventThreshold = previousThreshold;
            Logging.Complete();
        }
    }

    [Test]
    public void EnumerableExpansionIsLimitedToOneHundredByDefaultAndMarksTruncation() {
        var actual = new LogFormatter().Format((object)new RangeEnumerable(101));

        Assert.That(actual, Has.Length.EqualTo(101));
        Assert.That(actual[0], Is.EqualTo("0"));
        Assert.That(actual[99], Is.EqualTo("99"));
        Assert.That(actual[100], Does.Contain("truncated"));
    }

    [Test]
    public void EnumerableExpansionLimitCanBeSetAndMustBePositive() {
        var formatter = new LogFormatter { EnumerableItemLimit = 2 };

        var actual = formatter.Format((object)new RangeEnumerable(5));

        Assert.That(actual, Has.Length.EqualTo(3));
        Assert.That(actual[0], Is.EqualTo("0"));
        Assert.That(actual[1], Is.EqualTo("1"));
        Assert.That(actual[2], Does.Contain("truncated"));
        Assert.Throws<ArgumentOutOfRangeException>(() => formatter.EnumerableItemLimit = 0);
    }

    [Test]
    public void EnumerableExpansionLimitIsConfigurableThroughLogging() {
        var previous = Logging.EnumerableItemLimit;
        try {
            Logging.EnumerableItemLimit = 3;
            Assert.That(Logging.EnumerableItemLimit, Is.EqualTo(3));
        }
        finally {
            Logging.EnumerableItemLimit = previous;
        }
    }

    private sealed class RangeEnumerable(int count) : IEnumerable {
        public IEnumerator GetEnumerator() {
            for (var i = 0; i < count; i++) yield return i;
        }
    }
}
