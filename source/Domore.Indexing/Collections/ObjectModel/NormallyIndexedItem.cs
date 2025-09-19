using Domore.Collections.Generic;
using System.Collections.Generic;

namespace Domore.Collections.ObjectModel;

/// <summary>
/// Represents an abstract base class for items that are indexed by a normalized string value.
/// </summary>
/// <remarks>This class provides a foundation for items that are identified by a string index, which is normalized
/// for consistent comparisons. It implements the <see cref="IIndexedItem{T}"/> interface, exposing the normalized index
/// value through the <see cref="IIndexedItem{T}.Index"/> property.</remarks>
public abstract class NormallyIndexedItem : IIndexedItem<string> {
    /// <summary>
    /// Gets the index value associated with the current instance.
    /// </summary>
    protected string Index { get; private set; }

    string IIndexedItem<string>.Index => Index;

    /// <summary>
    /// Represents an abstract base class for a source of items that are indexed using a normalized string comparer.
    /// </summary>
    /// <remarks>This class provides functionality for managing a collection of items with normalized string
    /// indices. Derived classes can extend this functionality as needed.</remarks>
    /// <typeparam name="TItem">The type of items in the source. Must derive from <see cref="NormallyIndexedItem"/> and have a parameterless
    /// constructor.</typeparam>
    public abstract class Source<TItem> : NormallyIndexedItemSource<TItem> where TItem : NormallyIndexedItem, new() {
        /// <summary>
        /// Initializes a new instance of the <see cref="Source{TItem}"/> class with an optional collection of items and
        /// a synchronization object.
        /// </summary>
        /// <param name="items">An optional collection of items to initialize the source with. If <see langword="null"/>, the collection is
        /// initialized empty.</param>
        /// <param name="syncRoot">An optional synchronization object used to ensure thread safety. If <see langword="null"/>, no synchronization
        /// is applied.</param>
        protected Source(IEnumerable<TItem> items = null, object syncRoot = null) : base(items, syncRoot) {
        }

        /// <summary>
        /// Creates a new instance of <typeparamref name="TItem"/> with the specified index.
        /// </summary>
        /// <remarks>The <paramref name="index"/> parameter is normalized using <see
        /// cref="NormalStringComparer.Normalize(string)"/>  before being assigned to the item's <c>Index</c>
        /// property.</remarks>
        /// <param name="index">The index to associate with the created item. This value is normalized before being assigned.</param>
        /// <returns>A new instance of <typeparamref name="TItem"/> with the normalized index set.</returns>
        protected sealed override TItem CreateItem(string index) =>
            new() { Index = NormalStringComparer.Normalize(index) };
    }
}

/// <summary>
/// Represents an abstract base class for items that are normally indexed, with support for a strongly-typed source.
/// </summary>
/// <remarks>This class provides a generic base for creating items that are indexed in a collection. It enforces a
/// self-referential generic constraint to ensure that derived types are strongly typed and compatible with the
/// associated <see cref="Source"/> class.</remarks>
/// <typeparam name="TSelf">The type of the derived class, which must inherit from <see cref="NormallyIndexedItem"/> and have a parameterless
/// constructor.</typeparam>
public abstract class NormallyIndexedItem<TSelf> : NormallyIndexedItem where TSelf : NormallyIndexedItem, new() {
    /// <summary>
    /// Represents a collection of items of type <typeparamref name="TSelf"/> with optional synchronization
    /// support.
    /// </summary>
    public sealed class Source : Source<TSelf> {
        /// <summary>
        /// Initializes a new instance of the <see cref="Source"/> class with the specified items and
        /// synchronization object.
        /// </summary>
        /// <param name="items">An optional collection of items to initialize the source with. If <see langword="null"/>, the collection is
        /// initialized empty.</param>
        /// <param name="syncRoot">An optional synchronization object used to ensure thread safety. If <see langword="null"/>, no synchronization
        /// is applied.</param>
        public Source(IEnumerable<TSelf> items = null, object syncRoot = null) : base(items, syncRoot) {
        }
    }
}
