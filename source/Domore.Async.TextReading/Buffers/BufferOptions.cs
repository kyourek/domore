using System.Buffers;

namespace Domore.Buffers;

/// <summary>
/// Options for the buffers rented while decoding.
/// </summary>
public sealed class BufferOptions {
    internal BufferPool<T> CreatePool<T>() {
        return new BufferPool<T> {
            Clear = Clear,
            Pool = Shared
                ? ArrayPool<T>.Shared
                : ArrayPool<T>.Create(),
            RentSize = new BufferSize {
                Length = Size
            }
        };
    }

    /// <summary>
    /// Gets or sets the minimum length of each rented buffer. Larger buffers are rented when
    /// more space is needed. The default is 512.
    /// </summary>
    public int Size { get; set; } = 512;

    /// <summary>
    /// Gets or sets whether buffers are cleared when they are returned to the pool. The default
    /// is false.
    /// </summary>
    public bool Clear { get; set; } = false;

    /// <summary>
    /// Gets or sets whether buffers are rented from <see cref="ArrayPool{T}.Shared"/>. If false,
    /// each decode rents from a pool of its own. The default is true.
    /// </summary>
    public bool Shared { get; set; } = true;
}
