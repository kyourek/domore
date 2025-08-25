using Domore.Conf.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Domore.Conf.Text;

internal sealed class TextSourceProvider {
    private static string Multiline(string s) {
        if (s != null) {
            if (s.Contains('\n')) {
                s = string.Join(Environment.NewLine, "{", s, "}");
            }
        }
        return s;
    }

    private IEnumerable<KeyValuePair<string, string>> ListConfContents(IList list, string key) {
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
                    foreach (var kvp in ConfContents(v, k, help: false)) {
                        yield return kvp;
                    }
                }
            }
        }
    }

    private IEnumerable<KeyValuePair<string, string>> DictionaryConfContents(IDictionary dictionary, string key) {
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
                        foreach (var kvp in ConfContents(v, k, help: false)) {
                            yield return kvp;
                        }
                    }
                }
            }
        }
    }

    private IEnumerable<KeyValuePair<string, string>> DefaultConfContents(object source, string key, bool help) {
        var type = source.GetType();
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(property => property.Name);
        key = key ?? type.Name;
        string k(string s) => key == "" ? s : string.Join(".", key, s);
        foreach (var property in properties) {
            if (property.CanRead) {
                var confAttr = property.GetConfAttribute();
                if (confAttr == null || confAttr.IgnoreGet == false) {
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
                                    help: help);
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

    private IEnumerable<KeyValuePair<string, string>> ConfContents(object source, string key, bool help) {
        if (null == source) throw new ArgumentNullException(nameof(source));

        var list = source as IList;
        if (list != null) {
            return ListConfContents(list, key);
        }

        var dictionary = source as IDictionary;
        if (dictionary != null) {
            return DictionaryConfContents(dictionary, key);
        }

        return DefaultConfContents(source, key, help);
    }

    public string GetConfSource(object obj, string key = null, bool? multiline = null) {
        var equals = multiline == false ? "=" : " = ";
        var separator = multiline == false ? ";" : Environment.NewLine;
        var confContents = ConfContents(obj, key, help: multiline != false);
        return string
            .Join(separator, confContents
                .Select(pair => pair.Value == null
                    ? pair.Key
                    : string.Join(equals, pair.Key, pair.Value)))
            .Trim();
    }
}
