using System;
using System.Collections.Generic;

namespace Domore.Conf {
    /// <summary>
    /// Implementations of this interface provide serialization from conf to objects.
    /// </summary>
    public interface IConf {
        /// <summary>
        /// Gets the serialization source used by the instance.
        /// </summary>
        object Source { get; }

        /// <summary>
        /// Gets the collection of serialization sources used by the instance.
        /// </summary>
        IEnumerable<object> Sources { get; }

        /// <summary>
        /// Gets a key/value pair lookup from the instance.
        /// </summary>
        IConfLookup Lookup { get; }

        /// <summary>
        /// Populates properties of <paramref name="target"/> using the serialization source of the instance.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="target"/>.</typeparam>
        /// <param name="target">The object populated by the instance.</param>
        /// <param name="key">The key used to identify the items in the serialization source that are used to populate <paramref name="target"/>.</param>
        /// <returns>The populated <paramref name="target"/>.</returns>
        T Configure<T>(T target, string key = null);

        /// <summary>
        /// Populates properties of items created by <paramref name="factory"/>.
        /// </summary>
        /// <typeparam name="T">The type of item created by <paramref name="factory"/>.</typeparam>
        /// <param name="factory">The factory that creates the items that are populated.</param>
        /// <param name="key">The common key of the items in the serialization source.</param>
        /// <param name="comparer">The equality comparer used to compare the item indexes.</param>
        /// <returns>The collection of objects populated.</returns>
        IEnumerable<T> Configure<T>(Func<T> factory, string key = null, IEqualityComparer<string> comparer = null);

        /// <summary>
        /// Populates properties of items created by <paramref name="factory"/>.
        /// </summary>
        /// <typeparam name="T">The type of item created by <paramref name="factory"/>.</typeparam>
        /// <param name="factory">The factory that creates the items that are populated.</param>
        /// <param name="key">The common key of the items in the serialization source.</param>
        /// <param name="comparer">The equality comparer used to compare the item indexes.</param>
        /// <returns>The collection of objects populated as key/value pairs.</returns>
        IEnumerable<KeyValuePair<string, T>> Configure<T>(Func<string, T> factory, string key = null, IEqualityComparer<string> comparer = null);
    }
}
