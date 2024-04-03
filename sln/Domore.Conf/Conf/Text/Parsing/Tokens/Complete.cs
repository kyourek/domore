using System;

namespace Domore.Conf.Text.Parsing.Tokens {
    internal sealed class Complete : Token, IConfPair {
        protected sealed override string Create() {
            return ConfPair.ToString(this);
        }

        public KeyBuilder Key { get; }
        public ValueBuilder Value { get; }

        public Complete(KeyBuilder key, ValueBuilder value) {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        IConfKey IConfPair.Key => Key;
        IConfValue IConfPair.Value => Value;
    }
}
