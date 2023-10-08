using System;
using System.ComponentModel;
using System.Reflection;

namespace Domore.Conf {
    public sealed class ConfValueConverterState {
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

        public object Target { get; }
        public IConf Conf { get; }
        public PropertyInfo Property { get; }

        public TypeConverter TypeConverter =>
            _TypeConverter ?? (
            _TypeConverter = TypeDescriptor.GetConverter(Property.PropertyType));
        private TypeConverter _TypeConverter;

        public ConfValueConverterState(object target, PropertyInfo property, IConf conf) {
            Conf = conf ?? throw new ArgumentNullException(nameof(conf));
            Target = target;
            Property = property ?? throw new ArgumentNullException(nameof(property));
        }
    }
}
