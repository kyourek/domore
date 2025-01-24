using System;

namespace Domore.Collections.ObjectModel {
    public abstract class NormallyIndexedDisposableItem<TItem> : NormallyIndexedItem, IDisposable where TItem : NormallyIndexedItem, IDisposable, new() {
        protected NormallyIndexedDisposableItem() {
        }

        protected virtual void Dispose(bool disposing) {
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NormallyIndexedDisposableItem() {
            Dispose(false);
        }

        public class Collection : DisposableCollection<TItem> {
        }
    }
}
