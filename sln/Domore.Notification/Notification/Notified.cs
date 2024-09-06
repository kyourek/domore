using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

namespace Domore.Notification {
    /// <summary>
    /// Wraps a value for notification.
    /// </summary>
    public abstract class Notified {
        private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> PropertyChangedEventArgsCache = new();
        internal PropertyChangedEventArgs PropertyChangedEventArgs { get; }

        internal Notified(string name, bool eventArgsCache) {
            Name = name;
            PropertyChangedEventArgs = eventArgsCache
                ? PropertyChangedEventArgsCache.GetOrAdd(Name, n => new(n))
                : new(Name);
        }

        internal Notified(string name) : this(name, true) {
        }

        /// <summary>
        /// Gets the name of the property wrapped.
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    /// Wraps a value for notification.
    /// </summary>
    /// <typeparam name="T">The type of value wrapped.</typeparam>
    public sealed class Notified<T> : Notified {
        private static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        internal bool Change(T value) {
            if (Comparer.Equals(Value, value)) {
                return false;
            }
            Value = value;
            return true;
        }

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Creates a new wrapper with the specified name.
        /// </summary>
        /// <param name="name">The name of the property wrapped.</param>
        public Notified(string name) : base(name) {
        }
    }
}
