using System;
using System.Buffers;
using System.Text;

namespace Domore.Text;

internal sealed class TextEncodingDetector {
    private const bool ThrowOnInvalid = true;
    private const int LongestPreamble = 4;

    private static readonly Preamble[] Preambles = [
        new([0xFF, 0xFE, 0x00, 0x00], () => new UTF32Encoding(bigEndian: false,
                                                              byteOrderMark: true,
                                                              throwOnInvalidCharacters: ThrowOnInvalid)),
        new([0x00, 0x00, 0xFE, 0xFF], () => new UTF32Encoding(bigEndian: true,
                                                              byteOrderMark: true,
                                                              throwOnInvalidCharacters: ThrowOnInvalid)),
        new([0xEF, 0xBB, 0xBF], () => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true,
                                                       throwOnInvalidBytes: ThrowOnInvalid)),
        new([0xFE, 0xFF], () => new UnicodeEncoding(bigEndian: true,
                                                    byteOrderMark: true,
                                                    throwOnInvalidBytes: ThrowOnInvalid)),
        new([0xFF, 0xFE], () => new UnicodeEncoding(bigEndian: false,
                                                    byteOrderMark: true,
                                                    throwOnInvalidBytes: ThrowOnInvalid))
    ];

    private static byte[] Slice(in ReadOnlySequence<byte> sequence) {
        var length = sequence.Length;
        if (length <= 0) {
            return [];
        }
        return sequence
            .Slice(0, Math.Min(LongestPreamble, length))
            .ToArray();
    }

    /// <summary>
    /// Looks for a byte order mark at the start of <paramref name="sequence"/>.
    /// </summary>
    /// <param name="sequence">The bytes read so far.</param>
    /// <param name="complete">Whether or not <paramref name="sequence"/> holds every byte of the stream.</param>
    /// <param name="encoding">The encoding of the detected byte order mark, or null if the bytes begin with no byte order mark.</param>
    /// <param name="preambleLength">The number of bytes occupied by the detected byte order mark.</param>
    /// <returns>
    /// True if the presence or absence of a byte order mark was determined. False if more bytes
    /// are needed, in which case no byte of <paramref name="sequence"/> may be decoded yet.
    /// </returns>
    public bool TryDetect(in ReadOnlySequence<byte> sequence, bool complete, out Encoding encoding, out int preambleLength) {
        encoding = null;
        preambleLength = 0;
        var slice = Slice(sequence);
        var length = slice.Length;
        if (complete == false) {
            foreach (var preamble in Preambles) {
                if (preamble.Undecided(slice, length)) {
                    return false;
                }
            }
        }
        foreach (var preamble in Preambles) {
            if (preamble.Matches(slice, length)) {
                encoding = preamble.Encoding();
                preambleLength = preamble.Bytes.Length;
                return true;
            }
        }
        return true;
    }

    private sealed class Preamble {
        public byte[] Bytes { get; }
        public Func<Encoding> Encoding { get; }

        public Preamble(byte[] bytes, Func<Encoding> encoding) {
            Bytes = bytes;
            Encoding = encoding;
        }

        public bool Matches(byte[] slice, int length) {
            var bytes = Bytes;
            if (length < bytes.Length) {
                return false;
            }
            for (var i = 0; i < bytes.Length; i++) {
                if (slice[i] != bytes[i]) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if this preamble is longer than the bytes read so far and
        /// every byte read so far matches it, meaning more bytes are needed before
        /// the preamble can be ruled in or out.
        /// </summary>
        public bool Undecided(byte[] slice, int length) {
            var bytes = Bytes;
            if (length >= bytes.Length) {
                return false;
            }
            for (var i = 0; i < length; i++) {
                if (slice[i] != bytes[i]) {
                    return false;
                }
            }
            return true;
        }
    }
}
