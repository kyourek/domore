using System;
using System.Collections.Generic;

namespace Domore.Conf.Text.Parsing {
    using Tokens;

    internal sealed class TokenParser {
        public static IConfKey Key(string s) {
            if (null == s) throw new ArgumentNullException(nameof(s));
            var sep = s.Contains("\n") ? '\n' : ';';
            var key = new KeyBuilder(sep);
            var token = key as Token;
            for (var i = 0; i < s.Length; i++) {
                if (token == null) {
                    return key;
                }
                if (token is Invalid) {
                    return key;
                }
                if (token is TokenBuilder builder) {
                    token = builder.Build(s, ref i);
                }
                if (token is Complete complete) {
                    return key;
                }
            }
            return key;
        }

        public IEnumerable<IConfPair> Pairs(string text) {
            if (null == text) throw new ArgumentNullException(nameof(text));
            var sep = text.Contains("\n") ? '\n' : ';';
            var token = new KeyBuilder(sep) as Token;
            for (var i = 0; i < text.Length; i++) {
                if (token == null) {
                    break;
                }
                if (token is Invalid) {
                    for (; i < text.Length; i++) {
                        if (text[i] == sep) {
                            token = new KeyBuilder(sep);
                            break;
                        }
                    }
                }
                if (token is TokenBuilder builder) {
                    token = builder.Build(text, ref i);
                }
                if (token is Complete complete) {
                    yield return complete;
                    token = new KeyBuilder(sep);
                }
            }
            if (token is ValueBuilder value) {
                yield return new Complete(value.Key, value);
            }
        }
    }
}
