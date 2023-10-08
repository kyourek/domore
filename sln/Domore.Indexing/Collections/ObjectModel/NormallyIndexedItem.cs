using System;

namespace Domore.Collections.ObjectModel {
    public class NormallyIndexedItem : IIndexedItem<string> {
        protected string Index { get; private set; }

        protected NormallyIndexedItem() {
        }

        string IIndexedItem<string>.Index => Index;

        public class Collection<TItem> : NormallyIndexedCollection<TItem> where TItem : NormallyIndexedItem, new() {
            protected Collection() {
            }

            protected override TItem CreateItem(string index) =>
                new TItem { Index = index };
        }

        public class DisposableCollection<TItem> : NormallyIndexedDisposableCollection<TItem> where TItem : NormallyIndexedItem, IDisposable, new() {
            protected DisposableCollection() {
            }

            protected override TItem CreateItem(string index) =>
                new TItem { Index = index };
        }
    }

    public class NormallyIndexedItem<TItem> : NormallyIndexedItem where TItem : NormallyIndexedItem, new() {
        protected NormallyIndexedItem() {
        }

        public class Collection : Collection<TItem> {
        }
    }
}
