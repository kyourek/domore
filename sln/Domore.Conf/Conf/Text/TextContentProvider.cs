﻿using Domore.Conf.Text.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Text {
    internal sealed class TextContentProvider : IConfContentProvider {
        private readonly TokenParser Parse = new();

        public ConfContent GetConfContent(object source, IEnumerable<object> sources) {
            var s = $"{source}";
            var p = Parse.Pairs(s);
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
