using System.Text;

namespace Domore.Conf.Text.Parsing.Tokens;

internal sealed class MultilineInTripleQuotesBuilder : MultiLineValueBuilder {
    private readonly StringBuilder Line = new();

    public char QuoteChar { get; }

    public MultilineInTripleQuotesBuilder(KeyBuilder key, char quoteChar) : base(key) {
        QuoteChar = quoteChar;
    }

    public sealed override Token Build(string s, ref int i) {
        var q = QuoteChar;
        var c = s[i];
        if (c == q) {
            var mightBeTripleQuote = true;
            for (var j = 0; j < Line.Length; j++) {
                if (char.IsWhiteSpace(Line[j]) == false) {
                    mightBeTripleQuote = false;
                    break;
                }
            }
            if (mightBeTripleQuote) {
                if (Peek(s, i + 1, includeWhiteSpace: true) == q) {
                    if (Peek(s, i + 2, includeWhiteSpace: true) == q) {
                        if (s.Length == i + 3) {
                            i = i + 3;
                            return Complete();
                        }
                        for (var j = i + 3; j < s.Length; j++) {
                            var k = s[j];
                            if (k == Sep) {
                                i = j;
                                return Complete();
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
