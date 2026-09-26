using Domore.IO;
using Domore.IO.Extensions;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Text.Builders;

[TestFixture]
public class TextStreamBuilderTest {
    private sealed class FailingStream : MemoryStream {
        public override Task<int> ReadAsync(byte[] buffer,
                                            int offset,
                                            int count,
                                            CancellationToken cancellationToken) =>
            Task.FromException<int>(new IOException("Read failed."));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer,
                                                 CancellationToken cancellationToken = default) =>
            ValueTask.FromException<int>(new IOException("Read failed."));
    }

    private sealed class Source : IStreamText {
        private readonly Func<Stream> Stream;

        public Source(Func<Stream> stream) {
            Stream = stream;
        }

        public Task<long> StreamLength(CancellationToken cancellationToken) => Task.FromResult(0L);
        public Stream StreamText() => Stream();
        public Task<IDisposable> StreamReady(CancellationToken cancellationToken) =>
            Task.FromResult(default(IDisposable));
    }

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private TextStreamBuilder Subject;
    private string File;

    private static async Task<string> Read(TextStreamBuilder builder) {
        var text = new StringBuilder();
        await foreach (var item in builder.Read(CancellationToken.None)) {
            if (item.Clear) {
                text.Clear();
            }
            else {
                text.Append(item.Text);
            }
        }
        return text.ToString();
    }

    private Task<string> Reading() {
        return Task.Run(() => Read(Subject));
    }

    private static async Task<string> Completed(Task<string> reading) {
        var completed = await Task.WhenAny(reading, Task.Delay(Timeout));
        Assert.That(completed, Is.SameAs(reading), "TextStreamBuilder.Read did not complete.");
        return await reading;
    }

    private static DecodedTextOptions Options(params string[] encoding) {
        return new DecodedTextOptions { Encoding = [.. encoding] };
    }

    [SetUp]
    public void SetUp() {
        File = Path.GetTempFileName();
        Subject = new TextStreamBuilder();
    }

    [TearDown]
    public void TearDown() {
        System.IO.File.Delete(File);
    }

    [Test]
    public async Task Read_YieldsTheDecodedText() {
        var reading = Reading();
        System.IO.File.WriteAllText(File, "hello\r\nworld");
        var decoded = await new FileInfo(File).DecodeText(Subject, Options("utf-8"), CancellationToken.None);
        Assert.That(decoded.Success, Is.True);
        Assert.That(await Completed(reading), Is.EqualTo("hello\r\nworld"));
    }

    [Test]
    public async Task Read_CompletesWhenNoCandidateEncodingSucceeds() {
        var reading = Reading();
        System.IO.File.WriteAllBytes(File, new byte[] { 0x61, 0xFF, 0x62 });
        var decoded = await new FileInfo(File).DecodeText(Subject, Options("utf-8"), CancellationToken.None);
        Assert.That(decoded, Is.Null);
        Assert.That(await Completed(reading), Is.Empty);
    }

    [Test]
    public void Read_ThrowsWhenTheFileCannotBeOpened() {
        var reading = Reading();
        System.IO.File.Delete(File);
        Assert.That(async () => await new FileInfo(File).DecodeText(Subject, Options("utf-8"), CancellationToken.None),
                    Throws.InstanceOf<FileNotFoundException>());
        Assert.That(async () => await Completed(reading),
            Throws.InstanceOf<FileNotFoundException>());
    }

    [Test]
    public void Read_ThrowsWhenTheStreamThrows() {
        var reading = Reading();
        var source = new Source(() => new FailingStream());
        Assert.That(async () => await source.DecodeText(Subject, Options("utf-8"), CancellationToken.None),
                    Throws.InstanceOf<IOException>());
        Assert.That(async () => await Completed(reading),
            Throws.InstanceOf<IOException>());
    }

    [Test]
    public void Read_ThrowsWhenDecodingIsCanceled() {
        var reading = Reading();
        System.IO.File.WriteAllText(File, "hello");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.That(async () => await new FileInfo(File).DecodeText(Subject, Options("utf-8"), cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(async () => await Completed(reading),
            Throws.InstanceOf<OperationCanceledException>());
    }
}
