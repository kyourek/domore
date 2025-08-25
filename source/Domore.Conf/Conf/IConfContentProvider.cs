namespace Domore.Conf;

/// <summary>
/// Implementations of this interface provide instances of <see cref="ConfContent"/>.
/// </summary>
public interface IConfContentProvider {
    /// <summary>
    /// Provides an instance of <see cref="ConfContent"/> created from the <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The object from which the instance of <see cref="ConfContent"/> is created.</param>
    /// <returns>An instance of <see cref="ConfContent"/> for the specified <paramref name="source"/>.</returns>
    ConfContent GetConfContent(object source);
}
