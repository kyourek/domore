using System;

namespace Domore.Conf {
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ConfConverterAttribute : Attribute {
        internal virtual ConfValueConverter ConverterInstance { get; }

        internal ConfConverterAttribute() {
        }

        public Type ConverterType { get; }

        public ConfConverterAttribute(Type converterType) {
            ConverterType = converterType;
        }
    }
}
