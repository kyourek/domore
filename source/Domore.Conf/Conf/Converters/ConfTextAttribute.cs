using Domore.Conf.Extensions;
using System;

namespace Domore.Conf.Converters;

/// <summary>
/// Conversion is done by considering the value as conf text.
/// </summary>
public sealed class ConfTextAttribute : ConfConverterAttribute {
    internal sealed override ConfValueConverter ConverterInstance => field ??=
        new ValueConverter();

    private sealed class ValueConverter : ConfValueConverter.Internal {
        protected sealed override object Convert(bool @internal, string value, ConfValueConverterState state) {
            if (null == state) throw new ArgumentNullException(nameof(state));
            var obj = state.Property.GetValue(state.Target, null);
            if (obj == null) {
                obj = Activator.CreateInstance(state.Property.PropertyType);
            }
            return obj.ConfFrom(value, key: "");
        }
    }
}
