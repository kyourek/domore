using System;
using System.Buffers;
using System.Collections.Generic;

namespace Domore.Buffers;

internal abstract class BufferPool {
    private readonly List<BufferRental> Buffers = [];

    public bool Clear { get; set; }

    public BufferSize RentSize {
        get => field ??= new BufferSize();
        set;
    }

    public void Free() {
        lock (Buffers) {
            foreach (var buffer in Buffers) {
                buffer.Free(Clear);
            }
            Buffers.Clear();
        }
    }

    public abstract class Of<T> : BufferPool {
        public ArrayPool<T> Pool {
            get => field ??= ArrayPool<T>.Shared;
            set;
        }

        public T[] Rent(int sizeHint = default) {
            var pool = Pool;
            var space = pool.Rent(Math.Max(sizeHint, RentSize.Length));
            var rental = BufferRental.Keep(pool, space);
            lock (Buffers) {
                Buffers.Add(rental);
            }
            return space;
        }
    }
}

internal sealed class BufferPool<T> : BufferPool.Of<T> {
}
