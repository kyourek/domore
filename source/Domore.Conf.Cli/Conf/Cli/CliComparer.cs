using System.Collections;
using System.Collections.Generic;

namespace Domore.Conf.Cli {
    public sealed class CliComparer : IEqualityComparer, IEqualityComparer<string> {
        public bool Equals(string x, string y) {
            return Token.Equals(x, y);
        }

        public int GetHashCode(string obj) {
            return EqualityComparer<string>.Default.GetHashCode(obj);
        }

        bool IEqualityComparer.Equals(object x, object y) {
            return x is string xs && y is string ys
                ? Equals(xs, ys)
                : EqualityComparer<object>.Default.Equals(x, y);
        }

        int IEqualityComparer.GetHashCode(object obj) {
            return obj is string s
                ? GetHashCode(s)
                : EqualityComparer<object>.Default.GetHashCode(obj);
        }
    }
}
