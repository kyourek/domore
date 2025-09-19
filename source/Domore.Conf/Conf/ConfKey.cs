using Domore.Conf.Equality;
using Domore.Conf.Text.Parsing;
using System;

namespace Domore.Conf;

internal static class ConfKey {
    public static readonly ConfKeyComparer Comparer = new();

    public static bool StartsWith(this IConfKey confKey, string key) {
        if (null == confKey) throw new ArgumentNullException(nameof(confKey));

        var parts = confKey.Parts;
        if (parts.Count < 1) return false;

        var first = parts[0];
        if (first == null) return false;

        var name = first.Content;
        if (name == null) return false;

        return name.Equals(key, StringComparison.OrdinalIgnoreCase);
    }

    public static IConfKey Build(string s) {
        return TokenParser.Key(s);
    }

    public static bool Equals(string s1, string s2) {
        if (s1 == null && s2 == null) {
            return true;
        }
        if (s1 == null || s2 == null) {
            return false;
        }
        var s1t = s1.Trim();
        var s2t = s2.Trim();
        if (s2t == s1t) {
            return true;
        }
        var s1k = Build(s1t);
        var s2k = Build(s2t);
        var kEq = Comparer.Equals(s1k, s2k);
        return kEq;
    }
}
