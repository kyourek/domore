using Domore.Conf.Extensions;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;

namespace Domore.Conf.Converters;

/// <summary>
/// Conversion of conf text to an instance of a list.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ConfListItemsAttribute : ConfConverterAttribute {
    internal sealed override ConfValueConverter ConverterInstance => _ConverterInstance ??=
        new ValueConverter {
            Separator = Separator,
            ItemConverter = ItemConverter == null
                ? null
                : Activator.CreateInstance(ItemConverter)
        };
    private ConfValueConverter _ConverterInstance;

    /// <summary>
    /// Gets or sets the string separator between items in the list.
    /// </summary>
    public string Separator {
        get => _Separator ??= ",";
        set {
            if (_Separator != value) {
                _Separator = value;
                _ConverterInstance = null;
            }
        }
    }
    private string _Separator;

    /// <summary>
    /// Gets or sets the type of the converter used to convert conf text into the items of the list.
    /// </summary>
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
