using Domore.Buffers;
using NUnit.Framework;
using System;
using System.Buffers;

namespace Domore.Text;

[TestFixture]
public class TextBufferWriterTest {
    private BufferPool<char> Pool;
    private TextBufferWriter Subject;
    private IBufferWriter<char> Writer => Subject;

    private void Write(string text) {
        var span = Writer.GetSpan(text.Length);
        text.AsSpan().CopyTo(span);
        Writer.Advance(text.Length);
    }

    private string Text() {
        var text = "";
        foreach (var memory in Subject.Sequence) {
            text += new string(memory.Span);
        }
        return text;
    }

    [SetUp]
    public void SetUp() {
        Pool = new BufferOptions { Size = 8 }.CreatePool<char>();
        Subject = new TextBufferWriter(Pool);
    }

    [TearDown]
    public void TearDown() {
        Pool.Free();
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(100)]
    [TestCase(70000)]
    public void GetSpan_ReturnsAtLeastTheRequestedSize(int sizeHint) {
        var span = Writer.GetSpan(sizeHint);
        Assert.That(span.Length, Is.GreaterThanOrEqualTo(sizeHint));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(100)]
    [TestCase(70000)]
    public void GetMemory_ReturnsAtLeastTheRequestedSize(int sizeHint) {
        var memory = Writer.GetMemory(sizeHint);
        Assert.That(memory.Length, Is.GreaterThanOrEqualTo(sizeHint));
    }

    [Test]
    public void GetSpan_ReturnsAtLeastTheRequestedSizeWhenTheCurrentBufferIsNearlyFull() {
        var first = Writer.GetSpan(1);
        Writer.Advance(first.Length - 1);
        var second = Writer.GetSpan(64);
        Assert.That(second.Length, Is.GreaterThanOrEqualTo(64));
    }

    [Test]
    public void GetMemory_ReturnsAtLeastTheRequestedSizeWhenTheCurrentBufferIsNearlyFull() {
        var first = Writer.GetMemory(1);
        Writer.Advance(first.Length - 1);
        var second = Writer.GetMemory(64);
        Assert.That(second.Length, Is.GreaterThanOrEqualTo(64));
    }

    [Test]
    public void GetSpan_ReturnsANonEmptySpanForNoSizeHint() {
        var span = Writer.GetSpan(0);
        Assert.That(span.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetMemory_ReturnsANonEmptyMemoryForNoSizeHint() {
        var memory = Writer.GetMemory(0);
        Assert.That(memory.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetSpan_ThrowsForANegativeSizeHint() {
        Assert.That(() => {
            var span = Writer.GetSpan(-1);
            _ = span.Length;
        }, Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Advance_BuildsTheWrittenTextAcrossBuffers() {
        Write("hello ");
        Write("world, this is longer than the pool size");
        Write("!");
        Assert.That(Text(), Is.EqualTo("hello world, this is longer than the pool size!"));
        Assert.That(Subject.Written, Is.EqualTo(47));
    }

    [Test]
    public void Advance_BuildsTheWrittenTextWithoutASizeHint() {
        for (var i = 0; i < 50; i++) {
            var span = Writer.GetSpan(0);
            span[0] = (char)('a' + (i % 26));
            Writer.Advance(1);
        }
        Assert.That(Text().Length, Is.EqualTo(50));
        Assert.That(Subject.Written, Is.EqualTo(50));
    }

    private int SegmentCount() {
        var count = 0;
        foreach (var _ in Subject.Sequence) {
            count++;
        }
        return count;
    }

    [Test]
    public void Advance_GrowsTheCurrentSegmentForContiguousWrites() {
        var capacity = Writer.GetSpan(1).Length;
        for (var i = 0; i < capacity; i++) {
            var span = Writer.GetSpan(1);
            span[0] = (char)('a' + (i % 26));
            Writer.Advance(1);
        }
        Assert.That(SegmentCount(), Is.EqualTo(1));
        Assert.That(Subject.Sequence.Length, Is.EqualTo(capacity));
    }

    [Test]
    public void Advance_StartsASegmentForEachBuffer() {
        var capacity = Writer.GetSpan(1).Length;
        Write(new string('a', capacity));
        Write("b");
        Write("c");
        Assert.That(SegmentCount(), Is.EqualTo(2));
        Assert.That(Text(), Is.EqualTo(new string('a', capacity) + "bc"));
    }

    [Test]
    public void Advance_LeavesPreviouslyPublishedSequencesUnchanged() {
        Write("ab");
        var before = Subject.Sequence;
        Write("cd");
        Assert.That(new string(before.ToArray()), Is.EqualTo("ab"));
        Assert.That(new string(Subject.Sequence.ToArray()), Is.EqualTo("abcd"));
    }

    [Test]
    public void Advance_ThrowsWhenCountExceedsTheAvailableSpace() {
        var span = Writer.GetSpan(1);
        var length = span.Length;
        Assert.That(() => Writer.Advance(length + 1), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}
