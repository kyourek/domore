using Domore.Conf.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Domore.Conf.Text;

internal sealed class TextSourceProvider {
    private static string Format(object value) {
        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static string Multiline(string s) {
        if (s?.Contains('\n') != true) {
            return s;
        }
        var line = default(StringBuilder);
        var open = "{";
        var close = "}";
        for (var i = 0; i < s.Length; i++) {
            if (s[i] == '\n') {
                if (line?.Length > 0) {
                    if (line.ToString().Trim() == "}") {
                        open = close = "\"\"\"";
                        break;
                    }
                    line.Clear();
                }
            }
            else {
                line ??= new();
                line.Append(s[i]);
            }
        }
        if (line?.Length > 0) {
            if (line.ToString().Trim() == "}") {
                open = close = "\"\"\"";
            }
        }
        return string.Join(Environment.NewLine, open, s, close);
    }

    private IEnumerable<KeyValuePair<string, string>> ListConfContents(IList list,
                                                                       string key,
                                                                       List<object> referenceList) {
        if (null == list) throw new ArgumentNullException(nameof(list));
        if (key == null) {
            var listType = list.GetType();
            var listArgs = listType.GetGenericArguments();
            if (listArgs.Length == 1) {
                key = listArgs[0].Name;
            }
        }
        for (var i = 0; i < list.Count; i++) {
            var k = $"{key}[{Format(i)}]";
            var v = list[i];
            if (v is not null) {
                var vType = v.GetType();
                if (vType.IsValueType || vType == typeof(string)) {
                    yield return new KeyValuePair<string, string>(
                        key: k,
                        value: Multiline(Format(v)));
                }
                else {
                    foreach (var kvp in ConfContents(v, k, help: false, referenceList)) {
                        yield return kvp;
                    }
                }
            }
        }
    }

    private IEnumerable<KeyValuePair<string, string>> DictionaryConfContents(IDictionary dictionary,
                                                                             string key,
                                                                             List<object> referenceList) {
        if (dictionary is null) {
            throw new ArgumentNullException(nameof(dictionary));
        }
        if (key is null) {
            var dictType = dictionary.GetType();
            var dictArgs = dictType.GetGenericArguments();
            if (dictArgs.Length == 2) {
                key = dictArgs[1].Name;
            }
        }
        var dKeys = dictionary.Keys;
        if (dKeys is not null) {
            foreach (var dKey in dKeys) {
                var k = $"{key}[{Format(dKey)}]";
                var v = dictionary[dKey];
                if (v is not null) {
                    var vType = v.GetType();
                    if (vType.IsValueType || vType == typeof(string)) {
                        yield return new KeyValuePair<string, string>(
                            key: k,
                            value: Multiline(Format(v)));
                    }
                    else {
                        foreach (var kvp in ConfContents(v, k, help: false, referenceList)) {
                            yield return kvp;
                        }
                    }
                }
            }
        }
    }

    private IEnumerable<KeyValuePair<string, string>> DefaultConfContents(object source,
                                                                          string key,
                                                                          bool help,
                                                                          List<object> referenceList) {
        var type = source.GetType();
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(property => property.Name);
        key = key ?? type.Name;
        string k(string s) => key == "" ? s : string.Join(".", key, s);
        foreach (var property in properties) {
            if (property.CanRead) {
                var confAttr = property.GetConfAttribute();
                var confIgnore = true == confAttr?.IgnoreGet;
                if (confIgnore == false) {
                    var parameters = property.GetIndexParameters();
                    if (parameters.Length == 0) {
                        var propertyValue = property.GetValue(source, null);
                        if (propertyValue is not null) {
                            var helpTxt = !help ? null : property.GetHelpAttribute()?.Format("# ");
                            if (helpTxt is not null) {
                                yield return new KeyValuePair<string, string>("", null);
                                yield return new KeyValuePair<string, string>(helpTxt, null);
                            }
                            var propertyValueType = propertyValue.GetType();
                            if (propertyValueType.IsValueType || propertyValueType == typeof(string) || propertyValue is Type) {
                                if (property.CanWrite) {
                                    var pairKey = k(property.Name);
                                    var pairValue = propertyValue is Type representedType
                                        ? representedType.AssemblyQualifiedName ?? representedType.FullName ?? representedType.ToString()
                                        : Format(propertyValue);
                                    if (pairValue.Contains("\n")) {
                                        pairValue = Multiline(pairValue);
                                    }
                                    var pair = new KeyValuePair<string, string>(pairKey, pairValue);
                                    yield return pair;
                                }
                            }
                            else {
                                var cc = ConfContents(
                                    source: propertyValue,
                                    key: k(property.Name),
                                    help: help, referenceList);
                                foreach (var item in cc) {
                                    yield return item;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private IEnumerable<KeyValuePair<string, string>> ConfContents(object source,
                                                                   string key,
                                                                   bool help,
                                                                   List<object> referenceList) {
        if (referenceList is null) {
            throw new ArgumentNullException(nameof(referenceList));
        }
        if (source is null) {
            throw new ArgumentNullException(nameof(source));
        }
        if (referenceList.Any(r => ReferenceEquals(r, source))) {
            throw new ConfCircularReferenceException(source);
        }
        referenceList.Add(source);
        try {
            IEnumerable<KeyValuePair<string, string>> contents;
            if (source is IList list) {
                contents = ListConfContents(list, key, referenceList);
            }
            else if (source is IDictionary dictionary) {
                contents = DictionaryConfContents(dictionary, key, referenceList);
            }
            else {
                contents = DefaultConfContents(source, key, help, referenceList);
            }
            foreach (var item in contents) {
                yield return item;
            }
        }
        finally {
            referenceList.RemoveAt(referenceList.Count - 1);
        }
    }

    public string GetConfSource(object obj, string key = null, bool? multiline = null) {
        var equals = multiline == false ? "=" : " = ";
        var separator = multiline == false ? ";" : Environment.NewLine;
        var confContents = ConfContents(obj, key, help: multiline != false, []);
        return string
            .Join(separator, confContents
                .Select(pair => pair.Value == null
                    ? pair.Key
                    : string.Join(equals, pair.Key, pair.Value)))
            .Trim();
    }
}
