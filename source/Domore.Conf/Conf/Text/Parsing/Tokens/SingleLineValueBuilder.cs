using System.Text;

namespace Domore.Conf.Text.Parsing.Tokens;

internal sealed class SingleLineValueBuilder : ValueBuilder {
    private StringBuilder WhiteSpace { get; } = new();

    public SingleLineValueBuilder(KeyBuilder key) : base(key) {
    }

    public sealed override Token Build(string s, ref int i) {
        var c = s[i];
        if (c == Sep) {
            if (String.Length > 0) {
                return new Complete(Key, this);
            }
            return new KeyBuilder(Sep);
        }
        switch (c) {
            case '{':
                if (String.Length == 0) {
                    for (var j = i + 1; j < s.Length; j++) {
                        if (s[j] == Sep) {
                            i = j;
                            return new MultilineInBracesBuilder(Key);
                        }
                        if (char.IsWhiteSpace(s[j]) == false) {
                            break;
                        }
                    }
                }
                goto default;
            case '"':
                if (String.Length == 0) {
                    var quotes = 1;
                    for (var j = i + 1; j < s.Length; j++) {
                        if (quotes > 3) {
                            break;
                        }
                        if (s[j] == '"') {
                            quotes++;
                        }
                        if (s[j] == Sep) {
                            if (quotes == 3) {
                                i = j;
                                return new MultilineInTripleQuotesBuilder(Key);
                            }
                            break;
                        }
                        if (char.IsWhiteSpace(s[j])) {
                            if (quotes < 3) {
                                break;
                            }
                        }
                    }
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
