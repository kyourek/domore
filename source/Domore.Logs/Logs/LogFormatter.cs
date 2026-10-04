using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs; 
internal sealed class LogFormatter {
    private const int MaxEnumerableItems = 100;

    private ConcurrentDictionary<Type, Func<object, string[]>> Lookup { get; } = [];

    private IEnumerable<string> Split(string s) {
        return (s ?? "")
            .Split(['\n'])
            .Select(line => line.Length > 0 && line[line.Length - 1] == '\r'
                ? line.Substring(0, line.Length - 1)
                : line);
    }

    private void Format(object obj, bool expandEnumerable, List<string> lines) {
        try {
            if (obj == null) {
                lines.Add("");
                return;
            }
            if (obj is string s) {
                lines.AddRange(Split(s));
                return;
            }
            if (Lookup.TryGetValue(obj.GetType(), out var format) && format != null) {
                var formatted = format(obj);
                if (formatted != null) {
                    lines.AddRange(formatted);
                    return;
                }
            }
            if (expandEnumerable && obj is IEnumerable enumerable) {
                var itemCount = 0;
                foreach (var item in enumerable) {
                    if (itemCount >= MaxEnumerableItems) {
                        lines.Add("… (truncated)");
                        break;
                    }
                    Format(item, expandEnumerable: false, lines);
                    itemCount++;
                }
                return;
            }
            lines.AddRange(Split(obj.ToString()));
        }
        catch (Exception ex) {
            Logging.Notify(ex);
            var message = ex.Message?.Replace("\r", " ").Replace("\n", " ");
            lines.Add($"<format error: {ex.GetType().Name}: {message}>");
        }
    }

    public bool ExpandEnumerable { get; set; } = true;

    public void Format(Type type, Func<object, string[]> toString) {
        Lookup[type] = toString;
    }

    public string[] Format(params object[] data) {
        if (data == null) return [""];
        var lines = new List<string>();
        foreach (var obj in data) {
            Format(obj, ExpandEnumerable, lines);
        }
        return lines.ToArray();
    }
}
