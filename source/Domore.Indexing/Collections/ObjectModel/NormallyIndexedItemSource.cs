using Domore.Collections.Generic;
using System.Collections.Generic;

namespace Domore.Collections.ObjectModel;

/// <summary>
/// Represents a source of items indexed by strings, using a case-insensitive comparison for string keys.
/// </summary>
/// <remarks>This class provides a base implementation for managing a collection of items indexed by string keys,
/// where key comparisons are performed in a case-insensitive manner using <see cref="NormalStringComparer"/>.</remarks>
/// <typeparam name="TItem">The type of items in the source. Each item must implement <see cref="IIndexedItem{TKey}"/> with a string key.</typeparam>
public abstract class NormallyIndexedItemSource<TItem> : IndexedItemSource<string, TItem> where TItem : IIndexedItem<string> {
    /// <summary>
    /// Initializes a new instance of the <see cref="NormallyIndexedItemSource{TItem}"/> class with an optional
    /// collection of items and a synchronization object.
    /// </summary>
    /// <param name="items">An optional collection of items to initialize the source with. If <see langword="null"/>, the collection is
    /// initialized empty.</param>
    /// <param name="syncRoot">An optional synchronization object used to ensure thread safety. If <see langword="null"/>, no synchronization
    /// is applied.</param>
    public NormallyIndexedItemSource(IEnumerable<TItem> items = null, object syncRoot = null) : base(
        comparer: new NormalStringComparer(),
        items: items,
        syncRoot: syncRoot) {
    }
}
