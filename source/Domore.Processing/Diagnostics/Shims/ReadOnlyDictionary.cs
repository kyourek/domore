#if NET40
using System;
using System.Collections;
using System.Collections.Generic;

namespace Domore.Diagnostics.Shims;

internal sealed class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
    private readonly Dictionary<TKey, TValue> Agent;

    public ReadOnlyDictionary(IDictionary<TKey, TValue> source) {
        Agent = new(source);
    }

    public TValue this[TKey key] {
        get => ((IDictionary<TKey, TValue>)Agent)[key];
        set => throw new NotSupportedException();
    }

    public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)Agent).Keys;

    public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)Agent).Values;

    public int Count => ((ICollection<KeyValuePair<TKey, TValue>>)Agent).Count;

    public bool IsReadOnly => true;

    public void Add(TKey key, TValue value) {
        throw new NotSupportedException();
    }

    public void Add(KeyValuePair<TKey, TValue> item) {
        throw new NotSupportedException();
    }

    public void Clear() {
        throw new NotSupportedException();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        return ((ICollection<KeyValuePair<TKey, TValue>>)Agent).Contains(item);
    }

    public bool ContainsKey(TKey key) {
        return ((IDictionary<TKey, TValue>)Agent).ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        ((ICollection<KeyValuePair<TKey, TValue>>)Agent).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        return ((IEnumerable<KeyValuePair<TKey, TValue>>)Agent).GetEnumerator();
    }

    public bool Remove(TKey key) {
        throw new NotSupportedException();
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        throw new NotSupportedException();
    }

    public bool TryGetValue(TKey key, out TValue value) {
        return ((IDictionary<TKey, TValue>)Agent).TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)Agent).GetEnumerator();
    }
}
#endif
