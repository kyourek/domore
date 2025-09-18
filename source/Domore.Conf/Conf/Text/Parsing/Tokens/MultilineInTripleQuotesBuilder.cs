using System.Text;

namespace Domore.Conf.Text.Parsing.Tokens;

internal sealed class MultilineInTripleQuotesBuilder : MultiLineValueBuilder {
    private readonly StringBuilder Line = new();

    public MultilineInTripleQuotesBuilder(KeyBuilder key) : base(key) {
    }

    public sealed override Token Build(string s, ref int i) {
        var c = s[i];
        if (c == '"') {
            var mightBeTripleQuote = true;
            for (var j = 0; j < Line.Length; j++) {
                if (char.IsWhiteSpace(Line[j]) == false) {
                    mightBeTripleQuote = false;
                    break;
                }
            }
            if (mightBeTripleQuote) {
                if (Peek(s, i + 1, includeWhiteSpace: true) == '"') {
                    if (Peek(s, i + 2, includeWhiteSpace: true) == '"') {
                        for (var j = i + 3; j < s.Length; j++) {
                            var k = s[j];
                            if (k == Sep) {
                                i = j;
                                Cleanup();
                                if (String.Length > 0) {
                                    return new Complete(Key, this);
                                }
                                else {
                                    return new KeyBuilder(Sep);
                                }
                            }
                            if (char.IsWhiteSpace(k) == false) {
                                break;
                            }
                        }
                    }
                }
            }
        }
        if (c == Sep) {
            String.Append(Line);
            Line.Clear();
        }
        Line.Append(c);
        return this;
    }
}
