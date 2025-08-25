using Domore.Conf.Extensions;
using Domore.Conf.IO;
using Domore.Conf.Text.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Text;

internal sealed class TextContentProvider : ConfContentProviderBase {
    private readonly TokenParser Parse = new();

    private FileOrTextContentProvider FileOrText => _FileOrText ??= new();
    private FileOrTextContentProvider _FileOrText;

    private List<IConfPair> ConfigInclude(List<IConfPair> existing, List<object> sources, ConfContentProviderContext context) {
        if (null == existing) throw new ArgumentNullException(nameof(existing));
        var configKey = context?.Special?.Trim() ?? "";
        if (configKey == "") {
            return existing;
        }
        for (var i = 0; i < existing.Count; i++) {
            var existingPair = existing[i];
            var existingPairKey = existingPair.Key;
            if (existingPairKey.StartsWith(configKey)) {
                var config = new TextContentConfig().ConfFrom(existingPair.Content, key: configKey);
                var includes = config.Include;
                if (includes.Count > 0) {
                    var j = 1;
                    foreach (var include in includes) {
                        var includeProvider = FileOrText;
                        var includeContent = includeProvider.GetConfContent(include, sources, context);
                        var includePairs = includeContent.Pairs.ToList();
                        existing.InsertRange(i + j, includePairs);
                        j += includePairs.Count;
                    }
                }
            }
        }
        return existing;
    }

    public sealed override ConfContent GetConfContent(object source, IEnumerable<object> sources, ConfContentProviderContext context) {
        var s = $"{source}";
        var sourceList = sources?.Concat(new[] { s })?.ToList();
        if (sourceList == null) {
            sourceList = new List<object>(new[] { s });
        }
        var
        p = Parse.Pairs(s).ToList();
        p = ConfigInclude(p, sourceList, context);
        var c = new ConfContent(
            pairs: p,
            sources: sourceList);
        return c;
    }
}
