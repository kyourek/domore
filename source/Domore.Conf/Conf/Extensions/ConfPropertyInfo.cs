using System;
using System.Linq;
using System.Reflection;

namespace Domore.Conf.Extensions {
    internal static class ConfPropertyInfo {
        private static T GetAttribute<T>(this PropertyInfo propertyInfo) where T : Attribute {
            if (null == propertyInfo) throw new ArgumentNullException(nameof(propertyInfo));
            return propertyInfo.GetCustomAttributes(typeof(T), inherit: true)?.FirstOrDefault() as T;
        }

        public static ConfAttribute GetConfAttribute(this PropertyInfo propertyInfo) {
            return GetAttribute<ConfAttribute>(propertyInfo);
        }

        public static ConfConverterAttribute GetConverterAttribute(this PropertyInfo propertyInfo) {
            return GetAttribute<ConfConverterAttribute>(propertyInfo);
        }

        public static ConfHelpAttribute GetHelpAttribute(this PropertyInfo propertyInfo) {
            return GetAttribute<ConfHelpAttribute>(propertyInfo);
        }
    }
}
