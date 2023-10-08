using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Text {
    using Parsing;

    public sealed class TextContentProvider : IConfContentProvider {
        private readonly TokenParser Parse = new TokenParser();

        public ConfContent GetConfContent(object source, IEnumerable<object> sources) {
            var s = $"{source}";
            var p = Parse.Pairs(s);
            var a = p.ToArray();
            var c = new ConfContent(
                pairs: p.ToList(),
                sources: sources?
                    .Concat(new[] { s })?
                    .ToArray() ?? new[] { s });
            return c;
        }

        ConfContent IConfContentProvider.GetConfContent(object source) {
            return GetConfContent(source, null);
        }
    }
}
