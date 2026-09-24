using Domore.Conf.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Domore.Conf.Cli;

internal static class TargetPropertyKind {
    private static readonly HashSet<Type> Numbers = [typeof(decimal), typeof(double), typeof(float)];
    private static readonly HashSet<Type> Integers = [
        typeof(byte), typeof(sbyte), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(short), typeof(ushort)];

    private static string For(Type type) {
        if (type is null) {
            return null;
        }
        if (typeof(bool) == type) {
            return "true/false";
        }
        if (typeof(char) == type) {
            return "chr";
        }
        if (typeof(string) == type) {
            return "str";
        }
        if (Numbers.Contains(type)) {
            return "num";
        }
        if (Integers.Contains(type)) {
            return "int";
        }
        if (type.IsEnum) {
            var flags = type.IsEnumFlags();
            var separator = flags ? "|" : "/";
            return string.Join(separator, CliType.GetEnumDisplay(type).Select(pair => pair.Value.ToLowerInvariant()));
        }
        if (typeof(IList).IsAssignableFrom(type)) {
            var itemType = ConfType.GetItemType(type);
            var itemKind = For(itemType);
            return itemKind == null || itemType == typeof(string) || itemType == typeof(object)
                ? ","
                : ",<" + itemKind + ">";
        }
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType is not null) {
            return For(underlyingType);
        }
        return null;
    }

    public static string For(TargetPropertyDescription target) {
        if (target is null) {
            throw new ArgumentNullException(nameof(target));
        }
        var type = target.PropertyType;
        if (typeof(IList).IsAssignableFrom(type)) {
            var itemType = ConfType.GetItemType(type);
            var itemKind = For(itemType);
            var itemSeparator = target.ConfListItemsAttribute.Separator;
            return itemKind is null || itemType == typeof(string) || itemType == typeof(object)
                ? (itemSeparator)
                : (itemSeparator + '<' + itemKind + '>');
        }
        return For(type);
    }
}
