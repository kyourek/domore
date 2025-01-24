using System;

namespace Domore.Collections.ObjectModel {
    using Generic;

    public abstract class NormallyIndexedDisposableCollection<TItem> : IndexedDisposableCollection<string, TItem> where TItem : IIndexedItem<string>, IDisposable {
        public NormallyIndexedDisposableCollection() : base(new NormalStringComparer()) {
        }
    }
}
