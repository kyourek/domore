using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf;

internal sealed class ConfLookup : IConfLookup {
    private static readonly IEnumerable<string> Empty = new string[] { };

    private ILookup<IConfKey, IConfValue> Lookup =>
        _Lookup ?? (
        _Lookup = Pairs.ToLookup(
            keySelector: p => p.Key,
            elementSelector: p => p.Value,
            comparer: ConfKey.Comparer));
    private ILookup<IConfKey, IConfValue> _Lookup;

    private bool Contains(string key, out IConfKey conf) {
        conf = ConfKey.Build(key);
        return Lookup.Contains(conf);
    }

    public IEnumerable<IConfPair> Pairs { get; }

    public ConfLookup(IEnumerable<IConfPair> pairs) {
        Pairs = pairs ?? throw new ArgumentNullException(nameof(pairs));
    }

    public int Count =>
        Lookup.Count;

    public bool Contains(string key) {
        return Contains(key, out _);
    }

    public IEnumerable<string> All(string key) {
        if (Contains(key, out var conf)) {
            return Lookup[conf].Select(v => v.Content);
        }
        return Empty;
    }

    public string Value(string key) {
        return All(key).LastOrDefault();
    }
}
