using System;
using System.ComponentModel;
using System.Reflection;

namespace Domore.Conf {
    /// <summary>
    /// State object passed to value converters.
    /// </summary>
    public sealed class ConfValueConverterState {
        internal ConfValueConverterState(object target, PropertyInfo property, IConf conf) {
            Conf = conf ?? throw new ArgumentNullException(nameof(conf));
            Target = target;
            Property = property ?? throw new ArgumentNullException(nameof(property));
        }

        internal object PropertyTarget() {
            var obj = Property.GetValue(Target, null);
            if (obj == null) {
                obj = Activator.CreateInstance(Property.PropertyType);
            }
            return obj;
        }

        internal object Configure(object obj, string key) {
            return Conf.Configure(obj, key);
        }

        /// <summary>
        /// The object being populated by <see cref="Conf"/>.
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// The instance populating the <see cref="Target"/>.
        /// </summary>
        public IConf Conf { get; }

        /// <summary>
        /// The current property being converted.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// Gets the <see cref="TypeConverter"/> for the property type of <see cref="Property"/>.
        /// </summary>
        public TypeConverter TypeConverter => _TypeConverter ??= TypeDescriptor.GetConverter(Property.PropertyType);
        private TypeConverter _TypeConverter;
    }
}
