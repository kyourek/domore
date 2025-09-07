using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Domore.Collections.ObjectModel;

/// <summary>
/// Represents an abstract base class for a collection of indexed items, where each item is uniquely identified by an
/// index.
/// </summary>
/// <remarks>This class provides a thread-safe implementation for managing a collection of indexed items, with
/// support for lazy item creation, synchronization, and event handling when new items are created. Derived classes must
/// implement the <see cref="CreateItem(TIndex)"/> method to define how new items are created when they are accessed by
/// index.</remarks>
/// <typeparam name="TIndex">The type of the index used to identify items in the collection.</typeparam>
/// <typeparam name="TItem">The type of the items in the collection. Must implement <see cref="IIndexedItem{TIndex}"/>.</typeparam>
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

    /// <summary>
    /// An object used to synchronize access to shared resources in a thread-safe manner.
    /// </summary>
    /// <remarks>This field is typically used as a locking mechanism to ensure that multiple threads  do not
    /// simultaneously access or modify shared data. Use this object with the `lock` statement to implement thread
    /// synchronization.</remarks>
    protected readonly object SyncRoot;

    /// <summary>
    /// Gets the equality comparer used to determine equality of keys in the collection.
    /// </summary>
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

    /// <summary>
    /// Releases the resources used by the current instance of the class.
    /// </summary>
    /// <remarks>This method is called to release both managed and unmanaged resources.  Override this method
    /// in a derived class to provide custom cleanup logic.  Ensure that any overridden implementation calls the base
    /// class's <see cref="Dispose(bool)"/> method.</remarks>
    /// <param name="disposing">A value indicating whether to release managed resources. <see langword="true"/> to release both managed and
    /// unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedItemSource{TIndex, TItem}"/> class with the specified
    /// comparer, items, and synchronization object.
    /// </summary>
    /// <param name="comparer">An optional equality comparer used to compare index values. If <see langword="null"/>, the default equality
    /// comparer for <typeparamref name="TIndex"/> is used.</param>
    /// <param name="items">An optional collection of items to initialize the source with. If <see langword="null"/>, the collection is
    /// initialized empty.</param>
    /// <param name="syncRoot">An optional synchronization object used to ensure thread safety. If <see langword="null"/>, no synchronization
    /// is applied.</param>
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

    /// <summary>
    /// Creates an instance of the item associated with the specified index.
    /// </summary>
    /// <remarks>This method must be implemented by a derived class to define how items are created based on
    /// the provided index.</remarks>
    /// <param name="index">The index used to create the item. The value must be valid for the derived implementation.</param>
    /// <returns>The created item of type <typeparamref name="TItem"/>.</returns>
    protected abstract TItem CreateItem(TIndex index);

    /// <summary>
    /// Raises the <see cref="ItemCreated"/> event when a new item is created.
    /// </summary>
    /// <remarks>This method is called to notify subscribers that a new item has been created. Derived
    /// classes can override this method to provide additional processing  when the event is raised. Ensure that the
    /// base implementation is called to maintain the event invocation.</remarks>
    /// <param name="e">The event data containing information about the created item.</param>
    protected virtual void OnItemCreated(IndexedItemCreatedEventArgs<TItem> e) =>
        ItemCreated?.Invoke(this, e);

    /// <summary>
    /// Occurs when a new item is created and added to the collection.
    /// </summary>
    /// <remarks>This event is triggered after the item has been successfully created and added. Subscribers
    /// can use this event to perform additional actions, such as logging or updating related data.</remarks>
    public event IndexedItemCreatedEventHandler<TItem> ItemCreated;

    /// <summary>
    /// Gets the item associated with the specified index. If the item does not exist, it is created and added to the
    /// collection.
    /// </summary>
    /// <remarks>This indexer retrieves an existing item if it is present in the collection. If the item does
    /// not exist, it is created using the specified index and added to the collection.</remarks>
    /// <param name="index">The index of the item to retrieve or create.</param>
    /// <returns>The item associated with the specified index.</returns>
    public TItem this[TIndex index] =>
        GetOrCreate(index);

    /// <summary>
    /// Gets the number of elements contained in the collection.
    /// </summary>
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

    /// <summary>
    /// Determines whether the collection contains the specified index.
    /// </summary>
    /// <remarks>This method is thread-safe if the <c>SyncRoot</c> property is not <see
    /// langword="null"/>.</remarks>
    /// <param name="index">The index to locate in the collection.</param>
    /// <returns><see langword="true"/> if the specified index is found in the collection; otherwise, <see langword="false"/>.</returns>
    public bool Contains(TIndex index) {
        if (SyncRoot is null) {
            return Collection.Contains(index);
        }
        lock (SyncRoot) {
            return Collection.Contains(index);
        }
    }

    /// <summary>
    /// Retrieves a collection of indexes associated with the current instance.
    /// </summary>
    /// <remarks>If the <see cref="SyncRoot"/> property is not null, the operation is thread-safe and locks on
    /// the <see cref="SyncRoot"/> before accessing the indexes. Otherwise, the indexes are accessed directly.</remarks>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the indexes. The returned collection reflects the current state of
    /// the instance.</returns>
    public IEnumerable<TIndex> Indexes() {
        if (SyncRoot is null) {
            return Collection.Keys;
        }
        lock (SyncRoot) {
            return [.. Collection.Keys];
        }
    }

    /// <summary>
    /// Retrieves a collection of items, optionally filtered by the specified indexes.
    /// </summary>
    /// <remarks>This method is thread-safe. If a synchronization object is defined, access to the underlying
    /// collection is locked to ensure thread safety.</remarks>
    /// <param name="indexes">An optional collection of indexes used to filter the items. If <see langword="null"/>, all items are returned.</param>
    /// <returns>An enumerable collection of items. If <paramref name="indexes"/> is provided, only the items corresponding to
    /// the specified indexes are returned.</returns>
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

    /// <summary>
    /// Retrieves a collection of items, optionally filtered by the specified indexes.
    /// </summary>
    /// <remarks>This method is thread-safe. If a synchronization object is defined, access to the underlying
    /// collection is locked to ensure thread safety.</remarks>
    /// <param name="indexes">An optional collection of indexes used to filter the items. If <see langword="null"/>, all items are returned.</param>
    /// <returns>An enumerable collection of items. If <paramref name="indexes"/> is provided, only the items corresponding to
    /// the specified indexes are returned.</returns>
    public IEnumerable<TItem> Items(params TIndex[] indexes) =>
        Items(indexes?.AsEnumerable());

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <remarks>If a synchronization object is provided via <see cref="SyncRoot"/>, the collection is
    /// enumerated in a thread-safe manner by creating a snapshot of the collection. Otherwise, the enumerator directly
    /// iterates over the collection.</remarks>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
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
        protected sealed override TIndex GetKeyForItem(TItem item) {
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
            if (key is null) {
                var k = key = dict.Keys.FirstOrDefault(k => Comparer.Equals(key, k));
                if (k is null) {
                    value = default;
                    return false;
                }
            }
            return dict.TryGetValue(key, out value);
        }
    }
}
