using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Domore.IO;

internal sealed class PathFormatter {
    private static readonly ConcurrentBag<string> FolderKeys = [.. Enum.GetNames(typeof(Environment.SpecialFolder))];
    private static readonly HashSet<char> InvalidFileNameChars = [.. Path.GetInvalidFileNameChars()];

    private static readonly ConcurrentDictionary<string, Environment.SpecialFolder> FolderLookup = new(
        comparer: StringComparer.OrdinalIgnoreCase,
        collection: FolderKeys.Select(folder => new KeyValuePair<string, Environment.SpecialFolder>(
            folder,
            (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), folder))));

    private static readonly ConcurrentDictionary<Environment.SpecialFolder, string> FolderCache = new();

    private static string Lookup(Environment.SpecialFolder folder) {
        if (FolderCache.TryGetValue(folder, out var path) == false) {
            FolderCache[folder] = path =
                Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
        }
        return path;
    }

    private static string Format(string path, IEnumerable<KeyValuePair<string, Func<object>>> args) {
        if (string.IsNullOrWhiteSpace(path)) {
            return "";
        }
        var root = Path.GetPathRoot(path) ?? "";
        var parts = path
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(part => part != "")
            .ToArray();
        var rootPartCount = root
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Count(part => part != "");
        if (parts.Length == 0) {
            return root;
        }
        args = args ?? new Dictionary<string, Func<object>> {
            { "AppDomain.FriendlyName", () => AppDomain.CurrentDomain?.FriendlyName },
            { "Thread.Name", () => Thread.CurrentThread?.Name },
            { "Thread.ManagedThreadId", () => Thread.CurrentThread?.ManagedThreadId }
        };
        var replacements = args
            .Select(arg => new KeyValuePair<string, Func<object>>("{" + arg.Key + "}", arg.Value))
            .ToArray();
        var values = new string[replacements.Length];
        var resolved = new bool[replacements.Length];
        string formatPart(string part) {
            var builder = default(StringBuilder);
            for (var index = 0; index < part.Length;) {
                var match = -1;
                for (var replacementIndex = 0; replacementIndex < replacements.Length; replacementIndex++) {
                    var key = replacements[replacementIndex].Key;
                    if (index + key.Length <= part.Length &&
                        string.Compare(part, index, key, 0, key.Length, StringComparison.OrdinalIgnoreCase) == 0) {
                        match = replacementIndex;
                        break;
                    }
                }
                if (match < 0) {
                    builder?.Append(part[index]);
                    index++;
                    continue;
                }
                if (builder is null) {
                    builder = new StringBuilder(part.Length);
                    builder.Append(part, 0, index);
                }
                if (resolved[match] == false) {
                    var
                    value = $"{replacements[match].Value?.Invoke()}";
                    value = new string([.. value.Select(c => InvalidFileNameChars.Contains(c) ? '_' : c)]);
                    values[match] = value;
                    resolved[match] = true;
                }
                builder.Append(values[match]);
                index += replacements[match].Key.Length;
            }
            return builder?.ToString() ?? part;
        }
        for (var i = 0; i < parts.Length; i++) {
            parts[i] = formatPart(parts[i]);
        }
        var rootBuilder = new StringBuilder(root.Length);
        var rootPartIndex = 0;
        for (var i = 0; i < root.Length;) {
            if (root[i] == Path.DirectorySeparatorChar || root[i] == Path.AltDirectorySeparatorChar) {
                rootBuilder.Append(root[i++]);
            }
            else {
                while (i < root.Length &&
                       root[i] != Path.DirectorySeparatorChar &&
                       root[i] != Path.AltDirectorySeparatorChar) {
                    i++;
                }
                rootBuilder.Append(parts[rootPartIndex++]);
            }
        }
        var formattedRoot = rootBuilder.ToString();
        var relativeParts = parts.Skip(rootPartCount).ToArray();
        if (relativeParts.Length == 0) {
            return formattedRoot;
        }
        var formatted = Path.Combine(relativeParts);
        if (root.Length == 0) {
            return formatted;
        }
        var lastRootChar = formattedRoot[formattedRoot.Length - 1];
        if (lastRootChar == Path.DirectorySeparatorChar ||
            lastRootChar == Path.AltDirectorySeparatorChar ||
            lastRootChar == Path.VolumeSeparatorChar) {
            return formattedRoot + formatted;
        }
        return formattedRoot + Path.DirectorySeparatorChar + formatted;
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
                            if (sub.Length == 1 && (sub[0] == Path.DirectorySeparatorChar ||
                                                    sub[0] == Path.AltDirectorySeparatorChar)) {
                                return specialPath + sub;
                            }
                            return Path.Combine(
                                specialPath,
                                sub.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
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
