using Domore.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO.Extensions;

[TestFixture]
public class DecodeTextExtensionPreambleTest {
    /// <summary>
    /// A stream that hands out at most a few bytes per read, so that a preamble
    /// is split across several reads no matter how big the stream buffer is.
    /// </summary>
    private sealed class ThrottledStream : Stream {
        private const int LongestPreamble = 4;

        private int Cursor;

        private readonly byte[] Bytes;
        private readonly int BytesPerRead;

        public ThrottledStream(byte[] bytes, int bytesPerRead) {
            Bytes = bytes;
            BytesPerRead = bytesPerRead;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => Bytes.Length;

        public override long Position {
            get => Cursor;
            set => throw new NotSupportedException();
        }

        public override void Flush() {
        }

        public override int Read(byte[] buffer, int offset, int count) {
            var length = Math.Min(Math.Min(count, BytesPerRead), Bytes.Length - Cursor);
            Array.Copy(Bytes, Cursor, buffer, offset, length);
            Cursor += length;
            return length;
        }

        public override async Task<int> ReadAsync(byte[] buffer,
                                                  int offset,
                                                  int count,
                                                  CancellationToken cancellationToken) {
            if (Cursor <= LongestPreamble) {
                // Give the decoder a chance to run before the rest of the preamble arrives.
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }
            return Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class ThrottledSource : IStreamText {
        private readonly byte[] Bytes;
        private readonly int BytesPerRead;

        public ThrottledSource(byte[] bytes, int bytesPerRead) {
            Bytes = bytes;
            BytesPerRead = bytesPerRead;
        }

        public long StreamLength => Bytes.Length;
        public Stream StreamText() => new ThrottledStream(Bytes, BytesPerRead);
        public Task<IDisposable> StreamReady(CancellationToken cancellationToken) =>
            Task.FromResult(default(IDisposable));
    }

    private const string Text = "h\u00e9llo \u4e2d\u6587\r\nsecond line";

    private static byte[] Bytes(Encoding encoding, string text = Text) {
        return encoding
            .GetPreamble()
            .Concat(encoding.GetBytes(text))
            .ToArray();
    }

    private static async Task<DecodedText> DecodeText(byte[] bytes, int bytesPerRead, params string[] encoding) {
        return await DecodeTextWithFallback(bytes, bytesPerRead, fallback: null, encoding);
    }

    private static async Task<DecodedText> DecodeTextWithFallback(byte[] bytes,
                                                                  int bytesPerRead,
                                                                  string fallback,
                                                                  params string[] encoding) {
        var options = new DecodedTextOptions {
            Encoding = new List<string>(encoding)
        };
        if (fallback != null) {
            foreach (var e in encoding) {
                options.EncodingFallback[e] = fallback;
            }
        }
        using (options.Disposable()) {
            var source = new ThrottledSource(bytes, bytesPerRead);
            return await source.DecodeText((DecodedTextDelegate)null, options, CancellationToken.None);
        }
    }

    // A byte order mark that names an encoding other than the candidate, followed by
    // "a", a sequence that is invalid in the named encoding, and "b".
    private static IEnumerable<TestCaseData> InvalidAfterOtherPreamble() {
        yield return new TestCaseData(new byte[] { 0xEF, 0xBB, 0xBF, 0x61, 0xFF, 0x62 }, "utf-16")
            .SetName("UTF-8 preamble, utf-16 candidate");
        yield return new TestCaseData(new byte[] { 0xFF, 0xFE, 0x61, 0x00, 0x00, 0xD8, 0x62, 0x00 }, "utf-8")
            .SetName("UTF-16 LE preamble, utf-8 candidate");
    }

    [TestCaseSource(nameof(InvalidAfterOtherPreamble))]
    public async Task DecodeText_KeepsTheConfiguredFallbackWhenThePreambleNamesAnotherEncoding(byte[] bytes,
                                                                                            string candidate) {
        var decoded = await DecodeTextWithFallback(bytes, bytesPerRead: 512, fallback: "?", candidate);
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.EncodingWebName, Is.Not.EqualTo(Encoding.GetEncoding(candidate).WebName));
        Assert.That(decoded.Text(), Is.EqualTo("a?b"));
    }

    [TestCaseSource(nameof(InvalidAfterOtherPreamble))]
    public async Task DecodeText_FailsOnInvalidBytesAfterAnotherPreambleWithNoFallback(byte[] bytes,
                                                                                       string candidate) {
        var decoded = await DecodeText(bytes, bytesPerRead: 512, candidate);
        Assert.That(decoded, Is.Null);
    }

    [Test]
    public async Task DecodeText_SkipsTheUTF8PreambleWhenItIsSplitAcrossReads([Values(1, 2, 3, 4)] int bytesPerRead) {
        var bytes = Bytes(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        var decoded = await DecodeText(bytes, bytesPerRead, "utf-8");
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.EncodingName, Is.EqualTo(Encoding.UTF8.EncodingName));
        Assert.That(decoded.EncodingWebName, Is.EqualTo(Encoding.UTF8.WebName));
        Assert.That(decoded.Text(), Is.EqualTo(Text));
    }

    [Test]
    public async Task
    DecodeText_DetectsUTF32WhenOnlyThePartialPreambleIsAvailableAtFirst([Values(1, 2, 3)] int bytesPerRead) {
        var encoding = new UTF32Encoding(bigEndian: false, byteOrderMark: true);
        var bytes = Bytes(encoding);
        var decoded = await DecodeText(bytes, bytesPerRead, "utf-8");
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.EncodingName, Is.EqualTo(encoding.EncodingName));
        Assert.That(decoded.EncodingWebName, Is.EqualTo(encoding.WebName));
        Assert.That(decoded.Text(), Is.EqualTo(Text));
    }

    [Test]
    public async Task DecodeText_DetectsUTF16WhenThePreambleIsSplitAcrossReads([Values(1, 2, 3)] int bytesPerRead) {
        var encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
        var bytes = Bytes(encoding);
        var decoded = await DecodeText(bytes, bytesPerRead, "utf-8");
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.EncodingName, Is.EqualTo(encoding.EncodingName));
        Assert.That(decoded.Text(), Is.EqualTo(Text));
    }

    [Test]
    public async Task DecodeText_HonorsThePreambleForAnEncodingThatHasNone([Values(1, 512)] int bytesPerRead) {
        var encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
        var bytes = Bytes(encoding);
        var decoded = await DecodeText(bytes, bytesPerRead, "iso-8859-1");
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.EncodingName, Is.EqualTo(encoding.EncodingName));
        Assert.That(decoded.Text(), Is.EqualTo(Text));
    }

    [Test]
    public async Task
    DecodeText_DecodesTextWithNoPreambleThatIsShorterThanAnyPreamble([Values(1, 512)] int bytesPerRead) {
        var bytes = Encoding.UTF8.GetBytes("ab");
        var decoded = await DecodeText(bytes, bytesPerRead, "utf-8");
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Text(), Is.EqualTo("ab"));
    }

    [Test]
    public async Task DecodeText_DecodesAStreamThatHoldsNothingButAPreamble([Values(1, 512)] int bytesPerRead) {
        var bytes = Encoding.UTF8.GetPreamble();
        var decoded = await DecodeText(bytes, bytesPerRead, "utf-8");
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.EncodingName, Is.EqualTo(Encoding.UTF8.EncodingName));
        Assert.That(decoded.Text(), Is.Empty);
    }

    [Test]
    public async Task DecodeText_DecodesAnEmptyStream([Values(1, 512)] int bytesPerRead) {
        var decoded = await DecodeText([], bytesPerRead, "utf-8");
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Text(), Is.Empty);
    }
}
