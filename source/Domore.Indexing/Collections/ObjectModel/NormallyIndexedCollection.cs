using Domore.Collections.Generic;

namespace Domore.Collections.ObjectModel;

public abstract class NormallyIndexedCollection<TItem> : IndexedCollection<string, TItem> where TItem : IIndexedItem<string> {
    public NormallyIndexedCollection() : base(new NormalStringComparer()) {
    }
}
