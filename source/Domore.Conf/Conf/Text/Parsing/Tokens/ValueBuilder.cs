using System;
using System.Text;

namespace Domore.Conf.Text.Parsing.Tokens;

internal abstract class ValueBuilder : TokenBuilder, IConfValue {
    protected StringBuilder String { get; } = new();

    protected sealed override string Create() {
        return String.ToString();
    }

    public KeyBuilder Key { get; }

    public ValueBuilder(KeyBuilder key) : base((key ?? throw new ArgumentNullException(nameof(key))).Sep) {
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }
}
