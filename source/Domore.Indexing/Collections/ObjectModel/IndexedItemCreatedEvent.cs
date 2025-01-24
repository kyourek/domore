using System;

namespace Domore.Collections.ObjectModel {
    public delegate void IndexedItemCreatedEventHandler<TItem>(object sender, IndexedItemCreatedEventArgs<TItem> e);

    public sealed class IndexedItemCreatedEventArgs<TItem> : EventArgs {
        public TItem Item { get; }

        public IndexedItemCreatedEventArgs(TItem item) {
            Item = item;
        }
    }
}
