using System;

namespace Domore.Collections.ObjectModel;

/// <summary>
/// Represents the method that will handle an event when a new indexed item is created.
/// </summary>
/// <typeparam name="TItem">The type of the item associated with the event.</typeparam>
/// <param name="sender">The source of the event.</param>
/// <param name="e">The event data containing information about the created indexed item.</param>
public delegate void IndexedItemCreatedEventHandler<TItem>(object sender, IndexedItemCreatedEventArgs<TItem> e);

/// <summary>
/// Provides data for an event that occurs when a new item is created and added to an indexed collection.
/// </summary>
/// <typeparam name="TItem">The type of the item that was created.</typeparam>
public sealed class IndexedItemCreatedEventArgs<TItem> : EventArgs {
    /// <summary>
    /// Gets the item associated with the current instance.
    /// </summary>
    public TItem Item { get; }

    /// <summary>
    /// Provides data for the event that occurs when an item is created and added to an index.
    /// </summary>
    /// <param name="item">The item that was created and added to the index.</param>
    public IndexedItemCreatedEventArgs(TItem item) {
        Item = item;
    }
}
