using System.Text;

namespace Domore.Conf.Text.Parsing.Tokens {
    internal sealed class MultilineValueBuilder : ValueBuilder {
        private readonly StringBuilder Line = new StringBuilder();

        private void Cleanup() {
            if (String.Length > 0) {
                if (Sep == '\n') {
                    if (String[String.Length - 1] == '\r') {
                        String.Remove(String.Length - 1, 1);
                    }
                }
                var whitespace = true;
                for (var i = 0; i < String.Length; i++) {
                    var ws = whitespace = whitespace && char.IsWhiteSpace(String[i]);
                    if (ws == false) {
                        break;
                    }
                }
                if (whitespace) {
                    String.Clear();
                }
            }
        }

        public MultilineValueBuilder(KeyBuilder key) : base(key) {
        }

        public sealed override Token Build(string s, ref int i) {
            var c = s[i];
            var lastChar = i == s.Length - 1;
            var lastCharIsClosingTag = lastChar && c == '}';
            if (lastCharIsClosingTag) {
                Cleanup();
                return new Complete(Key, this);
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
                    Cleanup();
                    if (String.Length > 0) {
                        return new Complete(Key, this);
                    }
                    else {
                        return new KeyBuilder(Sep);
                    }
                }
                String.Append(Line);
                Line.Clear();
            }
            Line.Append(c);
            return this;
        }
    }
}
