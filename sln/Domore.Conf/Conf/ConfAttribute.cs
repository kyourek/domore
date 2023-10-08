using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Domore.Conf {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ConfAttribute : Attribute {
        private ConfAttribute(bool ignore, bool ignoreGet, bool ignoreSet, string[] names) {
            Ignore = ignore;
            IgnoreGet = ignoreGet;
            IgnoreSet = ignoreSet;
            Names = new ReadOnlyCollection<string>(new List<string>(names ?? new string[] { }));
        }

        public bool Ignore { get; }
        public bool IgnoreGet { get; }
        public bool IgnoreSet { get; }
        public ReadOnlyCollection<string> Names { get; }

        public ConfAttribute(bool ignore) : this(ignore: ignore, ignoreGet: ignore, ignoreSet: ignore, names: null) {
        }

        public ConfAttribute(bool ignoreGet, bool ignoreSet) : this(ignore: ignoreGet && ignoreSet, ignoreGet: ignoreGet, ignoreSet: ignoreSet, names: null) {
        }

        public ConfAttribute(params string[] names) : this(ignore: false, ignoreGet: false, ignoreSet: false, names: names) {
        }
    }
}
