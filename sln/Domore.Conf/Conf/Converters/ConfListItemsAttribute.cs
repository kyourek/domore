using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;

namespace Domore.Conf.Converters {
    using Extensions;

    public sealed class ConfListItemsAttribute : ConfConverterAttribute {
        internal sealed override ConfValueConverter ConverterInstance =>
            _ConverterInstance ?? (
            _ConverterInstance = new ValueConverter {
                Separator = Separator,
                ItemConverter = ItemConverter == null
                    ? null
                    : Activator.CreateInstance(ItemConverter)
            });
        private ConfValueConverter _ConverterInstance;

        public string Separator {
            get => _Separator ?? (_Separator = ",");
            set {
                if (_Separator != value) {
                    _Separator = value;
                    _ConverterInstance = null;
                }
            }
        }
        private string _Separator;

        public Type ItemConverter {
            get => _ItemConverter;
            set {
                if (_ItemConverter != value) {
                    _ItemConverter = value;
                    _ConverterInstance = null;
                }
            }
        }
        private Type _ItemConverter;

        private sealed class ValueConverter : ConfValueConverter.Internal {
            protected sealed override object Convert(bool @internal, string value, ConfValueConverterState state) {
                if (null == value) throw new ArgumentNullException(nameof(value));
                if (null == state) throw new ArgumentNullException(nameof(state));
                var obj = state.Property.GetValue(state.Target, null);
                if (obj == null) {
                    obj = Activator.CreateInstance(state.Property.PropertyType);
                }
                var list = (IList)obj;
                var itemConverter = ItemConverter;
                if (itemConverter == null) {
                    var itemType = ConfType.GetItemType(list.GetType());
                    if (itemType != null) {
                        itemConverter = TypeDescriptor.GetConverter(itemType);
                    }
                }
                var typeConverter = itemConverter as TypeConverter;
                var valueConverter = itemConverter as ConfValueConverter;
                var itemSeparator = Separator;
                var itemStrings = value.Split(new[] { itemSeparator }, StringSplitOptions.RemoveEmptyEntries).Select(s => s?.Trim() ?? "").Where(s => s != "");
                foreach (var itemString in itemStrings) {
                    if (typeConverter != null) {
                        list.Add(typeConverter.ConvertFromString(itemString));
                        continue;
                    }
                    if (valueConverter != null) {
                        list.Add(valueConverter.Convert(itemString, state));
                        continue;
                    }
                    list.Add(itemString);
                }
                return list;
            }

            public string Separator { get; set; }
            public object ItemConverter { get; set; }
        }
    }
}
