using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Domore.Collections.ObjectModel;

public abstract class IndexedCollection<TIndex, TItem> : IIndexedCollection<TIndex, TItem> where TItem : IIndexedItem<TIndex> {
    private readonly KeyedCollection Collection;

    private TItem PrivateCreateItem(TIndex index) {
        var item = CreateItem(index);
        var args = new IndexedItemCreatedEventArgs<TItem>(item);
        OnItemCreated(args);
        return item;
    }

    protected IEqualityComparer<TIndex> Comparer =>
        Collection.Comparer;

    protected IndexedCollection(IEqualityComparer<TIndex> comparer) {
        Collection = new(comparer);
    }

    protected IndexedCollection() : this(default(IEqualityComparer<TIndex>)) {
    }

    protected IndexedCollection(IEqualityComparer<TIndex> comparer, IEnumerable<TItem> collection) : this(comparer) {
        if (null == collection) throw new ArgumentNullException(nameof(collection));
        foreach (var item in collection) {
            Collection.Add(item);
        }
    }

    protected IndexedCollection(IEnumerable<TItem> collection) : this(default, collection) {
    }

    protected abstract TItem CreateItem(TIndex index);

    protected virtual void OnItemCreated(IndexedItemCreatedEventArgs<TItem> e) =>
        ItemCreated?.Invoke(this, e);

    public event IndexedItemCreatedEventHandler<TItem> ItemCreated;

    public TItem this[TIndex index] {
        get {
            if (Collection.Contains(index) == false) {
                Collection.Add(PrivateCreateItem(index));
            }
            return Collection[index];
        }
    }

    public int Count => Collection.Count;

    public bool Contains(TIndex index) => Collection.Contains(index);
    public IEnumerable<TIndex> Indexes() => Collection.Select(item => item.Index);
    public IEnumerable<TItem> Items(params TIndex[] indexes) => Items(indexes?.AsEnumerable());

    public IEnumerable<TItem> Items(IEnumerable<TIndex> indexes = null) {
        if (indexes == null || !indexes.Any()) {
            indexes = Indexes();
        }
        return indexes.Select(index => this[index]);
    }

    public IEnumerator<TItem> GetEnumerator() =>
        Collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        Collection.GetEnumerator();

    private class KeyedCollection : KeyedCollection<TIndex, TItem> {
        protected override TIndex GetKeyForItem(TItem item) {
            if (null == item) throw new ArgumentNullException(nameof(item));
            return item.Index;
        }

        public KeyedCollection(IEqualityComparer<TIndex> comparer) : base(comparer) {
        }
    }
}
