using System.Collections;
using System.Collections.Generic;

namespace Domore.Collections.ObjectModel;

/// <summary>
/// A source of indexed items.
/// </summary>
public interface IIndexedItemSource : IEnumerable {
    /// <summary>
    /// Gets the number of items currently in the source.
    /// </summary>
    int Count { get; }
}

/// <summary>
/// A source of indexed items.
/// </summary>
/// <typeparam name="TIndex">The type of the item's index.</typeparam>
/// <typeparam name="TItem">The type of the item.</typeparam>
public interface IIndexedCollection<TIndex, out TItem> : IIndexedItemSource, IEnumerable<TItem> {
    TItem this[TIndex index] { get; }
    bool Contains(TIndex index);
    IEnumerable<TIndex> Indexes();
    IEnumerable<TItem> Items(params TIndex[] indexes);
    IEnumerable<TItem> Items(IEnumerable<TIndex> indexes);
}
