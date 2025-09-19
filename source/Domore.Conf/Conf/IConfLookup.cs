using System.Collections.Generic;

namespace Domore.Conf;

/// <summary>
/// A key/value pair representation of conf content.
/// </summary>
public interface IConfLookup {
    /// <summary>
    /// Gets the number of keys in the lookup.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Determines whether or not the <paramref name="key"/> exists in the lookup.
    /// </summary>
    /// <param name="key">The key to check for existence.</param>
    /// <returns>True if the key exists in the lookup, otherwise false.</returns>
    bool Contains(string key);

    /// <summary>
    /// Gets the last value in the lookup for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key whose last value is returned.</param>
    /// <returns>The last value in the lookup for the specified <paramref name="key"/>.</returns>
    string Value(string key);

    /// <summary>
    /// Gets all values in the lookup for teh specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key whose values are returned.</param>
    /// <returns>All values in the lookup for the specified <paramref name="key"/>.</returns>
    IEnumerable<string> All(string key);
}
