using System.Collections.Generic;

namespace Domore.Conf {
    public sealed class ConfContent {
        internal IEnumerable<object> Sources { get; }
        internal IEnumerable<IConfPair> Pairs { get; }

        internal ConfContent(IEnumerable<object> sources, IEnumerable<IConfPair> pairs) {
            Pairs = pairs;
            Sources = sources;
        }
    }
}
