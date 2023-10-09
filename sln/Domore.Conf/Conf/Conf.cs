using System;
using System.Collections.Generic;

namespace Domore.Conf {
    public sealed class Conf : IConf {
        private static readonly ConfContainer _Container = new ConfContainer();

        public static IConfContainer Container => _Container;

        public static IEnumerable<object> Sources =>
            _Container.Sources;

        public static object Source {
            get => _Container.Source;
            set => _Container.Source = value;
        }

        public static IConfContentProvider ContentProvider {
            get => _Container.ContentProvider;
            set => _Container.ContentProvider = value;
        }

        public static IConfLookup Lookup =>
            _Container.Lookup;

        public static T Configure<T>(T target, string key = null) {
            return _Container.Configure(target, key);
        }

        public static IEnumerable<T> Configure<T>(Func<T> factory, string key = null, IEqualityComparer<string> comparer = null) {
            return _Container.Configure(factory, key, comparer);
        }

        public static IEnumerable<KeyValuePair<string, T>> Configure<T>(Func<string, T> factory, string key = null, IEqualityComparer<string> comparer = null) {
            return _Container.Configure(factory, key, comparer);
        }

        public static IConfContainer Contain(object source) {
            return new ConfContainer { Source = source };
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
