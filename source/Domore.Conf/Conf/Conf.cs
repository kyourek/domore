using System;
using System.Collections.Generic;

namespace Domore.Conf {
    /// <summary>
    /// Provider of serialization to and from conf content.
    /// </summary>
    public sealed class Conf : IConf {
        private static readonly ConfContainer _Container = new();

        /// <summary>
        /// Gets the instance of <see cref="IConfContainer"/> used by static methods.
        /// </summary>
        public static IConfContainer Container => _Container;

        /// <summary>
        /// Gets the sources of the static <see cref="IConfContainer"/>.
        /// </summary>
        public static IEnumerable<object> Sources =>
            _Container.Sources;

        /// <summary>
        /// Gets or sets the source of the static <see cref="IConfContainer"/>.
        /// </summary>
        public static object Source {
            get => _Container.Source;
            set => _Container.Source = value;
        }

        /// <summary>
        /// Gets or sets the static instance <see cref="IConfContentProvider"/>.
        /// </summary>
        public static IConfContentProvider ContentProvider {
            get => _Container.ContentProvider;
            set => _Container.ContentProvider = value;
        }

        /// <summary>
        /// Gets or sets the special string used by static methods.
        /// </summary>
        public static string Special {
            get => _Container.Special;
            set => _Container.Special = value;
        }

        /// <summary>
        /// Gets the <see cref="IConfLookup"/> of the static <see cref="IConfContainer"/>.
        /// </summary>
        public static IConfLookup Lookup =>
            _Container.Lookup;

        /// <summary>
        /// Populates the <paramref name="target"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="target"/>.</typeparam>
        /// <param name="target">The object to be populated.</param>
        /// <param name="key">The conf key that identifies the conf items used to populate the <paramref name="target"/>.</param>
        /// <returns>The populated instance of <typeparamref name="T"/>.</returns>
        public static T Configure<T>(T target, string key = null) {
            return _Container.Configure(target, key);
        }

        /// <summary>
        /// Populates properties of items created by <paramref name="factory"/>.
        /// </summary>
        /// <typeparam name="T">The type of item created by <paramref name="factory"/>.</typeparam>
        /// <param name="factory">The factory that creates the items that are populated.</param>
        /// <param name="key">The common key of the items in the serialization source.</param>
        /// <param name="comparer">The equality comparer used to compare the item indexes.</param>
        /// <returns>The collection of objects populated.</returns>
        public static IEnumerable<T> Configure<T>(Func<T> factory, string key = null, IEqualityComparer<string> comparer = null) {
            return _Container.Configure(factory, key, comparer);
        }

        /// <summary>
        /// Populates properties of items created by <paramref name="factory"/>.
        /// </summary>
        /// <typeparam name="T">The type of item created by <paramref name="factory"/>.</typeparam>
        /// <param name="factory">The factory that creates the items that are populated.</param>
        /// <param name="key">The common key of the items in the serialization source.</param>
        /// <param name="comparer">The equality comparer used to compare the item indexes.</param>
        /// <returns>The collection of objects populated as key/value pairs.</returns>
        public static IEnumerable<KeyValuePair<string, T>> Configure<T>(Func<string, T> factory, string key = null, IEqualityComparer<string> comparer = null) {
            return _Container.Configure(factory, key, comparer);
        }

        /// <summary>
        /// Creates an instance of <see cref="IConfContainer"/> around the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The conf source.</param>
        /// <returns>The created instance of <see cref="IConfContainer"/>.</returns>
        public static IConfContainer Contain(object source) {
            return Contain(source, null);
        }

        /// <summary>
        /// Creates an instance of <see cref="IConfContainer"/> around the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The conf source.</param>
        /// <param name="special">The special conf key.</param>
        /// <returns>The created instance of <see cref="IConfContainer"/>.</returns>
        public static IConfContainer Contain(object source, string special) {
            return new ConfContainer { Source = source, Special = special };
        }

        object IConf.Source =>
            Source;

        IEnumerable<object> IConf.Sources =>
            Sources;

        IConfLookup IConf.Lookup =>
            Lookup;

        T IConf.Configure<T>(T target, string key) {
            return Configure(target, key);
        }

        IEnumerable<T> IConf.Configure<T>(Func<T> factory, string key, IEqualityComparer<string> comparer) {
            return Configure(factory, key, comparer);
        }

        IEnumerable<KeyValuePair<string, T>> IConf.Configure<T>(Func<string, T> factory, string key, IEqualityComparer<string> comparer) {
            return Configure(factory, key, comparer);
        }
    }
}
