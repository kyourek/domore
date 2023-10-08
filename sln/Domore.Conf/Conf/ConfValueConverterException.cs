using System;

namespace Domore.Conf {
    public sealed class ConfValueConverterException : ConfException {
        private static string GetMessage(ConfValueConverter converter, string value, ConfValueConverterState state, Exception innerException) {
            return "";
        }

        public string Value { get; }
        public ConfValueConverter Converter { get; }
        public ConfValueConverterState State { get; }

        public ConfValueConverterException(ConfValueConverter converter, string value, ConfValueConverterState state, Exception innerException) : base(GetMessage(converter, value, state, innerException), innerException) {
            Value = value;
            State = state;
            Converter = converter;
        }
    }
}
