using Domore.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO.Extensions;

/// <summary>
/// Decodes text from streams and files.
/// </summary>
public static class DecodeTextExtension {
    private static Task<DecodedText> DecodeText(IStreamText source,
                                                DecodedTextOptions options,
                                                Func<DecodedTextOptions, StreamTextDecoder> decoder,
                                                CancellationToken cancellationToken) {
        return options is null
            ? DecodeText(source, new(), disposeOptions: true, decoder, cancellationToken)
            : DecodeText(source, options, disposeOptions: false, decoder, cancellationToken);
    }

    internal static async Task<DecodedText> DecodeText(IStreamText source,
                                                       DecodedTextOptions options,
                                                       bool disposeOptions,
                                                       Func<DecodedTextOptions, StreamTextDecoder> decoder,
                                                       CancellationToken cancellationToken) {
        if (decoder is null) {
            throw new ArgumentNullException(nameof(decoder));
        }
        if (source is null) {
            throw new ArgumentNullException(nameof(source));
        }
        if (options is null) {
            throw new ArgumentNullException(nameof(options));
        }
        /*
         * Options created on the caller's behalf are disposed here, since nothing else can.
         * The text of a successful decode is copied out of the buffers, so it outlives them.
         */
        var disposable = disposeOptions ? options.Disposable() : null;
        using (disposable) {
            var streamReady = source.StreamReady(cancellationToken);
            if (streamReady is null) {
                return null;
            }
            using (await streamReady) {
                await using (var stream = source.StreamText()) {
                    if (stream is null) {
                        return null;
                    }
                    var dec = decoder(options);
                    return await dec.Decode(stream, cancellationToken);
                }
            }
        }
    }

    private static async Task<DecodedText> DecodeText(Func<IStreamText> source,
                                                      DecodedTextBuilder builder,
                                                      DecodedTextOptions options,
                                                      CancellationToken cancellationToken) {
        if (builder is null) {
            return await DecodeText(source(), options, opt => opt.ForStream(null, null), cancellationToken);
        }
        try {
            var decoded = await DecodeText(source(),
                                           options,
                                           opt => opt.ForStream(builder.Decode, null),
                                           cancellationToken);
            await builder.Complete(decoded, cancellationToken);
            return decoded;
        }
        catch (Exception ex) {
            /*
             * Tell the builder, so that consumers such as TextStreamBuilder.Read do not wait forever.
             */
            await builder.Failed(ex);
            throw;
        }
    }

    /// <summary>
    /// Decodes the text of a stream, trying each candidate encoding in
    /// <see cref="DecodedTextOptions.Encoding"/>.
    /// </summary>
    /// <param name="source">The source of the stream to decode.</param>
    /// <param name="decoded">Called with the progress of each candidate encoding. May be null.</param>
    /// <param name="options">
    /// The options for the decode. If null, default options are used, and their buffers are
    /// returned to the pool when the decode finishes.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the decode.</param>
    /// <returns>
    /// The text of the first candidate encoding that succeeded, or null if none succeeded or there
    /// was no stream to decode.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public static Task<DecodedText> DecodeText(this IStreamText source,
                                               DecodedTextDelegate decoded,
                                               DecodedTextOptions options,
                                               CancellationToken cancellationToken) {
        return DecodeText(source, options, opt => opt.ForStream(decoded, null), cancellationToken);
    }

    /// <summary>
    /// Decodes the text of a stream, trying each candidate encoding in
    /// <see cref="DecodedTextOptions.Encoding"/>.
    /// </summary>
    /// <param name="source">The source of the stream to decode.</param>
    /// <param name="builder">
    /// Receives the decoded text as it becomes available, and is completed or failed when the
    /// decode finishes. May be null.
    /// </param>
    /// <param name="options">
    /// The options for the decode. If null, default options are used, and their buffers are
    /// returned to the pool when the decode finishes.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the decode.</param>
    /// <returns>
    /// The text of the first candidate encoding that succeeded, or null if none succeeded or there
    /// was no stream to decode.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public static Task<DecodedText> DecodeText(this IStreamText source,
                                               DecodedTextBuilder builder,
                                               DecodedTextOptions options,
                                               CancellationToken cancellationToken) {
        return DecodeText(() => source, builder, options, cancellationToken);
    }

    /// <summary>
    /// Decodes the text of a stream, trying each candidate encoding in
    /// <see cref="DecodedTextOptions.Encoding"/>.
    /// </summary>
    /// <param name="source">The source of the stream to decode.</param>
    /// <param name="builders">
    /// Receive the decoded text as it becomes available, and are completed or failed when the
    /// decode finishes. Null items are ignored.
    /// </param>
    /// <param name="options">
    /// The options for the decode. If null, default options are used, and their buffers are
    /// returned to the pool when the decode finishes.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the decode.</param>
    /// <returns>
    /// The text of the first candidate encoding that succeeded, or null if none succeeded or there
    /// was no stream to decode.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="builders"/> is null.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public static Task<DecodedText> DecodeText(this IStreamText source,
                                               IEnumerable<DecodedTextBuilder> builders,
                                               DecodedTextOptions options,
                                               CancellationToken cancellationToken) {
        return DecodeText(source, DecodedTextBuilder.Combine(builders), options, cancellationToken);
    }

    /// <summary>
    /// Decodes the text of a file, trying each candidate encoding in
    /// <see cref="DecodedTextOptions.Encoding"/>.
    /// </summary>
    /// <param name="fileInfo">The file to decode.</param>
    /// <param name="decoded">Called with the progress of each candidate encoding. May be null.</param>
    /// <param name="options">
    /// The options for the decode. If null, default options are used, and their buffers are
    /// returned to the pool when the decode finishes.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the decode.</param>
    /// <returns>
    /// The text of the first candidate encoding that succeeded, or null if none succeeded or there
    /// was no stream to decode.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileInfo"/> is null.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public static Task<DecodedText> DecodeText(this FileInfo fileInfo,
                                               DecodedTextDelegate decoded,
                                               DecodedTextOptions options,
                                               CancellationToken cancellationToken) {
        return DecodeText(new StreamTextSourceFile(fileInfo),
                          options,
                          opt => opt.ForStream(decoded, null),
                          cancellationToken);
    }

    /// <summary>
    /// Decodes the text of a file, trying each candidate encoding in
    /// <see cref="DecodedTextOptions.Encoding"/>.
    /// </summary>
    /// <param name="fileInfo">The file to decode.</param>
    /// <param name="builder">
    /// Receives the decoded text as it becomes available, and is completed or failed when the
    /// decode finishes. May be null.
    /// </param>
    /// <param name="options">
    /// The options for the decode. If null, default options are used, and their buffers are
    /// returned to the pool when the decode finishes.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the decode.</param>
    /// <returns>
    /// The text of the first candidate encoding that succeeded, or null if none succeeded or there
    /// was no stream to decode.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileInfo"/> is null.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public static Task<DecodedText> DecodeText(this FileInfo fileInfo,
                                               DecodedTextBuilder builder,
                                               DecodedTextOptions options,
                                               CancellationToken cancellationToken) {
        return DecodeText(() => new StreamTextSourceFile(fileInfo), builder, options, cancellationToken);
    }

    /// <summary>
    /// Decodes the text of a file, trying each candidate encoding in
    /// <see cref="DecodedTextOptions.Encoding"/>.
    /// </summary>
    /// <param name="fileInfo">The file to decode.</param>
    /// <param name="builders">
    /// Receive the decoded text as it becomes available, and are completed or failed when the
    /// decode finishes. Null items are ignored.
    /// </param>
    /// <param name="options">
    /// The options for the decode. If null, default options are used, and their buffers are
    /// returned to the pool when the decode finishes.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the decode.</param>
    /// <returns>
    /// The text of the first candidate encoding that succeeded, or null if none succeeded or there
    /// was no stream to decode.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileInfo"/> is null.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="builders"/> is null.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public static Task<DecodedText> DecodeText(this FileInfo fileInfo,
                                               IEnumerable<DecodedTextBuilder> builders,
                                               DecodedTextOptions options,
                                               CancellationToken cancellationToken) {
        return DecodeText(fileInfo, DecodedTextBuilder.Combine(builders), options, cancellationToken);
    }
}
