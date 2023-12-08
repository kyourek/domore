using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Domore.IO {
    internal sealed class PathFormatter {
        private static readonly ConcurrentBag<string> FolderKeys = new ConcurrentBag<string>(Enum.GetNames(typeof(Environment.SpecialFolder)));
        private static readonly HashSet<char> InvalidFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars());

        private static readonly ConcurrentDictionary<string, Environment.SpecialFolder> FolderLookup = new ConcurrentDictionary<string, Environment.SpecialFolder>(
            comparer: StringComparer.OrdinalIgnoreCase,
            collection: FolderKeys.Select(folder => new KeyValuePair<string, Environment.SpecialFolder>(folder, (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), folder))));

        private static readonly ConcurrentDictionary<Environment.SpecialFolder, string> FolderCache = new();

        private static string Lookup(Environment.SpecialFolder folder) {
            if (FolderCache.TryGetValue(folder, out var path) == false) {
                FolderCache[folder] = path = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
            }
            return path;
        }

        private static string Format(string path, IEnumerable<KeyValuePair<string, Func<object>>> args) {
            if (string.IsNullOrWhiteSpace(path)) {
                return "";
            }
            var parts = path
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(part => part != "")
                .ToArray();
            if (parts.Length == 0) {
                return "";
            }
            if (parts[0][parts[0].Length - 1] == Path.VolumeSeparatorChar) {
                if (path.StartsWith($"{parts[0]}{Path.DirectorySeparatorChar}") || path.StartsWith($"{parts[0]}{Path.AltDirectorySeparatorChar}")) {
                    parts[0] = parts[0] + Path.DirectorySeparatorChar;
                }
            }
            args = args ?? new Dictionary<string, Func<object>> {
                { "AppDomain.FriendlyName", () => AppDomain.CurrentDomain?.FriendlyName },
                { "Thread.Name", () => Thread.CurrentThread?.Name },
                { "Thread.ManagedThreadId", () => Thread.CurrentThread?.ManagedThreadId }
            };
            foreach (var arg in args) {
                var key = "{" + arg.Key + "}";
                var val = default(string);
                for (var i = 0; i < parts.Length; i++) {
                    var idx = parts[i].IndexOf(key, StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) {
                        continue;
                    }
                    if (val == null) {
                        val = $"{arg.Value?.Invoke()}";
                        lock (InvalidFileNameChars) {
                            val = new string(val.Select(c => InvalidFileNameChars.Contains(c) ? '_' : c).ToArray());
                        }
                    }
                    parts[i] = parts[i].Remove(idx, key.Length);
                    parts[i] = parts[i].Insert(idx, val);
                }
            }
            return Path.Combine(parts);
        }

        public string Format(string path) {
            return Format(path, null);
        }

        public string Expand(string path) {
            if (path == null) return path;
            if (path.Length < 3) return path;
            if (path[0] != '{') return path;
            var sb = new StringBuilder();
            for (var i = 1; i < path.Length; i++) {
                var c = path[i];
                if (c == '}') {
                    if (FolderLookup.TryGetValue(sb.ToString(), out var specialFolder)) {
                        var specialPath = Lookup(specialFolder);
                        if (specialPath != null && specialPath != "") {
                            var pathSubIndex = i + 1;
                            if (path.Length > pathSubIndex) {
                                var sub = path.Substring(pathSubIndex);
                                if (sub.Length == 1 && (sub[0] == Path.DirectorySeparatorChar || sub[0] == Path.AltDirectorySeparatorChar)) {
                                    return specialPath + sub;
                                }
                                return Path.Combine(specialPath, sub.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                            }
                            return specialPath;
                        }
                    }
                    return path;
                }
                sb.Append(c);
            }
            return path;
        }
    }
}
