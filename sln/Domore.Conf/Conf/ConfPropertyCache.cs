using System;
using System.Collections.Generic;

namespace Domore.Conf {
    internal sealed class ConfPropertyCache {
        private readonly Dictionary<Type, Dictionary<string, ConfProperty>> Cache = [];

        public StringComparer StringComparer { get; } = StringComparer.OrdinalIgnoreCase;

        public ConfProperty Get(Type type, string name) {
            var cache = default(Dictionary<string, ConfProperty>);
            lock (Cache) {
                if (Cache.TryGetValue(type, out cache) == false) {
                    Cache[type] = cache = new Dictionary<string, ConfProperty>(StringComparer);
                }
            }
            var property = default(ConfProperty);
            lock (cache) {
                if (cache.TryGetValue(name, out property) == false) {
                    cache[name] = property = new ConfProperty(name, type);
                }
            }
            return property;
        }
    }
}
