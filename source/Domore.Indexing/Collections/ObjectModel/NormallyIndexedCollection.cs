namespace Domore.Collections.ObjectModel {
    using Generic;

    public abstract class NormallyIndexedCollection<TItem> : IndexedCollection<string, TItem> where TItem : IIndexedItem<string> {
        public NormallyIndexedCollection() : base(new NormalStringComparer()) {
        }
    }
}
