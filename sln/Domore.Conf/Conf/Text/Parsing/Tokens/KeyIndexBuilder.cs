using System;

namespace Domore.Conf.Text.Parsing.Tokens {
    internal sealed class KeyIndexBuilder : TokenBuilder, IConfKeyIndex {
        protected sealed override string Create() {
            return string.Join(",", Parts);
        }

        public ConfCollection<KeyIndexPartBuilder> Parts { get; } = new ConfCollection<KeyIndexPartBuilder>();
        public KeyPartBuilder KeyPart { get; }

        public KeyIndexBuilder(KeyPartBuilder keyPart) : base((keyPart ?? throw new ArgumentNullException(nameof(keyPart))).Sep) {
            KeyPart = keyPart ?? throw new ArgumentNullException(nameof(keyPart));
            KeyPart.Indices.Add(this);
        }

        public sealed override Token Build(string s, ref int i) {
            var c = Next(s, ref i);
            if (c == null) return null;
            if (c == Sep) return new KeyBuilder(Sep);
            switch (c) {
                default:
                    i--;
                    return new KeyIndexPartBuilder(this);
            }
        }

        IConfCollection<IConfKeyIndexPart> IConfKeyIndex.Parts =>
            Parts;
    }
}
