using System.Collections;
using System.Collections.Generic;

namespace Domore.Conf;

internal interface IConfCollection : IEnumerable {
    int Count { get; }
}

internal interface IConfCollection<out T> : IConfCollection, IEnumerable<T> {
    T this[int index] { get; }
}
