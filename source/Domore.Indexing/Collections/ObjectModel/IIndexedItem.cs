namespace Domore.Collections.ObjectModel;

/// <summary>
/// Implementations of this interface have an index that identifies the item
/// in an instance of <see cref="IIndexedItemSource"/>.
/// </summary>
/// <typeparam name="TIndex">The type of the index for the type.</typeparam>
public interface IIndexedItem<out TIndex> {
    /// <summary>
    /// Gets the index for the item.
    /// </summary>
    TIndex Index { get; }
}
