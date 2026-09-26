using Domore.IO.Extensions;
using Domore.Logs;
using Domore.Text;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.IO;

/// <summary>
/// Coordinates decoding work for a text-reading control.
/// </summary>
internal class TextReaderWorker {
    private static readonly ILog Log = Logging.For(typeof(TextReaderWorker));

    private async Task<DecodedText> Work(DecodedTextBuilder builder, CancellationToken cancellationToken) {
        if (Enabled == false) {
            return null;
        }
        var source = Source;
        if (source == null) {
            return null;
        }
        var options = Options;
        var sourceLengthMax = SourceLengthMax;
        try {
            return await Task.Run(cancellationToken: cancellationToken, function: async () => {
                if (sourceLengthMax.HasValue) {
                    var length = source.StreamLength;
                    if (length > sourceLengthMax.Value) {
                        return null;
                    }
                }
                return await source.DecodeText(builder, options, cancellationToken);
            });
        }
        catch (Exception ex) {
            if (ex is OperationCanceledException canceled && cancellationToken.IsCancellationRequested) {
                if (Log.Debug()) {
                    Log.Debug($"{nameof(canceled)}[{source}]");
                }
            }
            else {
                if (Log.Info()) {
                    Log.Info($"{nameof(Exception)}[{source}]", ex);
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Gets or sets whether decoding work is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum source length in bytes, or null for no limit.
    /// </summary>
    public long? SourceLengthMax { get; set; }

    /// <summary>
    /// Gets or sets the source to decode.
    /// </summary>
    public IStreamText Source { get; set; }

    /// <summary>
    /// Gets or sets the decoding options.
    /// </summary>
    public DecodedTextOptions Options { get; set; }

    /// <summary>
    /// Decodes the configured source and streams decoded text to a builder.
    /// </summary>
    /// <param name="builder">Receives incremental decoded text.</param>
    /// <param name="cancellationToken">Cancels the decoding operation.</param>
    /// <returns>The decoded result, or null if decoding is disabled, unavailable, or unsuccessful.</returns>
    public Task<DecodedText> Refresh(DecodedTextBuilder builder, CancellationToken cancellationToken) {
        return Work(builder, cancellationToken);
    }
}
