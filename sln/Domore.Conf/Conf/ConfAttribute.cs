using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Domore.Conf {
    /// <summary>
    /// Specifies behavior of a conf target member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ConfAttribute : Attribute {
        private ConfAttribute(bool ignore, bool ignoreGet, bool ignoreSet, string[] names) {
            Ignore = ignore;
            IgnoreGet = ignoreGet;
            IgnoreSet = ignoreSet;
            Names = new ReadOnlyCollection<string>(new List<string>(names ?? []));
        }

        /// <summary>
        /// Gets whether or not this member is ignored during population.
        /// </summary>
        public bool Ignore { get; }

        /// <summary>
        /// Gets whether or not the member will be included during serialization.
        /// </summary>
        public bool IgnoreGet { get; }

        /// <summary>
        /// Gets whether or not the member will be populated from conf content.
        /// </summary>
        public bool IgnoreSet { get; }

        /// <summary>
        /// Gets additional names, or aliases, for the member in conf content.
        /// </summary>
        public ReadOnlyCollection<string> Names { get; }

        /// <summary>
        /// Specifies behavior of a conf target member.
        /// </summary>
        /// <param name="ignore">True if the member should be ignored during serialization. Otherwise, false.</param>
        public ConfAttribute(bool ignore) : this(ignore: ignore, ignoreGet: ignore, ignoreSet: ignore, names: null) {
        }

        /// <summary>
        /// Specifies behavior of a conf target member.
        /// </summary>
        /// <param name="ignoreGet">True if the member should not be included during serialization. Otherwise, false.</param>
        /// <param name="ignoreSet">True if the member should not be populated from conf content. Otherwise, false.</param>
        public ConfAttribute(bool ignoreGet, bool ignoreSet) : this(ignore: ignoreGet && ignoreSet, ignoreGet: ignoreGet, ignoreSet: ignoreSet, names: null) {
        }

        /// <summary>
        /// Specifies behavior of a conf target member.
        /// </summary>
        /// <param name="names">Additional names, or aliases, for the member in conf content.</param>
        public ConfAttribute(params string[] names) : this(ignore: false, ignoreGet: false, ignoreSet: false, names: names) {
        }
    }
}
