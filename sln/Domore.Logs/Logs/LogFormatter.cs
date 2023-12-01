using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Logs {
    internal sealed class LogFormatter {
        private ConcurrentDictionary<Type, Func<object, string[]>> Lookup { get; } = new ConcurrentDictionary<Type, Func<object, string[]>>();

        private IEnumerable<string> Split(string s) {
            return (s ?? "")
                .Split(['\n'])
                .Select(line => line.Length > 0 && line[line.Length - 1] == '\r'
                    ? line.Substring(0, line.Length - 1)
                    : line);
        }

        private IEnumerable<string> Format(object obj, bool expandEnumerable) {
            if (obj == null) return new[] { "" };
            if (obj is string s) return Split(s);
            if (Lookup.Count > 0) {
                if (Lookup.TryGetValue(obj.GetType(), out var format)) {
                    if (format != null) {
                        return format(obj);
                    }
                }
            }
            if (expandEnumerable && obj is IEnumerable enumerable) {
                IEnumerable<IEnumerable<string>> format() {
                    foreach (var item in enumerable) {
                        yield return Format(item, expandEnumerable: false);
                    }
                }
                return format().SelectMany(s => s);
            }
            return Split(obj.ToString());
        }

        private IEnumerable<string> Format(object obj) {
            return Format(obj, ExpandEnumerable);
        }

        public bool ExpandEnumerable { get; set; } = true;

        public void Format(Type type, Func<object, string[]> toString) {
            Lookup[type] = toString;
        }

        public string[] Format(params object[] data) {
            if (data == null) return [""];
            try {
                return data.SelectMany(Format).ToArray();
            }
            catch (Exception ex) {
                return Split($"{ex}").ToArray();
            }
        }
    }
}
