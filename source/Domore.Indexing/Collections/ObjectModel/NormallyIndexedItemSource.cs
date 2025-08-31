using Domore.Collections.Generic;
using System.Collections.Generic;

namespace Domore.Collections.ObjectModel;

public abstract class NormallyIndexedItemSource<TItem> : IndexedItemSource<string, TItem> where TItem : IIndexedItem<string> {
    public NormallyIndexedItemSource(IEnumerable<TItem> items = null, object syncRoot = null) : base(
        comparer: new NormalStringComparer(),
        items: items,
        syncRoot: syncRoot) {
    }
}
