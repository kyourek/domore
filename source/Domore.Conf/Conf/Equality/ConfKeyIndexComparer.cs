using System.Collections.Generic;

namespace Domore.Conf.Equality;

internal sealed class ConfKeyIndexComparer : IEqualityComparer<IConfKeyIndex> {
    private readonly ConfKeyIndexPartComparer Part = new();

    public bool Equals(IConfKeyIndex x, IConfKeyIndex y) {
        if (x == null && y == null) {
            return true;
        }
        if (x == null || y == null) {
            return false;
        }
        var partCount = x.Parts?.Count;
        if (partCount != y.Parts?.Count) {
            return false;
        }
        for (var i = 0; i < partCount; i++) {
            var partsEqual = Part.Equals(x.Parts[i], y.Parts[i]);
            if (partsEqual == false) {
                return false;
            }
        }
        return true;
    }

    public int GetHashCode(IConfKeyIndex obj) {
        unchecked {
            var hashCode = (int)2166136261;
            var partCount = obj?.Parts?.Count;
            for (var i = 0; i < partCount; i++) {
                hashCode = (hashCode * 16777619) ^ Part.GetHashCode(obj.Parts[i]);
            }
            return hashCode;
        }
    }
}
