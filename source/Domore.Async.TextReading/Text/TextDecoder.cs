using Domore.Buffers;
using Domore.IO;
using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Domore.Text;

internal sealed class TextDecoder {
    private readonly Encoding Encoding;
    private readonly TextBufferWriter BufferWriter;
    private readonly StreamTextReader StreamReader;
    private readonly Channel<DecodedText> Ch = Channel.CreateUnbounded<DecodedText>(new UnboundedChannelOptions {
        SingleReader = true,
        SingleWriter = true
    });

    private SequenceUpdatedEventArgs<byte> Event;

    private DecodedText Decoded() {
        return new DecodedText(this);
    }

    private TextDecoderStates Complete(TextDecoderStates state, Exception exception = null) {
        Volatile.Write(ref Event, null);
        Exception = exception;
        ByteSequence.Updated -= ByteSequence_Updated;
        BufferWriter.Complete();
        States = state | TextDecoderStates.Complete;
        Ch.Writer.TryWrite(Decoded());
        return States;
    }

    private TextDecoderStates Update() {
        if (States.HasFlag(TextDecoderStates.Complete)) {
            return States;
        }
        lock (Ch) {
            // Each event carries every byte read so far, so only the latest one needs decoding.
            var e = Interlocked.Exchange(ref Event, null);
            if (States.HasFlag(TextDecoderStates.Complete)) {
                return States;
            }
            if (CancellationToken.IsCancellationRequested) {
                return Complete(TextDecoderStates.Canceled);
            }
            if (e is null) {
                return States;
            }
            var sequence = e.Sequence;
            var sequenceComplete = e.Complete;
            var writer = BufferWriter;
            var written = writer.Written;
            try {
                StreamReader.Decode(sequence, BufferWriter, complete: sequenceComplete);
                if (sequenceComplete) {
                    StreamReader.Flush(BufferWriter);
                }
            }
            catch (Exception ex) {
                return Complete(TextDecoderStates.Error, ex);
            }
            if (sequenceComplete) {
                return Complete(TextDecoderStates.Success);
            }
            if (writer.Written > written) {
                Ch.Writer.TryWrite(Decoded());
            }
            return States = TextDecoderStates.Running;
        }
    }

    private void TryUpdate() {
        var exception = default(Exception);
        try {
            Update();
        }
        catch (Exception ex) {
            exception = ex;
        }
        if (exception is not null) {
            lock (Ch) {
                if (States.HasFlag(TextDecoderStates.Complete)) {
                    return;
                }
                try {
                    Complete(TextDecoderStates.Error, exception);
                }
                catch (Exception ex) {
                    /*
                     * Faulting the channel surfaces the failure to the caller awaiting Decode.
                     */
                    Ch.Writer.TryComplete(new AggregateException(exception, ex));
                }
            }
        }
    }

    private void ByteSequence_Updated(object sender, SequenceUpdatedEventArgs<byte> e) {
        Volatile.Write(ref Event, e);
        Task.Run(TryUpdate);
    }

    internal void Win() {
        if (!States.HasFlag(TextDecoderStates.Success)) {
            throw new InvalidOperationException();
        }
        var sequence = BufferWriter.Sequence;
        var text = string.Create(checked((int)sequence.Length), sequence, static (span, s) => s.CopyTo(span));
        Volatile.Write(ref _TextWinner, text);
    }

    internal string TextWinner => Volatile.Read(ref _TextWinner);
    private string _TextWinner;

    public ReadOnlySequence<char> TextSequence =>
        TextWinner is { } text
            ? new ReadOnlySequence<char>(text.AsMemory())
            : BufferWriter.Sequence;

    public long TextLength =>
        BufferWriter.Written;

    public string EncodingUsedName =>
        StreamReader.EncodingName;

    public string EncodingUsedWebName =>
        StreamReader.EncodingWebName;

    public SequenceUpdater<byte> ByteSequence { get; }
    public Exception Exception { get; private set; }
    public TextDecoderStates States { get; private set; }
    public CancellationToken CancellationToken { get; }

    public string EncodingName { get; }
    public string ReplacementFallback { get; }
    public BufferPool<char> BufferPool { get; }

    public TextDecoder(string encodingName,
                       string replacementFallback,
                       SequenceUpdater<byte> byteSequence,
                       BufferPool<char> bufferPool,
                       CancellationToken cancellationToken) {
        ByteSequence = byteSequence ?? throw new ArgumentNullException(nameof(byteSequence));
        ByteSequence.Updated += ByteSequence_Updated;
        BufferPool = bufferPool;
        BufferWriter = new TextBufferWriter(BufferPool);
        EncodingName = encodingName;
        ReplacementFallback = replacementFallback;
        Encoding = Encoding.GetEncoding(EncodingName, EncoderFallback.ExceptionFallback, ReplacementFallback == null
            ? new DecoderExceptionFallback()
            : new DecoderReplacementFallback(ReplacementFallback));
        StreamReader = new StreamTextReader(Encoding);
        CancellationToken = cancellationToken;
    }

    public async Task<DecodedText> Decode(CancellationToken cancellationToken) {
        var wait = await Ch.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
        if (wait == false) {
            return Decoded();
        }
        var read = Ch.Reader.TryRead(out var item);
        if (read == false) {
            return Decoded();
        }
        if (item.Complete) {
            lock (Ch) {
                Ch.Writer.TryComplete();
            }
        }
        return item;
    }
}
