using System.Collections.Generic;

namespace Domore.Collections.Generic {
    public sealed class NormalStringComparer : IEqualityComparer<string> {
        private static string Normalize(string s) =>
            string.Join("", (s ?? "").Split()).ToUpperInvariant();

        public bool Equals(string x, string y) =>
            Normalize(x) == Normalize(y);

        public int GetHashCode(string obj) =>
            Normalize(obj).GetHashCode();
    }
}
