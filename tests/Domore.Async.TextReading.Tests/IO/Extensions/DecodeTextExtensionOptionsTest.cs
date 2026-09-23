using Domore.Text;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO.Extensions;

[TestFixture]
public class DecodeTextExtensionOptionsTest {
    private sealed class Source : IStreamText {
        private readonly byte[] Bytes;

        public Source(byte[] bytes) {
            Bytes = bytes;
        }

        public long StreamLength => Bytes.Length;
        public Stream StreamText() => new MemoryStream(Bytes);
        public Task<IDisposable> StreamReady(CancellationToken cancellationToken) =>
            Task.FromResult(default(IDisposable));
    }

    private static readonly string Text = "h\u00e9llo \u4e2d\u6587\r\nsecond line";

    private static Source TextSource() =>
        new(Encoding.UTF8.GetBytes(Text));

    private string File;

    [SetUp]
    public void SetUp() {
        File = Path.GetTempFileName();
        System.IO.File.WriteAllText(File, Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    [TearDown]
    public void TearDown() {
        System.IO.File.Delete(File);
    }

    [Test]
    public async Task DecodeText_FreesOptionsItCreated() {
        var options = new DecodedTextOptions();
        var decoded = await DecodeTextExtension.DecodeText(TextSource(),
                                                           options,
                                                           disposeOptions: true,
                                                           opt => opt.ForStream(null, null),
                                                           CancellationToken.None);
        Assert.That(options.PoolCount, Is.Zero);
        Assert.That(decoded.Text(), Is.EqualTo(Text));
    }

    [Test]
    public void DecodeText_FreesOptionsItCreatedWhenDecodingThrows() {
        var options = new DecodedTextOptions();
        Assert.That(async () => await DecodeTextExtension.DecodeText(
                        TextSource(),
                        options,
                        disposeOptions: true,
                        opt => opt.ForStream((_, _) => throw new InvalidOperationException(), null),
                        CancellationToken.None),
                    Throws.InstanceOf<InvalidOperationException>());
        Assert.That(options.PoolCount, Is.Zero);
    }

    [Test]
    public async Task DecodeText_LeavesTheCallersOptionsForTheCallerToDispose() {
        var options = new DecodedTextOptions();
        using (options.Disposable()) {
            await TextSource().DecodeText((DecodedTextDelegate)null, options, CancellationToken.None);
            Assert.That(options.PoolCount, Is.GreaterThan(0));
        }
        Assert.That(options.PoolCount, Is.Zero);
    }

    [Test]
    public async Task DecodeText_ReturnsTextThatOutlivesDefaultOptions() {
        var builder = new Domore.Text.Builders.TextStringBuilder();
        var decoded = await new FileInfo(File).DecodeText(builder, options: null, CancellationToken.None);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Text(), Is.EqualTo(Text));
        Assert.That(builder.ToString(), Is.EqualTo(Text));
    }
}
