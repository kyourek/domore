using Domore.Conf.Text.Parsing.Tokens;
using System;
using System.Collections.Generic;

namespace Domore.Conf.Text.Parsing;

internal sealed class TokenParser {
    public static IConfKey Key(string s) {
        if (s is null) {
            throw new ArgumentNullException(nameof(s));
        }
        var sContainsNewLine = s.Contains(
#if NETCOREAPP
            '\n'
#else
            "\n"
#endif
            );
        var sep = sContainsNewLine ? '\n' : ';';
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
        if (text is null) {
            throw new ArgumentNullException(nameof(text));
        }
        var textContainsNewLine = text.Contains(
#if NETCOREAPP
            '\n'
#else
            "\n"
#endif
            );
        var sep = textContainsNewLine ? '\n' : ';';
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
