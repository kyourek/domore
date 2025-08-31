using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Domore.Collections.ObjectModel;

public abstract class IndexedItemSource<TIndex, TItem> : IIndexedCollection<TIndex, TItem> where TItem : IIndexedItem<TIndex> {
    private readonly KeyedCollection Collection;

    private bool Disposed;

    private ObjectDisposedException DisposedException() =>
        new(GetType().Name);

    private void DisposeUnlocked() {
        try {
            var exceptions = new List<Exception>();
            foreach (var item in Collection) {
                if (item is IDisposable disposable) {
                    try {
                        disposable.Dispose();
                    }
                    catch (Exception ex) {
                        exceptions.Add(ex);
                    }
                }
            }
            if (exceptions.Count > 0) {
                throw new AggregateException(exceptions);
            }
        }
        finally {
            Disposed = true;
        }
    }

    private TItem GetOrCreateUnlocked(TIndex index) {
        if (Collection.TryGet(key: index, out var value) == false) {
            if (Disposed) {
                throw DisposedException();
            }
            var item = value = CreateItem(index);
            var args = new IndexedItemCreatedEventArgs<TItem>(item);
            OnItemCreated(args);
            Collection.Add(item);
        }
        return value;
    }

    private TItem GetOrCreate(TIndex index) {
        if (SyncRoot is null) {
            return GetOrCreateUnlocked(index);
        }
        lock (SyncRoot) {
            return GetOrCreateUnlocked(index);
        }
    }

    protected readonly object SyncRoot;

    protected IEqualityComparer<TIndex> Comparer {
        get {
            if (SyncRoot is null) {
                return Collection.Comparer;
            }
            lock (SyncRoot) {
                return Collection.Comparer;
            }
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            if (SyncRoot is null) {
                DisposeUnlocked();
            }
            else {
                lock (SyncRoot) {
                    DisposeUnlocked();
                }
            }
        }
    }

    protected IndexedItemSource(IEqualityComparer<TIndex> comparer = null, IEnumerable<TItem> items = null, object syncRoot = null) {
        var locker = SyncRoot = syncRoot;
        if (locker is null) {
            Collection = new(comparer, items);
        }
        else {
            lock (locker) {
                Collection = new(comparer, items);
            }
        }
    }

    protected abstract TItem CreateItem(TIndex index);

    protected virtual void OnItemCreated(IndexedItemCreatedEventArgs<TItem> e) =>
        ItemCreated?.Invoke(this, e);

    public event IndexedItemCreatedEventHandler<TItem> ItemCreated;

    public TItem this[TIndex index] =>
        GetOrCreate(index);

    public int Count {
        get {
            if (SyncRoot is null) {
                return Collection.Count;
            }
            lock (SyncRoot) {
                return Collection.Count;
            }
        }
    }

    public bool Contains(TIndex index) {
        if (SyncRoot is null) {
            return Collection.Contains(index);
        }
        lock (SyncRoot) {
            return Collection.Contains(index);
        }
    }

    public IEnumerable<TIndex> Indexes() {
        if (SyncRoot is null) {
            return Collection.Keys;
        }
        lock (SyncRoot) {
            return [.. Collection.Keys];
        }
    }

    public IEnumerable<TItem> Items(IEnumerable<TIndex> indexes = null) {
        if (indexes is null) {
            if (SyncRoot is null) {
                return Collection.Values;
            }
            lock (SyncRoot) {
                return [.. Collection.Values];
            }
        }
        if (SyncRoot is null) {
            return indexes.Select(GetOrCreate);
        }
        lock (SyncRoot) {
            return [.. indexes.Select(GetOrCreate)];
        }
    }

    public IEnumerable<TItem> Items(params TIndex[] indexes) =>
        Items(indexes?.AsEnumerable());


    public IEnumerator<TItem> GetEnumerator() {
        if (SyncRoot is null) {
            return Collection.GetEnumerator();
        }
        lock (SyncRoot) {
            return Collection.ToList().GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    private class KeyedCollection : KeyedCollection<TIndex, TItem> {
        protected override TIndex GetKeyForItem(TItem item) {
            if (item is null) {
                throw new ArgumentNullException(nameof(item));
            }
            return item.Index;
        }

        public ICollection<TIndex> Keys =>
            Dictionary?.Keys ?? [];

        public ICollection<TItem> Values =>
            Dictionary?.Values ?? [];

        public KeyedCollection(IEqualityComparer<TIndex> comparer, IEnumerable<TItem> items) : base(comparer, dictionaryCreationThreshold: 0) {
            if (items is not null) {
                foreach (var item in items) {
                    if (item is not null) {
                        Add(item);
                    }
                }
            }
        }

        public bool TryGet(TIndex key, out TItem value) {
            var dict = Dictionary;
            if (dict is null) {
                value = default;
                return false;
            }
            return dict.TryGetValue(key, out value);
        }
    }
}
