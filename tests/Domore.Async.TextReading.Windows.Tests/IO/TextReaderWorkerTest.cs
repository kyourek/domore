using Domore.Text;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

[TestFixture]
public class TextReaderWorkerTest {
    private sealed class RecordingBuilder : DecodedTextBuilder {
        public StringBuilder Text { get; } = new();

        protected override Task Clear(CancellationToken cancellationToken) {
            Text.Clear();
            return Task.CompletedTask;
        }

        protected override Task Add(ReadOnlyMemory<char> memory, CancellationToken cancellationToken) {
            Text.Append(memory.Span);
            return Task.CompletedTask;
        }
    }

    private static readonly byte[] Latin1Cafe = [0x63, 0x61, 0x66, 0xE9];

    private TextReaderWorker Subject;

    [SetUp]
    public void SetUp() {
        Subject = new TextReaderWorker { Enabled = true };
    }

    [Test]
    public void Enabled_IsFalseByDefault() {
        Assert.That(new TextReaderWorker().Enabled, Is.False);
    }

    [Test]
    public async Task Refresh_DecodesSource() {
        Subject.Source = new TestStreamText("hello world");
        var decoded = await Subject.Refresh(null, CancellationToken.None);
        Assert.That(decoded?.Success, Is.True);
        Assert.That(decoded.Text(), Is.EqualTo("hello world"));
    }

    [Test]
    public async Task Refresh_SendsTextToBuilder() {
        var builder = new RecordingBuilder();
        Subject.Source = new TestStreamText("hello builder");
        await Subject.Refresh(builder, CancellationToken.None);
        Assert.That(builder.Text.ToString(), Is.EqualTo("hello builder"));
    }

    [Test]
    public async Task Refresh_ReturnsNullWhenDisabled() {
        var source = new TestStreamText("hello");
        Subject.Source = source;
        Subject.Enabled = false;
        Assert.That(await Subject.Refresh(null, CancellationToken.None), Is.Null);
        Assert.That(source.StreamTextCalls, Is.Zero);
    }

    [Test]
    public async Task Refresh_ReturnsNullWhenSourceIsNull() {
        Subject.Source = null;
        Assert.That(await Subject.Refresh(null, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task Refresh_UsesOptions() {
        Subject.Source = new TestStreamText(Latin1Cafe);
        Subject.Options = new DecodedTextOptions { Encoding = ["iso-8859-1"] };
        var decoded = await Subject.Refresh(null, CancellationToken.None);
        Assert.That(decoded?.Success, Is.True);
        Assert.That(decoded.Text(), Is.EqualTo("café"));
    }

    [Test]
    public async Task Refresh_IsUnsuccessfulWhenDefaultEncodingFails() {
        Subject.Source = new TestStreamText(Latin1Cafe);
        var decoded = await Subject.Refresh(null, CancellationToken.None);
        Assert.That(decoded?.Success, Is.Not.True);
    }

    [Test]
    public async Task Refresh_DoesNotQueryLengthWithoutMax() {
        var source = new TestStreamText("hello");
        Subject.Source = source;
        await Subject.Refresh(null, CancellationToken.None);
        Assert.That(source.StreamLengthCalls, Is.Zero);
    }

    [Test]
    public async Task Refresh_ReturnsNullWhenLengthExceedsMax() {
        var source = new TestStreamText("hello");
        Subject.Source = source;
        Subject.SourceLengthMax = 4;
        Assert.That(await Subject.Refresh(null, CancellationToken.None), Is.Null);
        Assert.That(source.StreamLengthCalls, Is.EqualTo(1));
        Assert.That(source.StreamTextCalls, Is.Zero);
    }

    [Test]
    public async Task Refresh_DecodesWhenLengthEqualsMax() {
        Subject.Source = new TestStreamText("hello");
        Subject.SourceLengthMax = 5;
        var decoded = await Subject.Refresh(null, CancellationToken.None);
        Assert.That(decoded?.Text(), Is.EqualTo("hello"));
    }

    [Test]
    public async Task Refresh_UsesReportedLengthForMax() {
        Subject.Source = new TestStreamText("hello") { Length = 1000 };
        Subject.SourceLengthMax = 999;
        Assert.That(await Subject.Refresh(null, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task Refresh_DecodesWhenLengthTaskIsNull() {
        Subject.Source = new TestStreamText("hello") { LengthTaskIsNull = true };
        Subject.SourceLengthMax = 1;
        var decoded = await Subject.Refresh(null, CancellationToken.None);
        Assert.That(decoded?.Text(), Is.EqualTo("hello"));
    }

    [Test]
    public async Task Refresh_ReturnsNullWhenLengthThrows() {
        var source = new TestStreamText("hello") { LengthException = new FileNotFoundException() };
        Subject.Source = source;
        Subject.SourceLengthMax = 100;
        Assert.That(await Subject.Refresh(null, CancellationToken.None), Is.Null);
        Assert.That(source.StreamTextCalls, Is.Zero);
    }

    [Test]
    public async Task Refresh_ReturnsNullWhenStreamThrows() {
        Subject.Source = new TestStreamText("hello") { StreamException = new IOException() };
        Assert.That(await Subject.Refresh(null, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task Refresh_ReturnsNullWhenAlreadyCanceled() {
        var source = new TestStreamText("hello");
        Subject.Source = source;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.That(await Subject.Refresh(null, cancellation.Token), Is.Null);
        Assert.That(source.StreamTextCalls, Is.Zero);
    }

    [Test]
    public async Task Refresh_ReturnsNullWhenCanceledWhileWorking() {
        var source = new TestStreamText("hello") { WaitUntilCanceled = true };
        Subject.Source = source;
        using var cancellation = new CancellationTokenSource();
        var refresh = Subject.Refresh(null, cancellation.Token);
        cancellation.CancelAfter(50);
        Assert.That(await refresh.WaitAsync(TimeSpan.FromSeconds(10)), Is.Null);
        Assert.That(source.ReadyCanceled.IsCompleted, Is.True);
        Assert.That(source.StreamTextCalls, Is.Zero);
    }

    [Test]
    public async Task Refresh_UsesSourceCapturedAtStart() {
        Subject.Source = new TestStreamText("first");
        var refresh = Subject.Refresh(null, CancellationToken.None);
        Subject.Source = new TestStreamText("second");
        var decoded = await refresh;
        Assert.That(decoded?.Text(), Is.EqualTo("first"));
    }
}
