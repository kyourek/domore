using System.Text;

namespace Domore.Conf.Text.Parsing.Tokens;

internal sealed class MultilineInBracesBuilder : MultiLineValueBuilder {
    private readonly StringBuilder Line = new();

    public MultilineInBracesBuilder(KeyBuilder key) : base(key) {
    }

    public sealed override Token Build(string s, ref int i) {
        var c = s[i];
        var lastChar = i == s.Length - 1;
        var lastCharIsClosingTag = lastChar && c == '}';
        if (lastCharIsClosingTag) {
            return Complete();
        }
        if (c == Sep) {
            var tags = 0;
            var whitespace = true;
            for (var j = 0; j < Line.Length; j++) {
                if (Line[j] == '}') {
                    var t = ++tags;
                    if (t > 1) {
                        break;
                    }
                }
                else {
                    var ws = whitespace = whitespace && char.IsWhiteSpace(Line[j]);
                    if (ws == false) {
                        break;
                    }
                }
            }
            if (whitespace && tags == 1) {
                return Complete();
            }
            String.Append(Line);
            Line.Clear();
        }
        Line.Append(c);
        return this;
    }
}
