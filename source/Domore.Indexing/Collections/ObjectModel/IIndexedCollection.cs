using System.Collections;
using System.Collections.Generic;

namespace Domore.Collections.ObjectModel;

public interface IIndexedCollection : IEnumerable {
    int Count { get; }
}

public interface IIndexedCollection<TIndex, out TItem> : IIndexedCollection, IEnumerable<TItem> {
    TItem this[TIndex index] { get; }
    bool Contains(TIndex index);
    IEnumerable<TIndex> Indexes();
    IEnumerable<TItem> Items(params TIndex[] indexes);
    IEnumerable<TItem> Items(IEnumerable<TIndex> indexes);
}
