using Domore.Conf.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Domore.Conf.Text;

internal sealed class TextSourceProvider {
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

    private IEnumerable<KeyValuePair<string, string>> ListConfContents(IList list, string key, List<object> referenceList) {
        if (null == list) throw new ArgumentNullException(nameof(list));
        if (key == null) {
            var listType = list.GetType();
            var listArgs = listType.GetGenericArguments();
            if (listArgs.Length == 1) {
                key = listArgs[0].Name;
            }
        }
        for (var i = 0; i < list.Count; i++) {
            var k = $"{key}[{i}]";
            var v = list[i];
            if (v != null) {
                var vType = v.GetType();
                if (vType.IsValueType || vType == typeof(string)) {
                    yield return new KeyValuePair<string, string>(
                        key: k,
                        value: Multiline($"{v}"));
                }
                else {
                    foreach (var kvp in ConfContents(v, k, help: false, referenceList)) {
                        yield return kvp;
                    }
                }
            }
        }
    }

    private IEnumerable<KeyValuePair<string, string>> DictionaryConfContents(IDictionary dictionary, string key, List<object> referenceList) {
        if (null == dictionary) throw new ArgumentNullException(nameof(dictionary));
        if (key == null) {
            var dictType = dictionary.GetType();
            var dictArgs = dictType.GetGenericArguments();
            if (dictArgs.Length == 2) {
                key = dictArgs[1].Name;
            }
        }
        var dKeys = dictionary.Keys;
        if (dKeys != null) {
            foreach (var dKey in dKeys) {
                var k = $"{key}[{dKey}]";
                var v = dictionary[dKey];
                if (v != null) {
                    var vType = v.GetType();
                    if (vType.IsValueType || vType == typeof(string)) {
                        yield return new KeyValuePair<string, string>(
                            key: k,
                            value: Multiline($"{v}"));
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

    private IEnumerable<KeyValuePair<string, string>> DefaultConfContents(object source, string key, bool help, List<object> referenceList) {
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
                        if (propertyValue != null) {
                            var helpTxt = !help ? null : property.GetHelpAttribute()?.Format("# ");
                            if (helpTxt != null) {
                                yield return new KeyValuePair<string, string>("", null);
                                yield return new KeyValuePair<string, string>(helpTxt, null);
                            }
                            var propertyValueType = propertyValue.GetType();
                            if (propertyValueType.IsValueType || propertyValueType == typeof(string)) {
                                if (property.CanWrite) {
                                    var pairKey = k(property.Name);
                                    var pairValue = Convert.ToString(propertyValue);
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

    private IEnumerable<KeyValuePair<string, string>> ConfContents(object source, string key, bool help, List<object> referenceList) {
        if (referenceList is null) {
            throw new ArgumentNullException(nameof(referenceList));
        }
        if (source is null) {
            throw new ArgumentNullException(nameof(source));
        }
        if (referenceList.Any(r => ReferenceEquals(r, source))) {
            throw new ConfCircularReferenceException(source);
        }
        else {
            referenceList.Add(source);
        }
        if (source is IList list) {
            return ListConfContents(list, key, referenceList);
        }
        if (source is IDictionary dictionary) {
            return DictionaryConfContents(dictionary, key, referenceList);
        }
        return DefaultConfContents(source, key, help, referenceList);
    }

    public string GetConfSource(object obj, string key = null, bool? multiline = null) {
        var equals = multiline == false ? "=" : " = ";
        var separator = multiline == false ? ";" : Environment.NewLine;
        var confContents = ConfContents(obj, key, help: multiline != false, new());
        return string
            .Join(separator, confContents
                .Select(pair => pair.Value == null
                    ? pair.Key
                    : string.Join(equals, pair.Key, pair.Value)))
            .Trim();
    }
}
