using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Domore.IO {
    internal sealed class PathSpecial {
        private static readonly ConcurrentBag<string> FolderKeys = new ConcurrentBag<string>(Enum.GetNames(typeof(Environment.SpecialFolder)));

        private static readonly ConcurrentDictionary<string, Environment.SpecialFolder> FolderLookup = new ConcurrentDictionary<string, Environment.SpecialFolder>(
            comparer: StringComparer.OrdinalIgnoreCase,
            collection: FolderKeys.Select(folder => new KeyValuePair<string, Environment.SpecialFolder>(folder, (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), folder))));

        private readonly ConcurrentDictionary<Environment.SpecialFolder, string> FolderCache = new();

        private string Lookup(Environment.SpecialFolder folder) {
            if (FolderCache.TryGetValue(folder, out var path) == false) {
                FolderCache[folder] = path = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
            }
            return path;
        }

        public string Expand(string path) {
            if (path == null) return path;
            if (path.Length < 3) return path;
            if (path[0] != '<') return path;
            var sb = new StringBuilder();
            for (var i = 1; i < path.Length; i++) {
                var c = path[i];
                if (c == '>') {
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
