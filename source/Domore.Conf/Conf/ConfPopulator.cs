using System;
using System.Collections.Generic;

namespace Domore.Conf;

internal sealed class ConfPopulator {
    private readonly ConfPropertyCache PropertyCache = new();
    private readonly ConfValueConverterCache ConverterCache = new();

    private ConfPopulator() {
    }

    private object Convert(IConfValue value, ConfTargetProperty property, IConf conf) {
        if (property is null) {
            throw new ArgumentNullException(nameof(property));
        }
        var converter = ConverterCache.ConverterFor(property.ConverterAttribute);
        var converted = converter.Convert(value?.Content, new(property.Target, property.PropertyInfo, conf));
        return converted;
    }

    private bool IsStringTarget(IConfKey key, object target) {
        if (key is null) {
            return false;
        }
        var type = target.GetType();
        object current = target;
        for (var i = 0; i < key.Parts.Count; i++) {
            var part = key.Parts[i];
            var property = PropertyCache.Get(type, part.Content);
            if (property.Exists != true || property.Populate != true) {
                return false;
            }
            type = property.PropertyType;
            if (part.Indices.Count > 0) {
                var item = PropertyCache.Get(type, "Item");
                if (item.Exists) {
                    if (!item.Populate) {
                        return false;
                    }
                    type = item.PropertyType;
                }
                current = null;
            }
            else if (current is not null && i + 1 < key.Parts.Count) {
                current = property.PropertyInfo.GetValue(current, null);
                type = current?.GetType() ?? type;
            }
        }
        return type == typeof(string);
    }

    private void Populate(IConfKey key, IConfValue value, object target, IConf conf) {
        if (key is not null && key.Parts.Count > 0) {
            var property = new ConfTargetProperty(target, key.Parts[0], PropertyCache);
            if (property.Exists) {
                if (property.Populate) {
                    switch (key.Parts.Count) {
                        case 1: {
                            var item = property.Item;
                            if (item is not null && item.Exists) {
                                if (item.Populate) {
                                    item.PropertyValue = Convert(value, item, conf);
                                }
                            }
                            else {
                                property.PropertyValue = Convert(value, property, conf);
                            }
                            break;
                        }
                        default: {
                            var keys = key.Skip();
                            var propertyValue = property.PropertyValue;
                            var propertyTarget = property;
                            if (propertyValue is null) {
                                propertyValue = property.PropertyValue =
                                    Activator.CreateInstance(property.PropertyType, nonPublic: true);
                            }
                            var propertyItem = property.Item;
                            if (propertyItem is not null) {
                                var itemValue = propertyItem.PropertyValue;
                                if (itemValue is null) {
                                    itemValue = propertyItem.PropertyValue =
                                        Activator.CreateInstance(propertyItem.PropertyType, nonPublic: true);
                                }
                                propertyValue = itemValue;
                                propertyTarget = propertyItem;
                            }
                            Populate(keys, value, propertyValue, conf);
                            if (propertyValue.GetType().IsValueType && propertyTarget.PropertyInfo.CanWrite) {
                                propertyTarget.PropertyValue = propertyValue;
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    public static ConfPopulator Cached { get; } = new ConfPopulator();

    public void Populate(object target, IConf conf, IEnumerable<IConfPair> pairs, bool includeEmptyStrings = false) {
        if (target is null) {
            throw new ArgumentNullException(nameof(target));
        }
        if (pairs is null) {
            throw new ArgumentNullException(nameof(pairs));
        }
        foreach (var pair in pairs) {
            if (pair is not null && (
                pair.Value?.Content != "" || (includeEmptyStrings && IsStringTarget(pair.Key, target)))) {
                Populate(pair.Key, pair.Value, target, conf);
            }
        }
        var callbacks = ConfPopulatedCallbackAttribute.For(target.GetType());
        foreach (var callback in callbacks) {
            callback.Call(target, conf, pairs);
        }
    }
}
