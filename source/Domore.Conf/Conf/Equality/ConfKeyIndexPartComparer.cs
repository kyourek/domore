using System;
using System.Collections.Generic;

namespace Domore.Conf.Equality {
    internal sealed class ConfKeyIndexPartComparer : IEqualityComparer<IConfKeyIndexPart> {
        private static string ContentSource(string s) {
            return s?.Trim();
        }

        private static bool ContentEqual(string x, string y) {
            return StringComparer.Ordinal.Equals(ContentSource(x), ContentSource(y));
        }

        public bool Equals(IConfKeyIndexPart x, IConfKeyIndexPart y) {
            if (x == null && y == null) {
                return true;
            }
            if (x == null || y == null) {
                return false;
            }
            return ContentEqual(x.Content, y.Content);
        }

        public int GetHashCode(IConfKeyIndexPart obj) {
            return StringComparer.Ordinal.GetHashCode(ContentSource(obj?.Content));
        }
    }
}
