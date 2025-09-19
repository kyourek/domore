using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Domore.Conf.Extensions;

internal static class ConfType {
    public static IEnumerable<MemberInfo> GetEnumMembers(this Type type) {
        if (null == type) throw new ArgumentNullException(nameof(type));
        foreach (var name in type.GetEnumNames()) {
            var member = type.GetMember(name).FirstOrDefault(m => m.DeclaringType == type);
            if (member != null) {
                yield return member;
            }
        }
    }

    public static IEnumerable<KeyValuePair<MemberInfo, T>> GetEnumAttributes<T>(this Type type) {
        foreach (var member in GetEnumMembers(type)) {
            yield return new KeyValuePair<MemberInfo, T>(
                key: member,
                value: member
                    .GetCustomAttributes(typeof(T), inherit: true)
                    .OfType<T>()
                    .FirstOrDefault());
        }
    }

    public static Dictionary<MemberInfo, HashSet<string>> GetEnumAlias(this Type type) {
        return GetEnumAttributes<ConfAttribute>(type).ToDictionary(
            pair => pair.Key,
            pair => {
                var n = pair.Key.Name;
                var set = new HashSet<string>(new[] { n }, StringComparer.OrdinalIgnoreCase);
                var conf = pair.Value;
                if (conf != null) {
                    foreach (var alias in conf.Names) {
                        set.Add(alias);
                    }
                }
                return set;
            });
    }

    public static bool IsEnumFlags(this Type type) {
        if (null == type) throw new ArgumentNullException(nameof(type));
        return
            type.IsEnum &&
            type.GetCustomAttributes(typeof(FlagsAttribute), inherit: true).Any();
    }

    public static Type GetItemType(this Type type) {
        if (null == type) throw new ArgumentNullException(nameof(type));
        for (; ; ) {
            if (type == null) {
                return null;
            }
            if (type.IsGenericType) {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(List<>) || genericType == typeof(Collection<>)) {
                    return type.GetGenericArguments().FirstOrDefault();
                }
            }
            type = type.BaseType;
        }
    }
}
