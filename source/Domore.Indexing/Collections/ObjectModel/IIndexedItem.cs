namespace Domore.Collections.ObjectModel;

public interface IIndexedItem<out TIndex> {
    TIndex Index { get; }
}
