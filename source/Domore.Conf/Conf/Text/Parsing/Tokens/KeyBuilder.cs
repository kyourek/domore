using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Text.Parsing.Tokens;

internal sealed class KeyBuilder : TokenBuilder, IConfKey {
    protected sealed override string Create() {
        return string.Join(".", Parts);
    }

    public ConfCollection<KeyPartBuilder> Parts { get; } = new ConfCollection<KeyPartBuilder>();

    public KeyBuilder(char sep) : base(sep) {
    }

    public sealed override Token Build(string s, ref int i) {
        var c = Next(s, ref i);
        if (c == null) return null;
        if (c == Sep) return new KeyBuilder(Sep);
        switch (c) {
            default:
                i--;
                return new KeyPartBuilder(this);
        }
    }

    IConfCollection<IConfKeyPart> IConfKey.Parts =>
        Parts;

    IConfKey IConfKey.Skip() {
        return new Skip(1, Parts);
    }

    private sealed class Skip : IConfKey, IConfCollection<IConfKeyPart> {
        public string Content => field ??=
            string.Join(".", Parts.Skip(Count));

        public int Count { get; }
        public IConfCollection<IConfKeyPart> Parts { get; }

        public Skip(int count, IConfCollection<IConfKeyPart> parts) {
            Count = count;
            Parts = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        public override string ToString() {
            return Content;
        }

        IConfCollection<IConfKeyPart> IConfKey.Parts =>
            this;

        IConfKey IConfKey.Skip() {
            return new Skip(Count + 1, Parts);
        }

        IConfKeyPart IConfCollection<IConfKeyPart>.this[int index] =>
            Parts[index + Count];

        int IConfCollection.Count =>
            Parts.Count - Count;

        IEnumerator<IConfKeyPart> IEnumerable<IConfKeyPart>.GetEnumerator() {
            return Parts.Skip(Count).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return Parts.Skip(Count).GetEnumerator();
        }
    }
}
