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
    /// <summary>
    /// Gets the item associated with the specified index.
    /// </summary>
    /// <param name="index">The index of the item to retrieve.</param>
    /// <returns>The item associated with the specified index.</returns>
    TItem this[TIndex index] { get; }

    /// <summary>
    /// Determines whether the collection contains an element at the specified index.
    /// </summary>
    /// <remarks>The behavior of this method may depend on the specific implementation of the collection. 
    /// Ensure that the provided index is within the valid range for the collection.</remarks>
    /// <param name="index">The index to locate in the collection.</param>
    /// <returns><see langword="true"/> if the collection contains an element at the specified index; otherwise, <see
    /// langword="false"/>.</returns>
    bool Contains(TIndex index);

    /// <summary>
    /// Retrieves a collection of all indexes associated with the current instance.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the indexes. The collection may be empty if no indexes are available.</returns>
    IEnumerable<TIndex> Indexes();

    /// <summary>
    /// Retrieves a collection of items based on the specified indexes.
    /// </summary>
    /// <param name="indexes">An array of indexes used to identify the items to retrieve.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the items corresponding to the specified indexes.</returns>
    IEnumerable<TItem> Items(params TIndex[] indexes);

    /// <summary>
    /// Retrieves a collection of items corresponding to the specified indexes.
    /// </summary>
    /// <param name="indexes">A collection of indexes used to locate the items.</param>
    /// <returns>An enumerable collection of items corresponding to the provided indexes.</returns>
    IEnumerable<TItem> Items(IEnumerable<TIndex> indexes);
}
