using System;

namespace Domore.Conf {
    /// <summary>
    /// The exception thrown when an error occurs during value conversion.
    /// </summary>
    public sealed class ConfValueConverterException : ConfException {
        private static string GetMessage(ConfValueConverter converter, string value, ConfValueConverterState state, Exception innerException) {
            return "";
        }

        internal ConfValueConverterException(ConfValueConverter converter, string value, ConfValueConverterState state, Exception innerException) : base(GetMessage(converter, value, state, innerException), innerException) {
            Value = value;
            State = state;
            Converter = converter;
        }

        /// <summary>
        /// Gets the original value that should have been converted.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the instance of <see cref="ConfValueConverter"/> that threw the exception.
        /// </summary>
        public ConfValueConverter Converter { get; }

        /// <summary>
        /// Gets the state object relevant at the time of the exception.
        /// </summary>
        public ConfValueConverterState State { get; }
    }
}
