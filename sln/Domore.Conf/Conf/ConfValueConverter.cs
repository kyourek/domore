using Domore.Conf.Converters;
using System;
using System.Collections;

namespace Domore.Conf {
    public class ConfValueConverter {
        private static readonly ConfValueConverter DefaultListItemsConverter = new ConfListItemsAttribute().ConverterInstance;
        private static readonly ConfValueConverter DefaultEnumFlagsConverter = new ConfEnumFlagsAttribute().ConverterInstance;

        internal static object Default(string value, ConfValueConverterState state) {
            if (null == state) throw new ArgumentNullException(nameof(state));
            var converter = state.TypeConverter;
            try {
                return converter.ConvertFromString(value);
            }
            catch {
                var type = state.Property.PropertyType;
                if (type == typeof(Type)) {
                    return Type.GetType(value, throwOnError: true, ignoreCase: true);
                }
                if (type.IsEnum || (Nullable.GetUnderlyingType(type)?.IsEnum == true)) {
                    return DefaultEnumFlagsConverter.Convert(value, state);
                }
                if (typeof(IList).IsAssignableFrom(type)) {
                    return DefaultListItemsConverter.Convert(value, state);
                }
                var instanceType = Type.GetType(value, throwOnError: false, ignoreCase: true);
                if (instanceType != null) {
                    return Activator.CreateInstance(instanceType);
                }
                throw;
            }
        }

        public virtual object Convert(string value, ConfValueConverterState state) {
            try {
                return Default(value, state);
            }
            catch (Exception ex) {
                throw new ConfValueConverterException(this, value, state, ex);
            }
        }

        internal abstract class Internal : ConfValueConverter {
            protected abstract object Convert(bool @internal, string value, ConfValueConverterState state);

            public sealed override object Convert(string value, ConfValueConverterState state) {
                try {
                    return Convert(@internal: true, value, state);
                }
                catch (Exception ex) {
                    throw new ConfValueConverterException(this, value, state, ex);
                }
            }
        }
    }
}
