using System.Collections.Generic;
using System.Text;

namespace Domore.Conf.Cli; 
internal struct Token {
    private Token(string key, string value) {
        Key = key;
        Value = value;
    }

    public string Key { get; }
    public string Value { get; }

    public static IEnumerable<Token> Parse(string line) {
        var s = line?.Trim() ?? "";
        if (s == "") {
            yield break;
        }
        var k = new StringBuilder();
        var v = default(StringBuilder);
        var b = k;
        var q = default(char?);
        foreach (var c in s) {
            if (c == '\'' || c == '\"') {
                if (q == c) {
                    q = null;
                }
                else {
                    if (q == null) {
                        q = c;
                    }
                    else {
                        b.Append(c);
                    }
                }
            }
            else {
                if (c == '=') {
                    if (q.HasValue) {
                        b.Append(c);
                    }
                    else {
                        b = v = v ?? new StringBuilder();
                    }
                }
                else {
                    if (char.IsWhiteSpace(c)) {
                        if (q == null) {
                            if (k.Length > 0 || v?.Length > 0) {
                                yield return new Token(k.ToString(), v?.ToString());
                                k = new StringBuilder();
                                v = null;
                                b = k;
                            }
                        }
                        else {
                            b.Append(c);
                        }
                    }
                    else {
                        b.Append(c);
                    }
                }
            }
        }
        if (k.Length > 0 || v?.Length > 0) {
            yield return new Token(k.ToString(), v?.ToString());
        }
    }

    public static bool Equals(string a, string b) {
        var ap = Parse(a);
        var bp = Parse(b);
        using (var ae = ap.GetEnumerator())
        using (var be = bp.GetEnumerator()) {
            for (; ; ) {
                var am = ae.MoveNext();
                var bm = be.MoveNext();
                if (bm == false && am == false) {
                    return true;
                }
                if (bm == false || am == false) {
                    return false;
                }
                var at = ae.Current;
                var bt = be.Current;
                if (string.Equals(bt.Value, at.Value) == false) {
                    return false;
                }
                if (ConfKey.Equals(bt.Key, at.Key) == false) {
                    return false;
                }
            }
        }
    }
}
