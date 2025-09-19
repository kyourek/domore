using System;
using System.Collections.Generic;

namespace Domore.Collections.Generic;

internal sealed class NormalStringComparer : IEqualityComparer<string> {
    internal static string Normalize(string s) {
        return string.Join("", (s ?? "").Split());
    }

    public bool Equals(string x, string y) {
        var nx = Normalize(x);
        var ny = Normalize(y);
        return StringComparer.OrdinalIgnoreCase.Equals(nx, ny);
    }

    public int GetHashCode(string obj) {
        var n = Normalize(obj);
        return StringComparer.OrdinalIgnoreCase.GetHashCode(n);
    }
}
