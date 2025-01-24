using System;
using System.Collections.Generic;

namespace Domore.Conf.Equality {
    internal sealed class ConfKeyPartComparer : IEqualityComparer<IConfKeyPart> {
        private readonly ConfKeyIndexComparer Index = new ConfKeyIndexComparer();

        private static string ContentSource(string s) {
            return s == null
                ? null
                : string.Join("", s.Split());
        }

        private static bool ContentEqual(string x, string y) {
            if (x == null && y == null) {
                return true;
            }
            if (x == null || y == null) {
                return false;
            }
            return StringComparer.OrdinalIgnoreCase.Equals(ContentSource(x), ContentSource(y));
        }

        public bool Equals(IConfKeyPart x, IConfKeyPart y) {
            if (x == null && y == null) {
                return true;
            }
            if (x == null || y == null) {
                return false;
            }
            var indexCount = x.Indices?.Count;
            if (indexCount != y.Indices?.Count) {
                return false;
            }
            for (var i = 0; i < indexCount; i++) {
                var indexEqual = Index.Equals(x.Indices[i], y.Indices[i]);
                if (indexEqual == false) {
                    return false;
                }
            }
            var contentEqual = ContentEqual(x.Content, y.Content);
            if (contentEqual == false) {
                return false;
            }
            return true;
        }

        public int GetHashCode(IConfKeyPart obj) {
            unchecked {
                var hashCode = (int)2166136261;
                var indexCount = obj?.Indices?.Count;
                for (var i = 0; i < indexCount; i++) {
                    hashCode = (hashCode * 16777619) ^ Index.GetHashCode(obj.Indices[i]);
                }
                hashCode = (hashCode * 16777619) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(ContentSource(obj?.Content));
                return hashCode;
            }
        }
    }
}
