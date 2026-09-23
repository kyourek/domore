using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domore.Text;

namespace Domore.IO.Extensions;

[TestFixture]
public class DecodeTextExtensionFlushTest {
    private class Builder : DecodedTextBuilder {
        private readonly StringBuilder StringBuilder = new();

        protected override Task Add(ReadOnlyMemory<char> memory, CancellationToken cancellationToken) {
            StringBuilder.Append(new string(memory.Span));
            return Task.CompletedTask;
        }

        protected override Task Clear(CancellationToken cancellationToken) {
            StringBuilder.Clear();
            return Task.CompletedTask;
        }

        public string Text =>
            StringBuilder.ToString();
    }

    private string File;
    private Builder TextBuilder;

    private async Task<DecodedText> DecodeText(byte[] bytes, int streamBufferSize, params string[] encoding) {
        await System.IO.File.WriteAllBytesAsync(File, bytes);
        var options = new DecodedTextOptions {
            Encoding = new List<string>(encoding)
        };
        options.StreamBuffer.Size = streamBufferSize;
        return await new FileInfo(File).DecodeText(TextBuilder, options, CancellationToken.None);
    }

    [SetUp]
    public void SetUp() {
        File = Path.GetTempFileName();
        TextBuilder = new Builder();
    }

    [TearDown]
    public void TearDown() {
        System.IO.File.Delete(File);
    }

    [Test]
    public async Task DecodeText_TextRemainsValidAfterOptionsAreDisposed([Values(1, 3, 512)] int textBufferSize) {
        var expected = string.Concat(Enumerable.Repeat("h\u00e9llo \u4e2d\u6587\r\n", 50));
        await System.IO.File.WriteAllBytesAsync(File, Encoding.UTF8.GetBytes(expected));
        var options = new DecodedTextOptions {
            Encoding = new List<string> { "utf-8" }
        };
        options.TextBuffer.Size = textBufferSize;
        options.TextBuffer.Clear = true;
        options.TextBuffer.Shared = false;
        DecodedText decoded;
        using (options.Disposable()) {
            decoded = await new FileInfo(File).DecodeText(TextBuilder, options, CancellationToken.None);
        }
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Text(), Is.EqualTo(expected));
        Assert.That(decoded.TextLength, Is.EqualTo(expected.Length));
    }

    [Test]
    public async Task DecodeText_SucceedsWhenFinalUTF8SequenceIsComplete([Values(1, 2, 3, 512)] int streamBufferSize) {
        var bytes = Encoding.UTF8.GetBytes("abc\u00e9");
        var decoded = await DecodeText(bytes, streamBufferSize, "utf-8");
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.TextLength, Is.EqualTo(4));
        Assert.That(decoded.Text(), Is.EqualTo("abc\u00e9"));
        Assert.That(TextBuilder.Text, Is.EqualTo("abc\u00e9"));
    }

    [TestCase(new byte[] { 0x61, 0x62, 0x63, 0xC3 })]
    [TestCase(new byte[] { 0x61, 0x62, 0x63, 0xE4, 0xB8 })]
    [TestCase(new byte[] { 0x61, 0x62, 0x63, 0xF0, 0x9F, 0x98 })]
    public async Task DecodeText_FailsWhenFinalUTF8SequenceIsTruncated(byte[] bytes) {
        var decoded = await DecodeText(bytes, 512, "utf-8");
        Assert.That(decoded, Is.Null);
    }

    [Test]
    public async Task DecodeText_FailsWhenFinalUTF8SequenceIsTruncatedWithSmallStreamBuffer([Values(1, 2, 3)] int streamBufferSize) {
        var bytes = new byte[] { 0x61, 0x62, 0x63, 0xF0, 0x9F, 0x98 };
        var decoded = await DecodeText(bytes, streamBufferSize, "utf-8");
        Assert.That(decoded, Is.Null);
    }

    [Test]
    public async Task DecodeText_FailsWhenTheOnlyBytesAreATruncatedUTF8Sequence() {
        var bytes = new byte[] { 0xC3 };
        var decoded = await DecodeText(bytes, 512, "utf-8");
        Assert.That(decoded, Is.Null);
    }

    [Test]
    public async Task DecodeText_UsesAnotherEncodingWhenFinalUTF8SequenceIsTruncated() {
        var bytes = new byte[] { 0x61, 0x62, 0x63, 0xC3 };
        var decoded = await DecodeText(bytes, 512, "utf-8", "iso-8859-1");
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.EncodingName, Is.EqualTo(Encoding.GetEncoding("iso-8859-1").EncodingName));
        Assert.That(decoded.EncodingWebName, Is.EqualTo(Encoding.GetEncoding("iso-8859-1").WebName));
        Assert.That(decoded.Text(), Is.EqualTo("abc\u00c3"));
        Assert.That(TextBuilder.Text, Is.EqualTo("abc\u00c3"));
    }

    [Test]
    public async Task DecodeText_FailsWhenFinalUTF16SequenceIsTruncatedAfterPreamble() {
        var bytes = Encoding.Unicode
            .GetPreamble()
            .Concat(Encoding.Unicode.GetBytes("ab"))
            .Concat(new byte[] { 0x63 })
            .ToArray();
        var decoded = await DecodeText(bytes, 512, "utf-8");
        Assert.That(decoded, Is.Null);
    }

    [Test]
    public async Task DecodeText_FailsWhenFinalUTF16SurrogateIsTruncatedAfterPreamble() {
        var bytes = Encoding.Unicode
            .GetPreamble()
            .Concat(Encoding.Unicode.GetBytes("ab"))
            .Concat(new byte[] { 0x3d, 0xd8 })
            .ToArray();
        var decoded = await DecodeText(bytes, 512, "utf-8");
        Assert.That(decoded, Is.Null);
    }

    [Test]
    public async Task DecodeText_SucceedsWhenFinalUTF16SurrogateIsCompleteAfterPreamble() {
        var text = "ab\U0001f600";
        var bytes = Encoding.Unicode
            .GetPreamble()
            .Concat(Encoding.Unicode.GetBytes(text))
            .ToArray();
        var decoded = await DecodeText(bytes, 1, "utf-8");
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.EncodingName, Is.EqualTo(Encoding.Unicode.EncodingName));
        Assert.That(decoded.Text(), Is.EqualTo(text));
    }

    [Test]
    public async Task DecodeText_UsesReplacementFallbackWhenFinalUTF8SequenceIsTruncated() {
        var bytes = new byte[] { 0x61, 0x62, 0x63, 0xC3 };
        await System.IO.File.WriteAllBytesAsync(File, bytes);
        var options = new DecodedTextOptions {
            Encoding = new List<string> { "utf-8" },
            EncodingFallback = new Dictionary<string, string> { { "utf-8", "?" } }
        };
        var decoded = await new FileInfo(File).DecodeText(TextBuilder, options, CancellationToken.None);
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Text(), Is.EqualTo("abc?"));
        Assert.That(TextBuilder.Text, Is.EqualTo("abc?"));
    }
}
