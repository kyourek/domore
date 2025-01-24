using System;
using System.Linq;
using System.Reflection;

namespace Domore.Conf {
    using Extensions;

    internal sealed class ConfProperty {
        private static PropertyInfo From(string name, Type type) {
            if (name == null) return null;
            if (type == null) return null;
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
            var property = type.GetProperty(name, flags);
            if (property == null) {
                property = type
                    .GetProperties(flags)
                    .Select(p => new {
                        PropertyInfo = p,
                        ConfAttribute = p.GetConfAttribute()
                    })
                    .Where(i => i.ConfAttribute != null)
                    .Where(i => i.ConfAttribute.Names.Any(n => name.Equals(n, StringComparison.OrdinalIgnoreCase)))
                    .Select(i => i.PropertyInfo)
                    .FirstOrDefault();
            }
            return property;
        }

        public ConfAttribute Attribute =>
            _Attribute ?? (
            _Attribute = PropertyInfo?.GetCustomAttributes(typeof(ConfAttribute), inherit: true)?.FirstOrDefault() as ConfAttribute);
        private ConfAttribute _Attribute;

        public Type PropertyType =>
            _PropertyType ?? (
            _PropertyType = PropertyInfo?.PropertyType);
        private Type _PropertyType;

        public bool Exists =>
            _Exists ?? (
            _Exists = PropertyInfo != null).Value;
        private bool? _Exists;

        public bool Populate =>
            _Populate ?? (
            _Populate = Attribute?.IgnoreSet != true).Value;
        private bool? _Populate;

        public PropertyInfo PropertyInfo { get; }

        public ConfProperty(PropertyInfo propertyInfo) {
            PropertyInfo = propertyInfo;
        }

        public ConfProperty(string name, Type type) : this(From(name, type)) {
        }
    }
}
