using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Text.Builders;

[TestFixture]
public class TextLineBuilderTest {
    private static readonly MethodInfo AddMethod = typeof(DecodedTextBuilder)
        .GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo ClearMethod = typeof(DecodedTextBuilder)
        .GetMethod("Clear", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo CompleteMethod = typeof(DecodedTextBuilder)
        .GetMethod("Complete", BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(CancellationToken)], null);

    private List<string> Lines;
    private TextLineBuilder Subject;

    private void Add(string text) {
        var task = (Task)AddMethod.Invoke(Subject, [text.AsMemory(), CancellationToken.None]);
        task.GetAwaiter().GetResult();
    }

    private void Clear() {
        var task = (Task)ClearMethod.Invoke(Subject, [CancellationToken.None]);
        task.GetAwaiter().GetResult();
    }

    private void Complete() {
        var task = (Task)CompleteMethod.Invoke(Subject, [CancellationToken.None]);
        task.GetAwaiter().GetResult();
    }

    [SetUp]
    public void SetUp() {
        Lines = [];
        Subject = new TextLineBuilder(onLine: Lines.Add);
    }

    [Test]
    public void Add_SplitsLinesWithinOneChunk() {
        Add("one\ntwo\nthree");
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "one", "two", "three" }));
    }

    [Test]
    public void Add_StripsCarriageReturnsWithinOneChunk() {
        Add("one\r\ntwo\r\n");
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "one", "two" }));
    }

    [Test]
    public void Add_FinishesTheBufferedLineWhenTheNextChunkStartsWithALineFeed() {
        Add("one");
        Add("\ntwo");
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "one", "two" }));
    }

    [Test]
    public void Add_FinishesTheBufferedLineWhenTheLineBreakIsSplitAcrossChunks() {
        Add("one\r");
        Add("\ntwo");
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "one", "two" }));
    }

    [Test]
    public void Add_SplitsEveryLineWhenChunksHoldOneCharacterEach() {
        foreach (var c in "one\r\ntwo\nthree") {
            Add(c.ToString());
        }
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "one", "two", "three" }));
    }

    [Test]
    public void Add_BuildsEmptyLines() {
        Add("\n");
        Add("\r\n");
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "", "" }));
    }

    [Test]
    public void Add_KeepsACarriageReturnThatDoesNotEndALine() {
        Add("one\rtwo\n");
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "one\rtwo" }));
    }

    [Test]
    public void Add_KeepsACarriageReturnThatDoesNotEndALineAcrossChunks() {
        Add("one\r");
        Add("two\n");
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "one\rtwo" }));
    }

    [Test]
    public void Complete_BuildsATrailingCarriageReturn() {
        Add("one\r");
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "one\r" }));
    }

    [Test]
    public void Complete_BuildsNoLineWhenNothingIsBuffered() {
        Add("one\n");
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "one" }));
    }

    [Test]
    public void Clear_ForgetsABufferedCarriageReturn() {
        Add("one\r");
        Clear();
        Add("two\n");
        Complete();
        Assert.That(Lines, Is.EqualTo(new[] { "two" }));
    }

    [Test]
    public void Complete_InvokesOnComplete() {
        var completed = false;
        Subject = new TextLineBuilder(onLine: Lines.Add, onComplete: () => completed = true);
        Add("one");
        Complete();
        Assert.That(completed, Is.True);
        Assert.That(Lines, Is.EqualTo(new[] { "one" }));
    }

    [Test]
    public void Clear_InvokesOnClear() {
        var cleared = false;
        Subject = new TextLineBuilder(onLine: Lines.Add, onClear: () => cleared = true);
        Add("one");
        Clear();
        Assert.That(cleared, Is.True);
        Assert.That(Lines, Is.Empty);
    }
}
