using System;
using System.Text;

namespace Domore.Conf.Text.Parsing.Tokens;

internal sealed class KeyIndexPartBuilder : TokenBuilder, IConfKeyIndexPart {
    private int OpenBracket;
    private StringBuilder WhiteSpace { get; } = new();

    protected sealed override string Create() {
        return String.ToString();
    }

    public StringBuilder String { get; } = new();
    public KeyIndexBuilder KeyIndex { get; }

    public KeyIndexPartBuilder(KeyIndexBuilder keyIndex) : base((keyIndex ?? throw new ArgumentNullException(nameof(keyIndex))).Sep) {
        KeyIndex = keyIndex ?? throw new ArgumentNullException(nameof(keyIndex));
        KeyIndex.Parts.Add(this);
    }

    public sealed override Token Build(string s, ref int i) {
        var c = s[i];
        if (c == Sep) {
            return new KeyBuilder(Sep);
        }
        switch (c) {
            case '[':
                OpenBracket++;
                goto default;
            case ']':
                if (OpenBracket == 0) {
                    return KeyIndex.KeyPart;
                }
                OpenBracket--;
                goto default;
            case ',':
                if (OpenBracket == 0) {
                    return new KeyIndexPartBuilder(KeyIndex);
                }
                goto default;
            default:
                if (char.IsWhiteSpace(c)) {
                    if (String.Length > 0) {
                        WhiteSpace.Append(c);
                    }
                }
                else {
                    String.Append(WhiteSpace);
                    String.Append(c);
                    WhiteSpace.Clear();
                }
                return this;
        }
    }
}
