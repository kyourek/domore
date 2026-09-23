using Domore.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Text;

internal sealed class TextDecoders {
    private static DecodedText Winner(DecodedText decoded) {
        decoded?.Decoder?.Win();
        return decoded;
    }

    private List<TextDecoder> Encoders(SequenceUpdater<byte> byteSequence, CancellationToken cancellationToken) {
        return [.. Encoding.Select(e => new TextDecoder(
            encodingName: e,
            replacementFallback: EncodingFallback.TryGetValue(e, out var fallback)
                ? fallback
                : null,
            byteSequence: byteSequence,
            bufferPool: BufferPool,
            cancellationToken: cancellationToken))];
    }

    public DecodedTextDelegate Decoded { get; set; }
    public BufferPool<char> BufferPool { get; set; }
    public bool ContinueOnCapturedContext { get; set; }

    public IReadOnlyList<string> Encoding {
        get => field ??= new List<string>();
        set;
    }

    public IReadOnlyDictionary<string, string> EncodingFallback {
        get => field ??= new Dictionary<string, string>();
        set;
    }

    public async Task<DecodedText> Decode(SequenceUpdater<byte> byteSequence, CancellationToken cancellationToken) {
        var running = Encoders(byteSequence, cancellationToken);
        var success = new List<DecodedText>(running.Count);
        var order = new List<string>(running.Select(decoder => decoder.EncodingName));
        var pending = new Dictionary<TextDecoder, Task<DecodedText>>(running.Count);
        for (; ; ) {
            if (running.Count == 0) {
                if (success.Count == 0) {
                    return null;
                }
                if (success.Count == 1) {
                    return Winner(success[0]);
                }
                foreach (var encoding in order) {
                    foreach (var item in success) {
                        if (item.Decoder.EncodingName == encoding) {
                            return Winner(item);
                        }
                    }
                }
                return Winner(success[0]);
            }
            foreach (var decoder in running) {
                if (pending.ContainsKey(decoder) != true) {
                    pending[decoder] = decoder.Decode(cancellationToken);
                }
            }
            var task = await Task.WhenAny(pending.Values).ConfigureAwait(ContinueOnCapturedContext);
            var decoded = await task.ConfigureAwait(ContinueOnCapturedContext);
            cancellationToken.ThrowIfCancellationRequested();
            pending.Remove(decoded.Decoder);
            if (decoded.Complete) {
                running.Remove(decoded.Decoder);
            }
            if (decoded.Success) {
                success.Add(decoded);
            }
            if (!decoded.Error && !decoded.Canceled) {
                var handler = Decoded;
                if (handler is not null) {
                    var handlerTask = handler(decoded, cancellationToken);
                    if (handlerTask is not null) {
                        await handlerTask.ConfigureAwait(ContinueOnCapturedContext);
                    }
                }
            }
        }
    }
}
