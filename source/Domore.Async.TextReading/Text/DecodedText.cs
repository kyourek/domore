using System;
using System.Buffers;
using System.Text;

namespace Domore.Text;

/// <summary>
/// Text decoded from a stream by one candidate encoding.
/// </summary>
/// <remarks>
/// The properties reflect the decoder as it is when they are read, not when this object was
/// created, so an instance received while decoding is in progress may report later progress.
/// </remarks>
public sealed class DecodedText {
    internal ReadOnlySequence<char> Sequence =>
        Decoder.TextSequence;

    internal TextDecoder Decoder { get; }

    internal DecodedText(TextDecoder decoder) {
        Decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
    }

    /// <summary>
    /// Gets an object that identifies the candidate encoding that produced this text. It is the
    /// same for every update from that candidate.
    /// </summary>
    public object State =>
        Decoder;

    /// <summary>
    /// Gets whether the candidate encoding is still decoding.
    /// </summary>
    public bool Running =>
        Decoder.States == TextDecoderStates.Running;

    /// <summary>
    /// Gets whether decoding with the candidate encoding failed, for example because the bytes
    /// were not valid in that encoding.
    /// </summary>
    public bool Error =>
        Decoder.States.HasFlag(TextDecoderStates.Error);

    /// <summary>
    /// Gets whether decoding with the candidate encoding was canceled.
    /// </summary>
    public bool Canceled =>
        Decoder.States.HasFlag(TextDecoderStates.Canceled);

    /// <summary>
    /// Gets whether the candidate encoding has finished decoding, whether or not it succeeded.
    /// </summary>
    public bool Complete =>
        Decoder.States.HasFlag(TextDecoderStates.Complete);

    /// <summary>
    /// Gets whether the candidate encoding decoded every byte of the stream.
    /// </summary>
    public bool Success =>
        Decoder.States.HasFlag(TextDecoderStates.Success);

    /// <summary>
    /// Gets the display name of the encoding used, such as "Unicode (UTF-8)". This differs
    /// from the candidate encoding when a byte order mark names another encoding.
    /// </summary>
    public string EncodingName =>
        Decoder.EncodingUsedName;

    /// <summary>
    /// Gets the web name of the encoding used, such as "utf-8". This differs from the
    /// candidate encoding when a byte order mark names another encoding.
    /// </summary>
    public string EncodingWebName =>
        Decoder.EncodingUsedWebName;

    /// <summary>
    /// Gets the number of characters decoded so far.
    /// </summary>
    public long TextLength =>
        Decoder.TextLength;

    /// <summary>
    /// Gets the text decoded so far.
    /// </summary>
    /// <remarks>
    /// The text of the result returned by DecodeText remains valid after the
    /// <see cref="DecodedTextOptions"/> are disposed. DecodedText instances received in progress
    /// callbacks for other candidates read from buffers that are returned to their pool when
    /// the options are disposed.
    /// </remarks>
    public string Text() {
        var text = Decoder.TextWinner;
        if (text is not null) {
            return text;
        }
        var builder = new StringBuilder();
        var sequence = Sequence;
        foreach (var memory in sequence) {
            builder.Append(memory);
        }
        return builder.ToString();
    }
}
