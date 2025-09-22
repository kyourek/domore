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
        var specialKey = context?.Special?.Trim() ?? "";
        if (specialKey == "") {
            return existing;
        }
        for (var i = 0; i < existing.Count; i++) {
            var existingPair = existing[i];
            var existingPairKey = existingPair.Key;
            if (existingPairKey.StartsWith(specialKey)) {
                var special = new TextContentSpecial().ConfFrom(existingPair.Content, key: specialKey);
                var includes = special.Include;
                if (includes?.Count > 0) {
                    var j = 1;
                    foreach (var include in includes) {
                        if (!string.IsNullOrWhiteSpace(include)) {
                            var includeProvider = FileOrText;
                            var includeContent = includeProvider.GetConfContent(include, sources, context);
                            var includePairs = includeContent.Pairs.ToList();
                            existing.InsertRange(i + j, includePairs);
                            j += includePairs.Count;
                        }
                    }
                }
                var prefixed = special.Key;
                if (prefixed?.Count > 0) {
                    var j = 1;
                    foreach (var prefix in prefixed) {
                        if (!string.IsNullOrWhiteSpace(prefix.Value)) {
                            var prefixProvider = FileOrText;
                            var prefixContent = prefixProvider.GetConfContent(prefix.Value, sources, context);
                            var prefixPairs = prefixContent.Pairs.ToList();
                            existing.InsertRange(i + j, prefixPairs.Select(pair => new ConfPair(
                                key: ConfKey.Build($"{prefix.Key}.{pair.Key}"),
                                value: pair.Value)));
                            j += prefixPairs.Count;
                        }
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
            sourceList = [s];
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
