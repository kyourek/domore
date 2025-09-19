using System.Collections;
using System.Collections.Generic;

namespace Domore.Conf;

internal sealed class ConfCollection<T> : IConfCollection<T> {
    private readonly List<T> List;

    public ConfCollection() {
        List = new List<T>();
    }

    public ConfCollection(IEnumerable<T> collection) {
        List = [.. collection];
    }

    public ConfCollection(params T[] items) {
        List = [.. items];
    }

    public void Add(T item) {
        List.Add(item);
    }

    int IConfCollection.Count =>
        List.Count;

    T IConfCollection<T>.this[int index] =>
        List[index];

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        return ((IEnumerable<T>)List).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)List).GetEnumerator();
    }
}
