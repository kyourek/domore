using System.Collections.Generic;

namespace Domore.Conf.Equality;

internal sealed class ConfKeyComparer : IEqualityComparer<IConfKey> {
    private readonly ConfKeyPartComparer Part = new();

    public bool Equals(IConfKey x, IConfKey y) {
        if (x == null && y == null) {
            return true;
        }
        if (x == null || y == null) {
            return false;
        }
        var partsCount = x.Parts?.Count;
        if (partsCount != y.Parts?.Count) {
            return false;
        }
        for (var i = 0; i < partsCount.Value; i++) {
            var partsEqual = Part.Equals(x.Parts[i], y.Parts[i]);
            if (partsEqual == false) {
                return false;
            }
        }
        return true;
    }

    public int GetHashCode(IConfKey obj) {
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
