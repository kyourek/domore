using Domore.Conf.Text;

namespace Domore.Conf.Extensions;

/// <summary>
/// Extension methods to serialize and deserialize conf objects.
/// </summary>
public static class ConfObject {
    private static readonly TextSourceProvider SourceProvider = new();
    private static readonly TextContentProvider ContentProvider = new();

    /// <summary>
    /// Serializes the object to conf text.
    /// </summary>
    /// <param name="obj">The object to be serialized.</param>
    /// <param name="key">The key of conf identifiers in the text.</param>
    /// <param name="multiline">True to return multiline text, otherwise false.</param>
    /// <returns>Conf text serialized from the object.</returns>
    public static string ConfText(this object obj, string key = null, bool? multiline = null) {
        return SourceProvider.GetConfSource(obj, key, multiline);
    }

    /// <summary>
    /// Populates the object from conf text.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="obj"/>.</typeparam>
    /// <param name="obj">The object to be populated.</param>
    /// <param name="text">The conf text.</param>
    /// <param name="key">The key of identifiers for the object in the conf text.</param>
    /// <returns>The populated instance of <typeparamref name="T"/>.</returns>
    public static T ConfFrom<T>(this T obj, string text, string key = null) {
        return new ConfContainer { Source = text, ContentProvider = ContentProvider }.Configure(obj, key);
    }
}
