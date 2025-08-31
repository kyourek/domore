namespace Domore.Collections.ObjectModel;

public abstract class NormallyIndexedItem : IIndexedItem<string> {
    protected string Index { get; private set; }

    protected NormallyIndexedItem() {
    }

    string IIndexedItem<string>.Index => Index;

    public class Source<TItem> : NormallyIndexedItemSource<TItem> where TItem : NormallyIndexedItem, new() {
        protected Source() {
        }

        protected override TItem CreateItem(string index) =>
            new() { Index = index };
    }
}

public abstract class NormallyIndexedItem<TSelf> : NormallyIndexedItem where TSelf : NormallyIndexedItem, new() {
    protected NormallyIndexedItem() {
    }

    public sealed class Source : Source<TSelf> {
    }
}
