using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs;

internal sealed class LogFormatter {
    private const int DefaultEnumerableItemLimit = 100;
    private const string FormattingFailure = "<formatting failed>";

    private ConcurrentDictionary<Type, Func<object, string[]>> Lookup { get; } = [];

    private int _EnumerableItemLimit = DefaultEnumerableItemLimit;

    private static IEnumerable<string> Split(string s) {
        return (s ?? "")
            .Split(['\n'])
            .Select(line => line.Length > 0 && line[line.Length - 1] == '\r'
                ? line.Substring(0, line.Length - 1)
                : line);
    }

    private static void AddException(Exception exception, List<string> result) {
        string message;
        try {
            message = exception?.ToString();
        }
        catch {
            message = null;
        }
        if (message == null) {
            result.Add(FormattingFailure);
            return;
        }
        foreach (var line in Split(message)) {
            result.Add(line ?? "");
        }
    }

    private static void AddLines(IEnumerable<string> lines, List<string> result) {
        foreach (var line in lines) {
            result.Add(line ?? "");
        }
    }

    private void FormatEnumerable(IEnumerable enumerable, List<string> result) {
        IEnumerator iterator = null;
        var count = 0;
        var itemLimit = EnumerableItemLimit;
        try {
            iterator = enumerable.GetEnumerator();
            while (count < itemLimit) {
                bool hasNext;
                try {
                    hasNext = iterator.MoveNext();
                }
                catch (Exception ex) {
                    AddException(ex, result);
                    return;
                }
                if (hasNext == false) {
                    return;
                }

                count++;
                object item;
                try {
                    item = iterator.Current;
                }
                catch (Exception ex) {
                    AddException(ex, result);
                    continue;
                }
                Format(item, expandEnumerable: false, result);
            }

            bool truncated;
            try {
                truncated = iterator.MoveNext();
            }
            catch (Exception ex) {
                AddException(ex, result);
                return;
            }
            if (truncated) {
                result.Add($"... (truncated after {itemLimit} items)");
            }
        }
        catch (Exception ex) {
            AddException(ex, result);
        }
        finally {
            try {
                (iterator as IDisposable)?.Dispose();
            }
            catch (Exception ex) {
                AddException(ex, result);
            }
        }
    }

    private void Format(object obj, bool expandEnumerable, List<string> result) {
        if (obj == null) {
            result.Add("");
            return;
        }
        if (obj is string s) {
            AddLines(Split(s), result);
            return;
        }
        if (Lookup.TryGetValue(obj.GetType(), out var format) && format != null) {
            try {
                var lines = format(obj);
                if (lines != null) {
                    AddLines(lines, result);
                    return;
                }
            }
            catch (Exception ex) {
                AddException(ex, result);
                return;
            }
        }
        if (expandEnumerable && obj is IEnumerable enumerable) {
            FormatEnumerable(enumerable, result);
            return;
        }
        try {
            AddLines(Split(obj.ToString()), result);
        }
        catch (Exception ex) {
            AddException(ex, result);
        }
    }

    public bool ExpandEnumerable { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of items emitted from each expanded enumerable.
    /// The default is 100. Values must be positive.
    /// </summary>
    public int EnumerableItemLimit {
        get => _EnumerableItemLimit;
        set {
            if (value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "The enumerable item limit must be greater than zero.");
            }
            _EnumerableItemLimit = value;
        }
    }

    public void Format(Type type, Func<object, string[]> toString) {
        Lookup[type] = toString;
    }

    public string[] Format(params object[] data) {
        if (data == null) return [""];
        var result = new List<string>();
        foreach (var obj in data) {
            try {
                Format(obj, ExpandEnumerable, result);
            }
            catch (Exception ex) {
                AddException(ex, result);
            }
        }
        return result.ToArray();
    }
}
