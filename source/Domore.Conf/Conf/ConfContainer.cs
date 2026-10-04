using System;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf;

internal sealed class ConfContainer : IConfContainer {
    private ConfPopulator Populator => field ??= ConfPopulator.Cached;

    private ConfContent Content {
        get => field ??= MakeContent();
        set;
    }

    private ConfContent MakeContent() {
        var provider = ContentProvider;
        return
            provider is not ConfContentProviderBase providerBase
                ? provider.GetConfContent(Source)
                : providerBase.GetConfContent(Source, InitialSources, new ConfContentProviderContext {
                    Special = Special,
                    BaseDirectory = SourceDirectory,
                    IncludeEmptyValues = IncludeEmptyStrings
                });
    }

    internal IEnumerable<object> InitialSources { get; set; }
    internal string SourceDirectory { get; set; }
    internal bool IncludeEmptyStrings { get; set; }

    public IConfContentProvider ContentProvider {
        get => field ??= new ConfContentProvider();
        set {
            if (field != value) {
                field = value;
                Content = null;
                Lookup = null;
            }
        }
    }

    public object Source {
        get;
        set {
            if (field != value) {
                field = value;
                Content = null;
                Lookup = null;
            }
        }
    }

    public string Special {
        get;
        set {
            if (field != value) {
                field = value;
                Content = null;
                Lookup = null;
            }
        }
    }

    public IEnumerable<object> Sources =>
        Content.Sources;

    public IConfLookup Lookup {
        get => field ??= new ConfLookup(Content.Pairs);
        private set;
    }

    public T Configure<T>(T target, string key = null) {
        if (null == target) throw new ArgumentNullException(nameof(target));
        var k = key ?? target?.GetType()?.Name ?? typeof(T).Name;
        var p = k == ""
            ? Content.Pairs
            : Content.Pairs.Where(pair => pair.Key.StartsWith(k))
                           .Select(pair => new ConfPair(pair.Key.Skip(), pair.Value));
        Populator.Populate(target, this, p, IncludeEmptyStrings);
        return target;
    }

    internal T Configure<T>(T target, string key, Func<IConfPair, bool> first) {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (first is null) throw new ArgumentNullException(nameof(first));
        var k = key ?? target?.GetType()?.Name ?? typeof(T).Name;
        var source = k == ""
            ? Content.Pairs
            : Content.Pairs.Where(pair => pair.Key.StartsWith(k))
                           .Select(pair => new ConfPair(pair.Key.Skip(), pair.Value));
        var ordered = new List<IConfPair>();
        var remaining = new List<IConfPair>();
        foreach (var pair in source) {
            (first(pair) ? ordered : remaining).Add(pair);
        }
        ordered.AddRange(remaining);
        Populator.Populate(target, this, ordered, IncludeEmptyStrings);
        return target;
    }

    public IEnumerable<T> Configure<T>(Func<T> factory,
                                       string key = null,
                                       IEqualityComparer<string> comparer = null) {
        if (factory is null) {
            throw new ArgumentNullException(nameof(factory));
        }
        var k = key ?? typeof(T).Name;
        var groups = Content.Pairs
            .Where(pair => pair.Key.StartsWith(k))
            .Where(pair => pair.Key.Parts.Count > 0)
            .Where(pair => pair.Key.Parts[0].Indices.Count <= 1)
            .GroupBy(pair => pair.Key.Parts[0].Indices.Count == 1
                ? pair.Key.Parts[0].Indices[0].Content
                : null, comparer);
        foreach (var group in groups) {
            var target = factory();
            var pairs = group.Select(pair => new ConfPair(pair.Key.Skip(), pair.Value));
            Populator.Populate(target, this, pairs, IncludeEmptyStrings);
            yield return target;
        }
    }

    public IEnumerable<KeyValuePair<string, T>> Configure<T>(Func<string, T> factory,
                                                             string key = null,
                                                             IEqualityComparer<string> comparer = null) {
        if (factory is null) {
            throw new ArgumentNullException(nameof(factory));
        }
        var k = key ?? typeof(T).Name;
        var groups = Content.Pairs
            .Where(pair => pair.Key.StartsWith(k))
            .Where(pair => pair.Key.Parts.Count > 0)
            .Where(pair => pair.Key.Parts[0].Indices.Count <= 1)
            .GroupBy(pair => pair.Key.Parts[0].Indices.Count == 1
                ? pair.Key.Parts[0].Indices[0].Content
                : null, comparer);
        foreach (var group in groups) {
            var target = factory(group.Key);
            var pairs = group.Select(pair => new ConfPair(pair.Key.Skip(), pair.Value));
            Populator.Populate(target, this, pairs, IncludeEmptyStrings);
            yield return new KeyValuePair<string, T>(group.Key, target);
        }
    }
}
