using System;

namespace Domore.Conf.Cli {
    public sealed class CliConversionException : Exception {
        private static string GetMessage(ConfValueConverterException innerException) {
            return "";
        }

        public CliConversionException(ConfValueConverterException innerException) : base(GetMessage(innerException), innerException) {
        }
    }
}
