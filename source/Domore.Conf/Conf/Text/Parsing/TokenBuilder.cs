namespace Domore.Conf.Text.Parsing;

internal abstract class TokenBuilder : Token {
    protected char? Peek(string s, int i, bool includeWhiteSpace = false) {
        for (; i < s.Length; i++) {
            var c = s[i];
            if (c == Sep) {
                return c;
            }
            if (includeWhiteSpace || (char.IsWhiteSpace(c) == false)) {
                return c;
            }
        }
        return null;
    }

    protected char? Next(string s, ref int i) {
        for (; i < s.Length; i++) {
            var c = s[i];
            if (c == Sep) {
                return c;
            }
            if (char.IsWhiteSpace(c) == false) {
                return c;
            }
        }
        return null;
    }

    public char Sep { get; }

    public TokenBuilder(char sep) {
        Sep = sep;
    }

    public abstract Token Build(string s, ref int i);
}
