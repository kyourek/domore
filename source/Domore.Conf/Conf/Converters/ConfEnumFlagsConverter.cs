using Domore.Conf.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Converters;

internal sealed class ConfEnumFlagsConverter {
    private static readonly string DefaultSeparators = "+|&,/\\";
    private static readonly Dictionary<Type, Dictionary<string, HashSet<string>>> AliasCache = [];

    public object Convert(string value, Type toType) {
        if (null == value) throw new ArgumentNullException(nameof(value));
        if (null == toType) throw new ArgumentNullException(nameof(toType));
        var type = Nullable.GetUnderlyingType(toType) ?? toType;
        var alias = default(Dictionary<string, HashSet<string>>);
        var aliasCache = AliasCache;
        lock (aliasCache) {
            if (aliasCache.TryGetValue(type, out alias) == false) {
                aliasCache[type] = alias = type
                    .GetEnumAlias()
                    .ToDictionary(pair => pair.Key.Name, pair => pair.Value);
            }
        }
        var separators = Separators;
        if (separators == null || separators.Length == 0) {
            separators = DefaultSeparators;
        }
        foreach (var c in separators) {
            if (value.Contains(c)) {
                var items = value
                    .Split(c)
                    .Select(s => s?.Trim() ?? "")
                    .Where(s => s != "")
                    .Select(s => alias.FirstOrDefault(pair => pair.Value.Contains(s)).Key ?? s);
                var parsableString = string.Join(",", items);
                var parseResult = Enum.Parse(type, parsableString, ignoreCase: true);
                return parseResult;
            }
        }
        value = alias.FirstOrDefault(pair => pair.Value.Contains(value)).Key ?? value;
        return Enum.Parse(type, value, ignoreCase: true);
    }

    public string Separators { get; set; }
}
