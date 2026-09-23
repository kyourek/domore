using Domore.Buffers;
using Domore.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domore.Text;

/// <summary>
/// Options for decoding text from a stream.
/// </summary>
/// <remarks>
/// <para>
/// Decoding keeps everything it reads in memory, so it is not suited to very large streams.
/// Every byte read from the stream is kept, because each candidate in <see cref="Encoding"/>
/// decodes the same bytes, and each candidate decodes into its own text buffers, even
/// candidates that fail. All of these buffers are held until the options are disposed. When
/// a candidate is selected, its text is also copied into a string that lives as long as the
/// returned <see cref="DecodedText"/>. Text from other candidates is not preserved after the
/// options are disposed.
/// </para>
/// <para>
/// Peak memory is therefore roughly the size of the stream, plus the decoded text once for
/// each candidate encoding, plus one more copy of the selected text.
/// Specifying a single encoding keeps this to a minimum.
/// </para>
/// <para>
/// Buffers are rented from the pools configured by <see cref="StreamBuffer"/> and
/// <see cref="TextBuffer"/>, and are returned only when the object from
/// <see cref="Disposable"/> or <see cref="DisposableAsync"/> is disposed. The same options may
/// be used for more than one decode; the buffers of every decode are held until then.
/// </para>
/// </remarks>
public class DecodedTextOptions {
    private readonly List<BufferPool> Pools = [];

    private void Free() {
        lock (Pools) {
            foreach (var pool in Pools) {
                pool.Free();
            }
            Pools.Clear();
        }
    }

    private BufferPool<T> Pool<T>(BufferOptions options) {
        if (options is null) {
            throw new ArgumentNullException(nameof(options));
        }
        var pool = options.CreatePool<T>();
        lock (Pools) {
            Pools.Add(pool);
        }
        return pool;
    }

    internal int PoolCount {
        get {
            lock (Pools) {
                return Pools.Count;
            }
        }
    }

    internal StreamTextDecoder ForStream(DecodedTextDelegate decoded, DecodedTextDelegate completed) {
        return new StreamTextDecoder {
            Completed = completed,
            Decoded = decoded,
            Encoding = Encoding.Count > 0
                ? Encoding
                : new[] { "utf-8" },
            EncodingFallback = EncodingFallback,
            StreamBuffer = Pool<byte>(StreamBuffer),
            TextBuffer = Pool<char>(TextBuffer),
        };
    }

    /// <summary>
    /// Gets or sets the options for the buffers that bytes are read into from the stream.
    /// </summary>
    public BufferOptions StreamBuffer {
        get => field ??= new();
        set;
    }

    /// <summary>
    /// Gets or sets the options for the buffers that text is decoded into.
    /// </summary>
    public BufferOptions TextBuffer {
        get => field ??= new();
        set;
    }

    /// <summary>
    /// Gets or sets the replacement for invalid bytes, keyed by a name in <see cref="Encoding"/>.
    /// A candidate encoding with no entry fails when it meets invalid bytes.
    /// </summary>
    public Dictionary<string, string> EncodingFallback {
        get => field ??= [];
        set;
    }

    /// <summary>
    /// Gets or sets the names of the candidate encodings, in order of preference. All candidates
    /// decode the stream at the same time, and the result is taken from the first one in this
    /// list that succeeds. If the list is empty, "utf-8" is used. A byte order mark at the start
    /// of the stream overrides the candidate's encoding.
    /// </summary>
    public List<string> Encoding {
        get => field ??= [];
        set;
    }

    /// <summary>
    /// Returns an object that, when disposed, returns every buffer rented by decodes that used
    /// these options to its pool.
    /// </summary>
    public IDisposable Disposable() {
        return new DisposableImplementation(this);
    }

    /// <inheritdoc cref="Disposable"/>
    public IAsyncDisposable DisposableAsync() {
        return new DisposableImplementation(this);
    }

    private sealed class DisposableImplementation : IAsyncDisposable, IDisposable {
        public DecodedTextOptions Options { get; }

        public DisposableImplementation(DecodedTextOptions options) {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        void IDisposable.Dispose() {
            Options.Free();
        }

        async ValueTask IAsyncDisposable.DisposeAsync() {
            var task = Task.Run(() => { using (this) { } });
            await task.ConfigureAwait(false);
        }
    }
}
