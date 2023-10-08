using System;

namespace Domore.Conf.Converters {
    using Extensions;

    public sealed class ConfTextAttribute : ConfConverterAttribute {
        internal sealed override ConfValueConverter ConverterInstance =>
            _ConverterInstance ?? (
            _ConverterInstance = new ValueConverter());
        private ConfValueConverter _ConverterInstance;

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
}
