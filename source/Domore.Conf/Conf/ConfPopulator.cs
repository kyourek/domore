using System;
using System.Collections.Generic;

namespace Domore.Conf {
    internal sealed class ConfPopulator {
        private readonly ConfPropertyCache PropertyCache = new ConfPropertyCache();
        private readonly ConfValueConverterCache ConverterCache = new ConfValueConverterCache();

        private ConfPopulator() {
        }

        private object Convert(IConfValue value, ConfTargetProperty property, IConf conf) {
            if (null == property) throw new ArgumentNullException(nameof(property));
            var converter = ConverterCache.ConverterFor(property.ConverterAttribute);
            var converted = converter.Convert(value?.Content, new ConfValueConverterState(property.Target, property.PropertyInfo, conf));
            return converted;
        }

        private void Populate(IConfKey key, IConfValue value, object target, IConf conf) {
            if (key != null && key.Parts.Count > 0) {
                var property = new ConfTargetProperty(target, key.Parts[0], PropertyCache);
                if (property.Exists) {
                    if (property.Populate) {
                        switch (key.Parts.Count) {
                            case 1: {
                                var item = property.Item;
                                if (item != null && item.Exists) {
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
                                if (propertyValue == null) {
                                    propertyValue = property.PropertyValue = Activator.CreateInstance(property.PropertyType, nonPublic: true);
                                }
                                var propertyItem = property.Item;
                                if (propertyItem != null) {
                                    var itemValue = propertyItem.PropertyValue;
                                    if (itemValue == null) {
                                        itemValue = propertyItem.PropertyValue = Activator.CreateInstance(propertyItem.PropertyType, nonPublic: true);
                                    }
                                    propertyValue = itemValue;
                                }
                                Populate(keys, value, propertyValue, conf);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static ConfPopulator Cached { get; } = new ConfPopulator();

        public void Populate(object target, IConf conf, IEnumerable<IConfPair> pairs) {
            if (null == pairs) throw new ArgumentNullException(nameof(pairs));
            foreach (var pair in pairs) {
                Populate(pair.Key, pair.Value, target, conf);
            }
        }
    }
}
