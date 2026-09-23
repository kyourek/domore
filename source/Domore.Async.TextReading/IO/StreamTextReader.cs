using Domore.Text;
using System;
using System.Buffers;
using System.Text;

namespace Domore.IO;

internal class StreamTextReader {
    private long BytesUsed;
    private long CharsUsed;
    private bool PreambleDetected;
    private StreamTextReader Agent;

    private TextEncodingDetector EncodingDetector => field ??= new();
    private Decoder Decoder => field ??= Encoding.GetDecoder();

    public string EncodingName => Agent is null
        ? Encoding.EncodingName
        : Agent.EncodingName;

    public string EncodingWebName => Agent is null
        ? Encoding.WebName
        : Agent.EncodingWebName;

    public Encoding Encoding { get; }

    public StreamTextReader(Encoding encoding) {
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
    }

    public void Decode(in ReadOnlySequence<byte> sequence, IBufferWriter<char> writer, bool complete) {
        if (Agent != null) {
            Agent.Decode(sequence, writer, complete);
            return;
        }
        if (PreambleDetected == false) {
            var detected = EncodingDetector.TryDetect(sequence,
                                                      complete,
                                                      out var encoding,
                                                      out var preambleLength);
            if (detected == false) {
                return;
            }
            PreambleDetected = true;
            if (encoding is not null) {
                if (encoding.CodePage == Encoding.CodePage) {
                    BytesUsed = preambleLength;
                }
                else {
                    /*
                     * The detected encoding always throws on invalid bytes, so it takes on
                     * the fallback that was configured for this reader's encoding.
                     */
                    var
                    agentEncoding = (Encoding)encoding.Clone();
                    agentEncoding.DecoderFallback = Encoding.DecoderFallback;
                    Agent = new(agentEncoding) {
                        PreambleDetected = true,
                        BytesUsed = preambleLength,
                    };
                    Agent.Decode(sequence, writer, complete);
                    return;
                }
            }
        }
        if (sequence.Length > BytesUsed) {
            var slice = sequence.Slice(BytesUsed);
            if (slice.Length > 0) {
                foreach (var memory in slice) {
                    if (memory.IsEmpty == false) {
                        var span = memory.Span;
                        var spanLength = span.Length;
                        Decoder.Convert(span, writer, flush: false, out var charsUsed, out _);
                        CharsUsed += charsUsed;
                        BytesUsed += spanLength;
                    }
                }
            }
        }
    }

    public void Flush(IBufferWriter<char> writer) {
        if (Agent != null) {
            Agent.Flush(writer);
            return;
        }
        Decoder.Convert(ReadOnlySpan<byte>.Empty, writer, flush: true, out var charsUsed, out _);
        CharsUsed += charsUsed;
    }
}
