using System;

namespace Domore.Conf.Converters {
    public sealed class ConfKeyAttribute : ConfConverterAttribute {
        internal sealed override ConfValueConverter ConverterInstance =>
            _ConverterInstance ?? (
            _ConverterInstance = new ValueConverter { PropertySetByKey = PropertySetByKey });
        private ConfValueConverter _ConverterInstance;

        public string PropertySetByKey {
            get => _PropertySetByKey;
            set {
                if (_PropertySetByKey != value) {
                    _PropertySetByKey = value;
                    _ConverterInstance = null;
                }
            }
        }
        private string _PropertySetByKey;

        private sealed class ValueConverter : ConfValueConverter.Internal {
            protected sealed override object Convert(bool @internal, string value, ConfValueConverterState state) {
                if (null == value) throw new ArgumentNullException(nameof(value));
                if (null == state) throw new ArgumentNullException(nameof(state));
                var
                obj = state.PropertyTarget();
                obj = state.Configure(obj, key: value);
                var propertySetByKey = PropertySetByKey;
                if (propertySetByKey != null) {
                    var property = state.Property.PropertyType.GetProperty(propertySetByKey);
                    if (property == null) throw new InvalidOperationException(message: nameof(propertySetByKey));
                    property.SetValue(obj, value, null);
                }
                return obj;
            }

            public string PropertySetByKey { get; set; }
        }
    }
}
